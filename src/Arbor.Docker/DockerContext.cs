using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Arbor.Docker;

public sealed class DockerContext : IAsyncDisposable
{
    private readonly IReadOnlyCollection<ContainerSettings> _containerArgs;

    public ILogger Logger { get; }
    private bool _isDisposing;
    private bool _isDisposed;

    private DockerContext(
        Task task,
        IReadOnlyCollection<ContainerSettings> containers,
        ILogger logger,
        CancellationTokenSource cancellationTokenSource)
    {
        ContainerTask = task;
        _containerArgs = containers;
        Logger = logger;
        CancellationTokenSource = cancellationTokenSource;

        Containers = [
            .._containerArgs
                .Select(containerArgs => new ContainerInfo(containerArgs.ContainerName,
                    containerArgs.ImageName, containerArgs.EnvironmentVariables,
                    containerArgs.Ports))
        ];
    }

    public ImmutableArray<ContainerInfo> Containers { get; }

    public Task ContainerTask { get; }

    public CancellationTokenSource CancellationTokenSource { get; }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed || _isDisposing)
        {
            return;
        }

        _isDisposing = true;

        if (CancellationTokenSource.IsCancellationRequested == false)
        {
            CancellationTokenSource.Cancel(false);
        }

        try
        {
            await ContainerTask;
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        finally
        {
            await Task.Delay(TimeSpan.FromSeconds(3));

            await ShutdownContainersAsync(_containerArgs, Logger, true);
        }

        CancellationTokenSource?.Dispose();

        _isDisposed = true;
        _isDisposing = false;
    }

    public static Task<DockerContext> CreateContextAsync(
        IReadOnlyCollection<ContainerSettings> args,
        ILogger logger)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        var dockerTask = Task.Run(
            () => StartAllDockerContainers(args, logger, cancellationTokenSource.Token),
            cancellationTokenSource.Token);

        var dockerContext = new DockerContext(dockerTask, args, logger, cancellationTokenSource);

        return Task.FromResult(dockerContext);
    }

    private static async Task ShutDownAndRemoveAsync(string containerName, ILogger logger, bool logAsDebug)
    {
        var args = new List<string> {"stop", containerName};

        var stopExitCode = await DockerHelper.RunDockerCommandsAsync(args, logger);

        logger.Information("Stop exit code: {StopExitCode}", stopExitCode);

        var removeArgs = new List<string> {"rm", containerName};

        var removeExitCode = await DockerHelper.RunDockerCommandsAsync(removeArgs, logger, logAsDebug: logAsDebug);

        logger.Information("Remove exit code: {RemoveExitCode}", removeExitCode);
    }

    private static async Task ShutdownContainersAsync(IReadOnlyCollection<ContainerSettings> containers,
        ILogger logger,
        bool logAsDebug)
    {
        foreach (var container in containers)
        {
            await ShutDownAndRemoveAsync(container.ContainerName, logger, logAsDebug);
        }
    }

    private static async Task StartAllDockerContainers(
        IReadOnlyCollection<ContainerSettings> containers,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await ShutdownContainersAsync(containers, logger, true);

        logger.Debug("All containers are shutdown");

        var tasks = containers.Select(
                containerArgs =>
                    DockerHelper.RunDockerCommandsAsync(containerArgs.StartArguments(), logger, null, false,
                        cancellationToken))
            .ToImmutableArray();

        await Task.WhenAll(tasks.ToArray());

        logger.Debug("All containers are started");
    }
}