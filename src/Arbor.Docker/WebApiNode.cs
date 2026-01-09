using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arbor.Docker.WebApi;
using Microsoft.AspNetCore.Hosting;

namespace Arbor.Docker;

internal class WebApiNode : ResourceNode
{
    private readonly PortHelper _portHelper;

    public WebApiNode(string name, DistributedApplicationBuilder builder, PortHelper portHelper) : base(name, builder) => _portHelper = portHelper;

    internal bool UseDynamicEndPointAllocation { get; set; } = true;

    internal int DefaultStartPort = 15000;

    public override async Task InitializeAsync()
    {
        string? listenUrl = null;
        if (UseDynamicEndPointAllocation)
        {
            PortUsage availablePort = _portHelper.GetAvailablePort(DefaultStartPort);

            var resourceEndPoint = new HttpEndPoint(availablePort);

            if (HealthCheck is HttpHealthCheck httpHealthCheck)
            {
                listenUrl = $"http://localhost:{resourceEndPoint.PortUsage.Port}";
                httpHealthCheck.Client = new HttpClient
                {
                    BaseAddress = new Uri(listenUrl)
                };
            }

            await Events.Publish(new EndPointAllocated(this, resourceEndPoint));
        }

        if (Resource is WebApiResource webApiNode && listenUrl is {})
        {
            webApiNode.ApplicationBuilder.WebApplicationBuilder.WebHost.UseUrls(listenUrl);
        }
    }
}