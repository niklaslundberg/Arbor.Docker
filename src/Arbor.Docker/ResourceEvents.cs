using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;

public sealed class ResourceEvents
{
    internal ResourceEvents(CancellationTokenSource cancellationTokenSource) => _cancellationTokenSource = cancellationTokenSource;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentDictionary<ResourceEventSubscription, Func<IResourceEvent, CancellationToken, Task>> _subscriptions = [];

    internal async Task Publish(IResourceEvent @event)
    {
        foreach (var (_, func) in _subscriptions)
        {
            await func.Invoke(@event, _cancellationTokenSource.Token);
        }
    }

    public IResourceEventSubscription Subscribe(Func<IResourceEvent, CancellationToken, Task> handler)
    {
        var eventSubscription = new ResourceEventSubscription(this);
        _ = _subscriptions.TryAdd(eventSubscription, handler);
        return eventSubscription;
    }

    internal void TryRemove(ResourceEventSubscription eventSubscription) => _ = _subscriptions.TryRemove(eventSubscription, out _);
}