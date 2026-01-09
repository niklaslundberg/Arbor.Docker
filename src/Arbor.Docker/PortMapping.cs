using System;

namespace Arbor.Docker;

public readonly struct PortMapping
{
    public static PortMapping MapSinglePort(int hostPort, int containerPort) =>
        new(new PortRange(hostPort), new PortRange(containerPort));

    public PortMapping(PortRange hostPorts, PortRange containerPorts)
    {
        if (hostPorts.Length != containerPorts.Length)
        {
            throw new ArgumentException("Port ranges must be equal");
        }

        HostPorts = hostPorts;
        ContainerPorts = containerPorts;
    }

    public PortRange HostPorts { get; }

    public PortRange ContainerPorts { get; }

    public override string ToString()
    {
        if (HostPorts.End > HostPorts.Start)
        {
            return $"{HostPorts.Start}-{HostPorts.End}:{ContainerPorts.Start}-{ContainerPorts.End}";
        }

        return $"{HostPorts.Start}:{ContainerPorts.Start}";
    }
}