namespace Arbor.Docker;

public class EndPointAllocated(IResourceReference resource, IResourceEndPoint endPoint) : IResourceEvent
{
    public IResourceReference Resource { get; } = resource;

    public IResourceEndPoint EndPoint { get; } = endPoint;

    public override string ToString() => $"{nameof(EndPointAllocated)} '{Resource.Name}' {EndPoint}";
}