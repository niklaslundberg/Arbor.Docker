using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;

public sealed class ApplicationEvents
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentDictionary<ApplicationEventSubscription, Func<IApplicationEvent, CancellationToken, Task>> _subscriptions = [];
    internal ApplicationEvents(CancellationTokenSource cancellationTokenSource) => _cancellationTokenSource = cancellationTokenSource;

    internal async Task Publish(IApplicationEvent @event, PublishMode publishMode = PublishMode.AwaitedSequential)
    {
        if (publishMode == PublishMode.FireAndForgetParallel)
        {
            _ = Task.Run(() => Parallel.ForEachAsync(_subscriptions, async (subscription, token) => await subscription.Value.Invoke(@event, token)));

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
            });

            return;
        }

        if (publishMode == PublishMode.AwaitedParallel)
        {
            await Parallel.ForEachAsync(_subscriptions,
                async (subscription, token) => await subscription.Value.Invoke(@event, token));

            return;
        }

        foreach (var (_, func) in _subscriptions)
        {
            await func.Invoke(@event, _cancellationTokenSource.Token);
        }
    }

    public IApplicationEventSubscription Subscribe(Func<IApplicationEvent, CancellationToken, Task> handler)
    {
        var applicationEventSubscription = new ApplicationEventSubscription(this);
        _ = _subscriptions.TryAdd(applicationEventSubscription, handler);
        return applicationEventSubscription;
    }

    internal void TryRemove(ApplicationEventSubscription applicationEventSubscription) =>
        _ = _subscriptions.TryRemove(applicationEventSubscription, out _);
}
