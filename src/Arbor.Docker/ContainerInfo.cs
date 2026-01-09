using System;
using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;

namespace Arbor.Docker;

public class ContainerInfo
{
    public ContainerInfo(string name,
        string imageName,
        ImmutableDictionary<string, string> environmentVariables,
        ImmutableArray<PortMapping> ports)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(imageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(imageName));
        }

        Name = name;
        ImageName = imageName;
        EnvironmentVariables = environmentVariables;
        Ports = ports;
    }

    public string Name { get; }

    public string ImageName { get; }

    public ImmutableDictionary<string, string> EnvironmentVariables { get; }

    public ImmutableArray<PortMapping> Ports { get; }
}