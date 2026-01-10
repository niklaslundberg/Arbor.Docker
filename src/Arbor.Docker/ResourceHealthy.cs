namespace Arbor.Docker;

public sealed class ResourceHealthy : IResourceEvent
{
    public IResourceReference Resource { get; }

    internal ResourceHealthy(IResourceReference resource) => Resource = resource;
    public override string ToString() => $"{nameof(ResourceHealthy)} '{Resource.Name}'";
}