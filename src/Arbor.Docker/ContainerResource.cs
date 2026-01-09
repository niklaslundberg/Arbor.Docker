using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;

namespace Arbor.Docker;

public class ContainerResource(ContainerSettings settings, ILogger? logger = null) : IResource
{
    public ContainerSettings Settings { get; internal set; } = settings;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Context = await DockerContext.CreateContextAsync([Settings], logger ?? Logger.None);

        await Context.ContainerTask;
    }

    public DockerContext? Context { get; private set; }

    public async ValueTask DisposeAsync() => await Context.SafeDisposeAsync(logger);
}