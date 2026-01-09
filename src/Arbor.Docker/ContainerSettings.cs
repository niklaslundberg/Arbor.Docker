using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Arbor.Docker;

public record ContainerSettings
{
    private readonly bool _useExplicitPlatform;

    public ContainerSettings(
        string imageName,
        string containerName,
        IEnumerable<PortMapping>? ports = null,
        IDictionary<string, string>? environmentVariables = null,
        string[]? args = null,
        string[]? entryPoint = null,
        bool useExplicitPlatform = false)
    {
        if (string.IsNullOrWhiteSpace(imageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(imageName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(containerName));
        }

        _useExplicitPlatform = useExplicitPlatform;

        ImageName = imageName;
        ContainerName = containerName;
        Ports = ports?.ToImmutableArray() ?? [];

        EnvironmentVariables = environmentVariables?.ToImmutableDictionary() ?? [];

        Args = args?.ToImmutableArray() ?? [];
        EntryPoint = entryPoint?.ToImmutableArray() ?? [];
    }

    public IResourceHealthCheck? HealthCheck { get; init; }

    public ImmutableArray<string> EntryPoint { get; }

    public ImmutableArray<string> Args { get; }

    public string ContainerName { get; }

    public ImmutableDictionary<string, string> EnvironmentVariables { get; }

    public string ImageName { get; }

    public ImmutableArray<PortMapping> Ports { get; init; }

    public ImmutableArray<string> StartArguments() => [..CombinedArgs().Concat(EntryPoint)];

    public ImmutableArray<string> CombinedArgs()
    {
        var args = new List<string> {"run", "-d"};

        foreach (var range in Ports)
        {
            args.Add("-p");

            if (range.HostPorts.End > range.HostPorts.Start)
            {
                args.Add(
                    $"{range.HostPorts.Start}-{range.HostPorts.End}:{range.ContainerPorts.Start}-{range.ContainerPorts.End}");
            }
            else
            {
                args.Add($"{range.HostPorts.Start}:{range.ContainerPorts.Start}");
            }
        }

        foreach (var keyValuePair in EnvironmentVariables)
        {
            args.Add("-e");
            args.Add($"{keyValuePair.Key}={keyValuePair.Value}");
        }

        foreach (string arg in Args)
        {
            args.Add(arg);
        }

        args.Add("--name");
        args.Add(ContainerName);

        if (_useExplicitPlatform)
        {
            args.Add("--platform=linux");
        }

        args.Add(ImageName);

        return [..args];
    }
}