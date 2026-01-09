using Arbor.Hosting;
using Microsoft.Extensions.Caching.Distributed;

namespace Arbor.Docker.SampleApi;

public class CustomApiSampleApp: ICustomApplication
{
    public Task ConfigureBuilder(WebApplicationBuilder builder)
    {

        // Add services to the container.
        builder.Services.AddAuthorization();

        builder.Services.AddHealthChecks();

        builder.Services.AddDistributedMemoryCache();

        return Task.CompletedTask;
    }

    public Task ConfigureApplication(WebApplicationBuilder builder, WebApplication app)
    {
        app.MapGet("/", async (IDistributedCache cache) =>
            new { CacheHit = await cache.GetStringAsync("test") is "hello world" });

        app.MapHealthChecks("/health");

        return Task.CompletedTask;
    }
}