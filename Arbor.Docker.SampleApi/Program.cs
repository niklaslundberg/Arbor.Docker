using Arbor.Hosting;

namespace Arbor.Docker.SampleApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = new CustomApiSampleApp();

        var applicationBuilder = new CustomApplicationBuilder(builder, app);

        await applicationBuilder.Initialize();

        await applicationBuilder.RunAsync();
    }
}