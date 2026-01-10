namespace Arbor.Docker;

public sealed class ResourceEventSubscription : IResourceEventSubscription
{
    private readonly ResourceEvents _resourceEvents;

    internal ResourceEventSubscription(ResourceEvents resourceEvents) => _resourceEvents = resourceEvents;

    public void Dispose() => _resourceEvents.TryRemove(this);
}