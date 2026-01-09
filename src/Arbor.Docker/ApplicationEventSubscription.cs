namespace Arbor.Docker;

internal class ApplicationEventSubscription : IApplicationEventSubscription
{
    private readonly ApplicationEvents _applicationEvents;

    internal ApplicationEventSubscription(ApplicationEvents applicationEvents)
    {
        _applicationEvents = applicationEvents;
    }
    public void Dispose()
    {
        _applicationEvents.TryRemove(this);
    }
}