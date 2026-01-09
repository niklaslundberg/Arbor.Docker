namespace Arbor.Docker;

public sealed class ResourceInitialized(IResourceReference resource) : IResourceEvent
{
    public IResourceReference Resource { get; } = resource;

    public override string ToString() => $"{nameof(ResourceInitialized)} '{Resource.Name}'";
}