namespace Arbor.Docker;

public static class ContainerResourceExtensions
{
    public static ContainerResourceBuilder AddContainer(this DistributedApplicationBuilder builder, string name,
        ContainerSettings settings)
    {
        var containerResource = new ContainerResource(settings, builder.Logger);

        var containerNode = new ContainerResourceNode(name, builder)
        {
            Resource = containerResource
        };

        var containerResourceBuilder = new ContainerResourceBuilder(containerNode);

        builder.AddResource(containerNode);

        return containerResourceBuilder;
    }
}