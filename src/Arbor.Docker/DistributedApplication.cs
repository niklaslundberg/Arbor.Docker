using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Docker.WebApi;
using Arbor.Processing;
using Serilog;

namespace Arbor.Docker;

public class DistributedApplication : IAsyncDisposable
{
    private readonly ILogger _logger;
    private ApplicationState _state = ApplicationState.NotStarted;
    private readonly SemaphoreSlim _startSemaphoreSlim = new(1, 1);
    private readonly SemaphoreSlim _stopSemaphoreSlim = new(1, 1);
    private readonly ManualResetEventSlim _resetEventSlim = new();
    private int _currentOrder;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    internal Dictionary<string, ResourceNode> Resources { get; }

    public ApplicationEvents Events { get; }
    public ResourceEvents ResourceEvents { get; }

    internal async Task Initialize()
    {
        foreach (var (_, resourceNode) in Resources.OrderBy(r => r.Value.Order))
        {
            await resourceNode.InitializeAsync();

            await ResourceEvents.Publish(new ResourceInitialized(resourceNode));
        }
    }

    internal DistributedApplication(Dictionary<string, ResourceNode> resources, ILogger logger, ResourceEvents resourceEvents)
    {
        _logger = logger;
        Resources = resources;
        Events = new ApplicationEvents(_cancellationTokenSource);
        ResourceEvents = resourceEvents;
    }

    public async ValueTask DisposeAsync()
    {
        _logger.Debug("Disposing distributed application");
        foreach (var (name, treeNode) in Resources.OrderByDescending(resource => resource.Value.Order))
        {
            await treeNode.Resource.SafeDisposeAsync();

            _logger.Debug("Disposed resource {Type} '{Name}'", treeNode.Resource.GetType().Name, name);
        }

        _startSemaphoreSlim.Dispose();
        _stopSemaphoreSlim.Dispose();
        await _logger.SafeDisposeAsync();

    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_state is ApplicationState.Started or ApplicationState.Starting)
        {
            return;
        }

        await _startSemaphoreSlim.WaitAsync(cancellationToken);

        if (_state is ApplicationState.Started or ApplicationState.Starting)
        {
            return;
        }

        if (_state is not ApplicationState.NotStarted)
        {
            throw new InvalidOperationException($"Cannot start, invalid state {_state}");
        }

        _state = ApplicationState.Starting;

        await Events.Publish(new ApplicationStarting());

        foreach (var (_, resourceNode) in Resources)
        {
            StartHealthCheck(cancellationToken, resourceNode);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var notStarted = GetNotStarted();

            if (notStarted.Count == 0)
            {
                break;
            }

            foreach (var node in notStarted.Where(node => node.Value.CanBeStarted))
            {
                _logger.Debug("Starting resource {Type} '{Name}'", node.Value.GetType().Name, node.Value.Name);
                node.Value.StartTask = node.Value.StartAsync(cancellationToken);

                _currentOrder++;
                node.Value.Order = _currentOrder;
            }

            if (GetNotStarted().Count == 0)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }

        await Task.WhenAll(Resources.Select(resourceNode => resourceNode.Value.StartTask!));

        foreach (ResourceNode resourcesValue in Resources.Values)
        {
            resourcesValue.StartTask = null;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var healthCheckResources = Resources.Where(r => r.Value.HealthCheck is { }).ToList();

            if (healthCheckResources.All(s => s.Value.Health == HealthCheckStatus.Healthy))
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        _state = ApplicationState.Started;

        await Events.Publish(new ApplicationStarted());
    }

    private List<KeyValuePair<string, ResourceNode>> GetNotStarted() => Resources.Where(resource => resource.Value is { StartTask: null }).ToList();

    private void StartHealthCheck(CancellationToken cancellationToken, ResourceNode resource)
    {
        if (resource.HealthCheck is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
                using var linkedToken =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

                try
                {
                    var health = await resource.HealthCheck.CheckHealthAsync(linkedToken.Token);

                    if (health is HealthCheckStatus.Healthy)
                    {
                        resource.Health = health;
                        _ = Task.Run(() => ResourceEvents.Publish(new ResourceHealthy(resource)), linkedToken.Token);
                        break;
                    }
                }
                catch (Exception ex) when (ex is TimeoutException or TaskCanceledException)
                {

                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Could not complete health check for resource '{ResourceName}'", resource.Name);
                }
            }
        }, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_state is ApplicationState.Stopped or ApplicationState.Stopping)
        {
            return;
        }

        await _stopSemaphoreSlim.WaitAsync(cancellationToken);

        if (_state is ApplicationState.Stopped or ApplicationState.Stopping)
        {
            return;
        }

        if (_state is not (ApplicationState.Started or ApplicationState.Starting))
        {
            throw new InvalidOperationException("Cannot stop unless starting or started");
        }

        _state = ApplicationState.Stopping;
        await Events.Publish(new ApplicationStopping());
        _resetEventSlim.Set();
        _state = ApplicationState.Stopped;
        await Events.Publish(new ApplicationStopped());
    }

    public async Task<ExitCode> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StartAsync(cancellationToken);

            if (!Resources.Any(resourceNode => resourceNode.Value.IsRunning))
            {
                _resetEventSlim.Set();
                await StopAsync(cancellationToken);
            }

            _resetEventSlim.Wait(cancellationToken);

            return ExitCode.Success;

        }
        catch (Exception ex)
        {
            _state = ApplicationState.Failed;
            await Events.Publish(new ApplicationFailed(ex));
            return ExitCode.Failure;
        }
    }

    public static DistributedApplicationBuilder CreateBuilder(string[] args, ILogger? logger = null) => new(args, logger);

    public HttpClient GetHttpClient(string name)
    {
        if (!Resources.TryGetValue(name, out var resource))
        {
            throw new InvalidOperationException($"Could not find resource '{name}'");
        }

        if (resource is not WebApiNode webApiNode)
        {
            throw new InvalidOperationException($"Resource '{name}' is not an {nameof(webApiNode)} resource");
        }

        if (webApiNode.EndPoints.IsEmpty)
        {
            throw new InvalidOperationException($"Resource '{name}' has no endpoints");
        }

        var httpEndPoints = webApiNode.EndPoints.OfType<HttpEndPoint>().ToList();

        if (httpEndPoints.Count == 1)
        {
            HttpEndPoint httpEndPoint = httpEndPoints[0];

            return new HttpClient { BaseAddress = new($"http://localhost:{httpEndPoint.PortUsage.Port}") };
        }

        if (httpEndPoints.Count > 1)
        {
            throw new InvalidOperationException($"Resource '{name}' has multiple http endpoints, cannot determine which to use");
        }

        throw new InvalidOperationException($"Could not find an http endpoint for resource '{name}'");
    }
}