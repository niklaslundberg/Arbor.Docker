using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;

internal class ResourceNode : IResourceReference
{
    public ResourceNode(string name, DistributedApplicationBuilder builder)
    {
        Events = new ResourceEvents(builder.CancellationTokenSource);

        _ = Events.Subscribe((@event, _) => builder.ResourceEvents.Publish(@event));

        _ = Events.Subscribe((@event, _) =>
        {
            if (@event is EndPointAllocated endPointAllocated)
            {
                ResourceEndPoints.Add(endPointAllocated.EndPoint);
            }

            return Task.CompletedTask;
        });

        Name = name;
    }

    public string Name { get; }

    public required IResource Resource { get; init; }

    public int Order { get; set; }

    public HashSet<ResourceNode> DependsOn { get; } = [];

    public HashSet<ResourceNode> DependsOnStarted { get; } = [];

    public ImmutableArray<IResourceEndPoint> EndPoints => [.. ResourceEndPoints];

    internal ConcurrentBag<IResourceEndPoint> ResourceEndPoints { get; } = [];

    public ResourceEvents Events { get; }

    public IResourceHealthCheck? HealthCheck { get; internal set; }

    internal Task? StartTask
    {
        get;
        set
        {
            field = value;
            IsRunning = true;
        }
    }

    public bool CanBeStarted => !IsRunning && StartTask is null &&
                                (DependsOnStarted.Count == 0 ||
                                 DependsOnStarted.All(dep => dep.StartTask?.IsCompletedSuccessfully == true && (dep.HealthCheck is null || dep.Health == HealthCheckStatus.Healthy)));

    public bool IsRunning { get; internal set; }

    public HealthCheckStatus Health { get; internal set; } = HealthCheckStatus.Unknown;

    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await Events.Publish(new ResourceStarting(this), cancellationToken: cancellationToken);

        await Resource.StartAsync(cancellationToken);

        await Events.Publish(new ResourceStarted(this), cancellationToken: cancellationToken);
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
}