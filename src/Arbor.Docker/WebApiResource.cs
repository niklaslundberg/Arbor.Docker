using System.Threading;
using System.Threading.Tasks;
using Arbor.Hosting;

namespace Arbor.Docker.WebApi;

public class WebApiResource: IResource
{
    internal CustomApplicationBuilder ApplicationBuilder { get; }

    public WebApiResource(CustomApplicationBuilder customApplicationBuilder)
    {
        ApplicationBuilder = customApplicationBuilder;
    }
    public async ValueTask DisposeAsync()
    {
        await ApplicationBuilder.DisposeAsync();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ApplicationBuilder.Initialize(cancellationToken);
        await ApplicationBuilder.StartAsync();
    }
}