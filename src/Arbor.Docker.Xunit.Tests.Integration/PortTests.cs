using System;
using AwesomeAssertions;
using Xunit;

namespace Arbor.Docker.Xunit.Tests.Integration;

public class PortTests
{
    [Fact]
    public void SinglePortRange()
    {
        var portRange = new PortRange(1);

        Assert.Equal(1, portRange.Start);
        Assert.Equal(1, portRange.End);
        Assert.Equal(1, portRange.Length);
    }

    [Fact]
    public void MultiplePortRange()
    {
        var portRange = new PortRange(1, 3);

        Assert.Equal(1, portRange.Start);
        Assert.Equal(3, portRange.End);
        Assert.Equal(3, portRange.Length);
    }

    [Fact]
    public void SinglePortMapping()
    {
        var mapping = PortMapping.MapSinglePort(8080, 80);

        Assert.Equal(1, mapping.HostPorts.Length);
        Assert.Equal(1, mapping.ContainerPorts.Length);
        Assert.Equal(8080, mapping.HostPorts.Start);
        Assert.Equal(8080, mapping.HostPorts.End);
        Assert.Equal(80, mapping.ContainerPorts.Start);
        Assert.Equal(80, mapping.ContainerPorts.End);
    }

    [Fact]
    public void SinglePortMappingWithSamePort()
    {
        var mapping = PortMapping.MapSinglePort(80, 80);

        Assert.Equal(1, mapping.HostPorts.Length);
        Assert.Equal(1, mapping.ContainerPorts.Length);
        Assert.Equal(80, mapping.HostPorts.Start);
        Assert.Equal(80, mapping.HostPorts.End);
        Assert.Equal(80, mapping.ContainerPorts.Start);
        Assert.Equal(80, mapping.ContainerPorts.End);
    }

    [Fact]
    public void MultiplePortRangeMapping()
    {
        var hostPorts = new PortRange(8080, 8082);
        var containerPorts = new PortRange(80, 82);
        var mapping = new PortMapping(hostPorts, containerPorts);

        Assert.Equal(3, mapping.HostPorts.Length);
        Assert.Equal(3, mapping.ContainerPorts.Length);
        Assert.Equal(8080, mapping.HostPorts.Start);
        Assert.Equal(8082, mapping.HostPorts.End);
        Assert.Equal(80, mapping.ContainerPorts.Start);
        Assert.Equal(82, mapping.ContainerPorts.End);
    }

    [Fact]
    public void GetAvailablePortShouldReturnPort()
    {
        var portHelper = new PortHelper();
        using var availablePortA = portHelper.GetAvailablePort(19000);
        using var availablePortB = portHelper.GetAvailablePort(19000);

        availablePortA.Should().NotBeNull();
        availablePortB.Should().NotBeNull();

        availablePortA.Port.Should().NotBe(availablePortB.Port);
    }

    [Fact]
    public void GetAvailablePortShouldThrowIfNotFound()
    {
        var portHelper = new PortHelper();
        using var availablePortA = portHelper.GetAvailablePort(ushort.MaxValue);
        var getPort = () => portHelper.GetAvailablePort(ushort.MaxValue);

        getPort.Should().Throw<InvalidOperationException>();
    }
}