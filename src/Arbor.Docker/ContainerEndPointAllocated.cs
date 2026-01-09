namespace Arbor.Docker;

public class ContainerEndPointAllocated : EndPointAllocated
{
    public ContainerEndPointAllocated(IResourceReference resource, ContainerEndPoint containerEndPoint) : base(resource,
        containerEndPoint)

    {

    }
}