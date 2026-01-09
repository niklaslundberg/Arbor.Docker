namespace Arbor.Docker;

public class ResourceHealthy : IResourceEvent
{
    public IResourceReference Resource { get; }

    public ResourceHealthy(IResourceReference resource)
    {
        Resource = resource;
    }
    public override string ToString() => $"{nameof(ResourceHealthy)} '{Resource.Name}'";
}