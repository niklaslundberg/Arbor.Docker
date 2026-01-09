using System;

namespace Arbor.Docker.WebApi;

public sealed class HttpEndPoint(PortUsage portUsage) : IResourceEndPoint, IDisposable
{
    public PortUsage PortUsage { get; } = portUsage;

    public void Dispose() => PortUsage.Dispose();

    public override string ToString() => $"http:{PortUsage.Port}";

    public int HostPort => PortUsage.Port;
}