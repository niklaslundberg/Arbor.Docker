using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Net.NetworkInformation;
using Serilog;
using Serilog.Core;

namespace Arbor.Docker;

public sealed class PortHelper
{
    private readonly ILogger _logger;

    public PortHelper(ILogger? logger = null)
    {
        _logger = logger ?? Logger.None;
    }

    internal void Return(PortUsage portUsage) =>
        _ = _usedPorts.TryRemove(portUsage.Port, out _);

    private readonly ConcurrentDictionary<int, PortUsage> _usedPorts = [];
    public PortUsage GetAvailablePort(int startingPort, int maxAttempts = 1000) =>
        GetAvailablePortInternal(startingPort, maxAttempts);

    private PortUsage GetAvailablePortInternal(int startingPort, int maxAttempts)
    {
        const ushort maxValue = ushort.MaxValue;

        if (startingPort is < 0 or > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(startingPort));
        }

        var properties = IPGlobalProperties.GetIPGlobalProperties();

        int[] tcpConnectionPorts = properties.GetActiveTcpConnections()
            .Where(n => n.LocalEndPoint.Port >= startingPort)
            .Select(n => n.LocalEndPoint.Port)
            .ToArray();

        int[] tcpListenerPorts = properties.GetActiveTcpListeners()
            .Where(ipEndPoint => ipEndPoint.Port >= startingPort)
            .Select(ipEndPoint => ipEndPoint.Port)
            .ToArray();

        int[] udpListenerPorts = properties.GetActiveUdpListeners()
            .Where(ipEndPoint => ipEndPoint.Port >= startingPort)
            .Select(ipEndPoint => ipEndPoint.Port)
            .ToArray();

        var allUsedPorts = tcpConnectionPorts
            .Concat(tcpListenerPorts)
            .Concat(udpListenerPorts)
            .ToImmutableHashSet();

        int currentAttempt = -1;

        while (currentAttempt <= maxAttempts)
        {
            currentAttempt++;
            int currentPort = startingPort + currentAttempt;

            if (_usedPorts.ContainsKey(currentPort))
            {
                continue;
            }

            if (currentPort > maxValue)
            {
                break;
            }

            var availablePort = new PortUsage(this, Enumerable
                .Range(currentPort, maxValue)
                .First(port => !allUsedPorts.Contains(port)));

            if (_usedPorts.TryAdd(availablePort.Port, availablePort))
            {
                _logger.Debug("Allocated port " + availablePort.Port);
                return availablePort;
            }
        }

        throw new InvalidOperationException(
            $"Could not get any available port starting with {startingPort} after {currentAttempt} attempts");
    }
}
