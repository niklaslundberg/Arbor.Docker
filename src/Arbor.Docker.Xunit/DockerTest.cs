using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using Xunit;

namespace Arbor.Docker.Xunit;

[Collection(nameof(DockerTest))]
public abstract class DockerTest : IAsyncLifetime
{
    private readonly ILogger _logger;

    /// <summary>
    /// Will dispose the provided logger on this async disposal
    /// </summary>
    /// <param name="logger"></param>
    protected DockerTest(ILogger logger) => _logger = logger;

    public DockerContext? Context { get; private set; }

    public virtual async ValueTask DisposeAsync()
    {
        if (Context is { })
        {
            await Context.SafeDisposeAsync();
        }

        await _logger.SafeDisposeAsync();
    }

    public virtual async ValueTask InitializeAsync()
    {
        var containers = new List<ContainerSettings>();

        await foreach (var container in AddContainersAsync())
        {
            containers.Add(container);
        }

        Context = await DockerContext.CreateContextAsync(containers, _logger);

        await Context.ContainerTask;
    }

    protected virtual async IAsyncEnumerable<ContainerSettings> AddContainersAsync()
    {
        yield break;
    }
}