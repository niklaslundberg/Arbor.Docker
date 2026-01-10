using System.Threading;
using System.Threading.Tasks;
using Arbor.Hosting;

namespace Arbor.Docker;

public class WebApiResource(CustomApplicationBuilder customApplicationBuilder) : IResource
{
    internal CustomApplicationBuilder ApplicationBuilder { get; } = customApplicationBuilder;

    public async ValueTask DisposeAsync() => await ApplicationBuilder.DisposeAsync();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ApplicationBuilder.Initialize(cancellationToken);
        await ApplicationBuilder.StartAsync();
    }
}