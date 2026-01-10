namespace Arbor.Docker;

public sealed class DynamicContainerPortMapping(int containerPort) : IContainerPortMapping
{
    public int ContainerPort { get; } = containerPort;

    public int? HostPort => null;
}