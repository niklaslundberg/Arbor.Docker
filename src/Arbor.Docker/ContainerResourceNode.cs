using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;

internal class ContainerResourceNode(string name, DistributedApplicationBuilder builder) : ResourceNode(name, builder)
{
    private readonly DistributedApplicationBuilder _builder = builder;

    public List<IContainerPortMapping> PortMappings { get; } = [];

    public override async Task InitializeAsync()
    {
        var portUsagesByContainerPort = PortMappings.OfType<DynamicContainerPortMapping>().ToDictionary(port => port.ContainerPort, _ => (PortUsage?)null);

        foreach (int containerPort in portUsagesByContainerPort.Keys)
        {
            portUsagesByContainerPort[containerPort] = _builder.PortHelper.GetAvailablePort(DefaultStartPort);
        }

        if (Resource is ContainerResource containerResource)
        {
            containerResource.Settings = containerResource.Settings with
            {
                Ports = [..containerResource.Settings.Ports.Concat(portUsagesByContainerPort.Select(pair => PortMapping.MapSinglePort(pair.Value!.Port, pair.Key)))]
            };
        }


        if (HealthCheck is HttpHealthCheck httpHealthCheck)
        {
            //var resourceEndPoint = new HttpEndPoint(availablePort);
            //httpHealthCheck.Client = new HttpClient
            //{
            //    BaseAddress = new Uri("http://localhost:" + resourceEndPoint.PortUsage.Port)
            //};
        }

        foreach (var (containerPort, hostPort) in portUsagesByContainerPort)
        {
            await Events.Publish(new ContainerEndPointAllocated(this,
                new(PortMapping.MapSinglePort(hostPort!.Port, containerPort))));
        }
    }

    internal int DefaultStartPort { get; set; } = 20_000;


    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
    }
}