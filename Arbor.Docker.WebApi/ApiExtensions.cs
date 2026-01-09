namespace Arbor.Docker.WebApi;

public static class ApiExtensions
{
    public static WebApiResourceBuilder AddWebApi(this DistributedApplicationBuilder builder,
        string name,
        WebApiResource resource)
    {
        var webApiNode = new WebApiNode(name, builder, builder.PortHelper)
        {
            Resource = resource
        };

        builder.AddResource(webApiNode);

        return new WebApiResourceBuilder(webApiNode);

    }
}