using Microsoft.AspNetCore.Builder;

namespace Arbor.Hosting;

public class CustomApplicationBuilder : IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    public ICustomApplication Application { get; }

    public CustomApplicationBuilder(WebApplicationBuilder webApplicationBuilder, ICustomApplication customApplication)
    {
        WebApplicationBuilder = webApplicationBuilder;
        Application = customApplication;
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await BeforeConfigureBuilder();
        await Application.ConfigureBuilder(WebApplicationBuilder);

        await BeforeBuildApplication();

        WebApplication = WebApplicationBuilder.Build();

        await BeforeConfigureApplication();

        await Application.ConfigureApplication(WebApplicationBuilder, WebApplication);
    }

    protected virtual async Task BeforeBuildApplication()
    {

    }

    public WebApplication WebApplication { get; private set; }

    public WebApplicationBuilder WebApplicationBuilder { get; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await WebApplication.RunAsync();
    }

    protected virtual async Task BeforeConfigureBuilder(CancellationToken cancellationToken = default)
    {

    }

    protected virtual async Task BeforeConfigureApplication(CancellationToken cancellationToken = default)
    {

    }

    protected virtual async Task BeforeStart(CancellationToken cancellationToken = default)
    {

    }

    public async ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource is IAsyncDisposable cancellationTokenSourceAsyncDisposable)
        {
            await cancellationTokenSourceAsyncDisposable.DisposeAsync();
        }
        else
        {
            _cancellationTokenSource.Dispose();
        }

        if (WebApplication is { })
        {
            await WebApplication.DisposeAsync();
        }
    }

    public async Task StartAsync()
    {
        await BeforeStart();
        await WebApplication.StartAsync();
    }
}