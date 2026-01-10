using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;

public interface IResource : IAsyncDisposable
{
    public Task StartAsync(CancellationToken cancellationToken);
}