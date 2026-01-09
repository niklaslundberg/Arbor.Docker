using System;

namespace Arbor.Docker;

public sealed class PortUsage : IDisposable
{
    private readonly PortHelper _helper;
    public int Port { get; }
    internal PortUsage(PortHelper helper, int port)
    {
        _helper = helper;
        Port = port;
    }
    public void Dispose() => _helper.Return(this);
}