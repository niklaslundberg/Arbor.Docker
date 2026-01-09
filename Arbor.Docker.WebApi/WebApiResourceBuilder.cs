namespace Arbor.Docker.WebApi;

public class WebApiResourceBuilder
{
    private readonly WebApiNode _apiResource;

    internal WebApiResourceBuilder(WebApiNode containerResource) =>
        _apiResource = containerResource;

    public WebApiResource WebApiResource =>
        (WebApiResource)_apiResource.Resource;

    public WebApiResourceBuilder DependsOn(IResourceReference resource)
    {
        _apiResource.DependsOn(resource);

        return this;
    }

    public WebApiResourceBuilder WaitFor(IResourceReference resource)
    {
        _apiResource.WaitFor(resource);

        return this;
    }

    public WebApiResourceBuilder AddHealthCheck()
    {
        _apiResource.HealthCheck = new HttpHealthCheck(new Uri("/health", UriKind.Relative));

        return this;
    }
}