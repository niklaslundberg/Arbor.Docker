namespace Arbor.Docker;

public class ResourceStarting : IResourceEvent
{
    public IResourceReference Resource { get; }

    internal ResourceStarting(IResourceReference resource)
    {
        Resource = resource;
    }

    public override string ToString() => $"{nameof(ResourceStarting)} '{Resource.Name}'";
}