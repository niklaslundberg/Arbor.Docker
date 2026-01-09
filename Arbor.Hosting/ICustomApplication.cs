using Microsoft.AspNetCore.Builder;

namespace Arbor.Hosting;

public interface ICustomApplication
{
    public Task ConfigureBuilder(WebApplicationBuilder builder);
    public Task ConfigureApplication(WebApplicationBuilder builder, WebApplication webApplication);
}