using System;

namespace Arbor.Docker;

public class ContainerResourceBuilder
{
    private readonly ContainerResourceNode _containerResource;

    internal ContainerResourceBuilder(ContainerResourceNode containerResource) =>
        _containerResource = containerResource;

    public IResourceReference ContainerResource => _containerResource;

    public ContainerResourceBuilder DependsOn(IResourceReference resource)
    {
        _containerResource.DependsOn(resource);

        return this;
    }

    public ContainerResourceBuilder WaitFor(IResourceReference resource)
    {
        _containerResource.WaitFor(resource);

        return this;
    }

    public ContainerResourceBuilder WithEndPoint(IContainerPortMapping containerPortMapping)
    {
        _containerResource.PortMappings.Add(containerPortMapping);

        return this;
    }
}