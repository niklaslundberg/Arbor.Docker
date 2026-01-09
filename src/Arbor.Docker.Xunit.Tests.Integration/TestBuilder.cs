using System.Net.Http;
using System.Threading.Tasks;
using Arbor.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Arbor.Docker.Xunit.Tests.Integration;

public class TestBuilder : CustomApplicationBuilder

{
    protected override Task BeforeBuildApplication()
    {
        if (_useTestServer)
        {
            _applicationBuilder.WebApplicationBuilder.WebHost.UseTestServer();
        }

        return Task.CompletedTask;
    }

    private readonly CustomApplicationBuilder _applicationBuilder;

    private bool _useTestServer;

    public TestBuilder(CustomApplicationBuilder applicationBuilder) : base(applicationBuilder.WebApplicationBuilder, applicationBuilder.Application) =>
        _applicationBuilder = applicationBuilder;

    public HttpClient GetApiClient() => WebApplication.GetTestClient();


    public void UseTestServer() => _useTestServer = true;
}