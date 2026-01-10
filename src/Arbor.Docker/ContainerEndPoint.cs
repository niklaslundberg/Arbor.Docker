using System;

namespace Arbor.Docker;

public class ContainerEndPoint: IResourceEndPoint
{
    public PortMapping PortMapping { get; }

    public ContainerEndPoint(PortMapping portMapping)
    {
        if (portMapping.HostPorts.Length != 1)
        {
            throw new ArgumentException("Port mapping must have exactly one host port.");
        }

        PortMapping = portMapping;
    }

    public override string ToString() => PortMapping.ToString();

    public int HostPort => PortMapping.HostPorts.Start;
}