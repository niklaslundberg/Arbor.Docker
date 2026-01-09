namespace Arbor.Docker;

public interface IContainerPortMapping
{
    public int ContainerPort { get; }
    public int? HostPort { get; }
}