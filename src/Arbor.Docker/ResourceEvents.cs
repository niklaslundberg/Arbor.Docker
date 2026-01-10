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

    internal async Task Publish(IResourceEvent @event, PublishMode publishMode = PublishMode.AwaitedSequential, CancellationToken cancellationToken = default)
    {
        if (publishMode == PublishMode.FireAndForgetParallel)
        {
            _ = Task.Run(() => Parallel.ForEachAsync(_subscriptions, cancellationToken, async (subscription, token) => await subscription.Value.Invoke(@event, token)), cancellationToken);

            return;
        }

        if (publishMode == PublishMode.FireAndForgetSequential)
        {
            _ = Task.Run(async () =>
            {
                foreach (var (_, func) in _subscriptions)
                {
                    await func.Invoke(@event, _cancellationTokenSource.Token);
                }
            }, cancellationToken);

            return;
        }

        if (publishMode == PublishMode.AwaitedParallel)
        {
            await Parallel.ForEachAsync(_subscriptions, cancellationToken, async (subscription, token) => await subscription.Value.Invoke(@event, token));

            return;
        }

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