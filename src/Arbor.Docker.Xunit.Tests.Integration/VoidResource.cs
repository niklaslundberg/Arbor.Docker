using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker.Xunit.Tests.Integration;

internal class VoidResource : IResource
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}