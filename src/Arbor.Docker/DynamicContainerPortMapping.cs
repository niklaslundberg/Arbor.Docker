namespace Arbor.Docker;

public sealed class DynamicContainerPortMapping : IContainerPortMapping
{
    public int ContainerPort { get; }
    public int? HostPort => null;

    public DynamicContainerPortMapping(int containerPort)
    {
        ContainerPort = containerPort;
    }
}