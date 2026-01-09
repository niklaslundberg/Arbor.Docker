namespace Arbor.Docker;

public class ResourceStarted: IResourceEvent
{
    public IResourceReference Resource { get; }

    internal ResourceStarted(IResourceReference resource)
    {
        Resource = resource;
    }

    public override string ToString() => $"{nameof(ResourceStarted)} '{Resource.Name}'";
}