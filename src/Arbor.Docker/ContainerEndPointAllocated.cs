namespace Arbor.Docker;

public class ContainerEndPointAllocated : EndPointAllocated
{
    internal ContainerEndPointAllocated(IResourceReference resource, ContainerEndPoint containerEndPoint)
        : base(resource, containerEndPoint)
    {
    }
}