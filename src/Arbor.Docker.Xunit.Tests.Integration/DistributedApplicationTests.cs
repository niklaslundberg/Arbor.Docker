using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arbor.Docker.SampleApi;
using Arbor.Docker.WebApi;
using Arbor.Hosting;
using Arbor.Processing;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog.Events;
using Xunit;

namespace Arbor.Docker.Xunit.Tests.Integration;

public class DistributedApplicationTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task RunEmptyShouldExitSuccessfully()
    {
        var logger = testOutputHelper.ToLogger();
        await using var distributedApplication = await DistributedApplication.CreateBuilder([]).BuildAsync(TestContext.Current.CancellationToken);

        var events = new List<IApplicationEvent>();

        using var subscription = distributedApplication.Events.Subscribe(async (@event, _) =>
            events.Add(@event));

        ExitCode exitCode = await distributedApplication.RunAsync(TestContext.Current.CancellationToken);

        logger.Information("Application events {@Events}", events);

        exitCode.Should().Be(ExitCode.Success);
    }

    [Fact]
    public async Task StartStopMultipleTimesShouldExitSuccessfully()
    {
        var logger = testOutputHelper.ToLogger();
        await using var distributedApplication = await DistributedApplication.CreateBuilder([]).BuildAsync(TestContext.Current.CancellationToken);

        var events = new List<IApplicationEvent>();

        using var subscription = distributedApplication.Events.Subscribe(async (@event, token) =>
            events.Add(@event));

        var start1 = distributedApplication.StartAsync(TestContext.Current.CancellationToken);
        var start2 = distributedApplication.StartAsync(TestContext.Current.CancellationToken);

        await Task.WhenAll(start1, start2);

        var stop1 = distributedApplication.StopAsync(TestContext.Current.CancellationToken);
        var stop2 = distributedApplication.StopAsync(TestContext.Current.CancellationToken);

        await Task.WhenAll(stop1, stop2);
        logger.Information("Application events {@Events}", events);
    }

    [Fact]
    public async Task ContainerApplicationShouldExitSuccessfully()
    {
        var portMappings = new[] { PortMapping.MapSinglePort(36000, 6379) };
        var settings = new ContainerSettings(
            "redis",
            "redistest",
            portMappings,
            entryPoint: ["redis-server", "--appendonly yes"]
        );


        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);

        distributedApplicationBuilder.AddResource("redis", new ContainerResource(settings));

        await using var distributedApplication = await distributedApplicationBuilder.BuildAsync(TestContext.Current.CancellationToken);

        await distributedApplication.StartAsync(TestContext.Current.CancellationToken);

        var redisCacheOptions = new RedisCacheOptions { Configuration = "localhost:36000" };
        IDistributedCache distributedCache =
            new RedisCache(new OptionsWrapper<RedisCacheOptions>(redisCacheOptions));

        await distributedCache.SetStringAsync("test", "hello world", token: TestContext.Current.CancellationToken);

        string? cachedValue = await distributedCache.GetStringAsync("test", token: TestContext.Current.CancellationToken);

        Assert.Equal("hello world", cachedValue);

        await distributedApplication.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ContainerApplicationWithDependencyShouldExitSuccessfully()
    {


        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var logger = testOutputHelper.ToLogger();
        distributedApplicationBuilder.AddLogging(logger);

        var voidResource = new VoidResource();
        var resourceReference = distributedApplicationBuilder.AddResource("void", voidResource);

        var portMappings = new[] { PortMapping.MapSinglePort(36000, 6379) };
        var settings = new ContainerSettings(
            "redis",
            "redistest",
            portMappings,
            entryPoint: ["redis-server", "--appendonly yes"]
        );
        var containerResourceBuilder = distributedApplicationBuilder.AddContainer("redis", settings);
        containerResourceBuilder.DependsOn(resourceReference);

        logger.Information("Distributed application {AppBuilder}", distributedApplicationBuilder);

        await using var distributedApplication = await distributedApplicationBuilder.BuildAsync(TestContext.Current.CancellationToken);

        await distributedApplication.StartAsync(TestContext.Current.CancellationToken);

        var redisCacheOptions = new RedisCacheOptions { Configuration = "localhost:36000" };
        IDistributedCache distributedCache =
            new RedisCache(new OptionsWrapper<RedisCacheOptions>(redisCacheOptions));

        await distributedCache.SetStringAsync("test", "hello world", token: TestContext.Current.CancellationToken);

        string? cachedValue = await distributedCache.GetStringAsync("test", token: TestContext.Current.CancellationToken);

        Assert.Equal("hello world", cachedValue);

        await distributedApplication.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ContainerApplicationWaitForShouldExitSuccessfully()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var logger = testOutputHelper.ToLogger();
        distributedApplicationBuilder.AddLogging(logger);

        var voidResource = new VoidResource();
        var resourceReference = distributedApplicationBuilder.AddResource("void", voidResource);

        var portMappings = new[] { PortMapping.MapSinglePort(36000, 6379) };
        var settings = new ContainerSettings(
            "redis",
            "redistest",
            portMappings,
            entryPoint: ["redis-server", "--appendonly yes"]
        );
        var containerResourceBuilder = distributedApplicationBuilder.AddContainer("redis", settings);
        containerResourceBuilder.WaitFor(resourceReference);

        logger.Information("Distributed application {AppBuilder}", distributedApplicationBuilder);

        await using var distributedApplication = await distributedApplicationBuilder.BuildAsync(TestContext.Current.CancellationToken);

        await distributedApplication.StartAsync(TestContext.Current.CancellationToken);

        var redisCacheOptions = new RedisCacheOptions { Configuration = "localhost:36000" };
        IDistributedCache distributedCache =
            new RedisCache(new OptionsWrapper<RedisCacheOptions>(redisCacheOptions));

        await distributedCache.SetStringAsync("test", "hello world", token: TestContext.Current.CancellationToken);

        string? cachedValue = await distributedCache.GetStringAsync("test", token: TestContext.Current.CancellationToken);

        Assert.Equal("hello world", cachedValue);

        await distributedApplication.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CyclesShouldNotBeAbleToBuild()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var logger = testOutputHelper.ToLogger();
        distributedApplicationBuilder.AddLogging(logger);

        var resourceA = new VoidResource();
        var resourceReferenceA = distributedApplicationBuilder.AddResource("a", resourceA);

        var resourceB = new VoidResource();
        var resourceReferenceB = distributedApplicationBuilder.AddResource("b", resourceB);

        resourceReferenceB.DependsOn(resourceReferenceA);
        resourceReferenceA.DependsOn(resourceReferenceB);

        distributedApplicationBuilder.HasCycles().Should().BeTrue();

        var build = () => distributedApplicationBuilder.BuildAsync();

        await build.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartCyclesShouldNotBeAbleToBuild()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var logger = testOutputHelper.ToLogger();
        distributedApplicationBuilder.AddLogging(logger);

        var resourceA = new VoidResource();
        var resourceReferenceA = distributedApplicationBuilder.AddResource("a", resourceA);

        var resourceB = new VoidResource();
        var resourceReferenceB = distributedApplicationBuilder.AddResource("b", resourceB);

        resourceReferenceB.DependsOn(resourceReferenceA);
        resourceReferenceA.DependsOn(resourceReferenceB);
        resourceReferenceA.WaitFor(resourceReferenceB);
        resourceReferenceB.WaitFor(resourceReferenceA);

        distributedApplicationBuilder.HasCycles().Should().BeTrue();

        var build = () => distributedApplicationBuilder.BuildAsync();

        await build.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void SelfReference()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var resourceA = new VoidResource();
        var resourceReferenceA = distributedApplicationBuilder.AddResource("a", resourceA);

        var referenceSelf = () => resourceReferenceA.DependsOn(resourceReferenceA);

        referenceSelf.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task HttpRequestToWebApiInMemoryServer()
    {
        var builder = WebApplication.CreateBuilder([]);

        var customApplication = new CustomApiSampleApp();

        await using var applicationBuilder = new CustomApplicationBuilder(builder, customApplication);

        var testBuilder = new TestBuilder(applicationBuilder);

        testBuilder.UseTestServer();

        await testBuilder.Initialize(TestContext.Current.CancellationToken);

        await testBuilder.StartAsync();

        var httpClient = testBuilder.GetApiClient();

        using var httpResponseMessage = await httpClient.GetAsync("/", TestContext.Current.CancellationToken);

        httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WebApiWithContainer()
    {
        var logger = testOutputHelper.ToLogger(minimumLevel: LogEventLevel.Information);
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([], logger);

        distributedApplicationBuilder.ResourceEvents.Subscribe((@event, _) =>
        {
            logger.Information("Global event subscription for resources: {Event}", @event);

            return Task.CompletedTask;
        });

        var apiBuilder = WebApplication.CreateBuilder([]);

        var apiApplication = new CustomApiSampleApp();

        await using var customApiBuilder = new CustomApplicationBuilder(apiBuilder, apiApplication);
        
        var settings = new ContainerSettings(
            "redis",
            "redistest",
            [],
            entryPoint: ["redis-server", "--appendonly yes"]
        );

        var containerResourceBuilder = distributedApplicationBuilder.AddContainer("redis", settings);
        containerResourceBuilder.WithEndPoint(new DynamicContainerPortMapping(6379));
        containerResourceBuilder.ContainerResource.Events.Subscribe((s, t) =>
        {
            if (s is EndPointAllocated endPointAllocated)
            {
                _ = customApiBuilder.WebApplicationBuilder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = $"localhost:{endPointAllocated.EndPoint.HostPort}";
                    options.InstanceName = "SampleInstance";
                });
            }

            return Task.CompletedTask;
        });

        var webApiResource = distributedApplicationBuilder.AddWebApi("api", new WebApiResource(customApiBuilder));
        webApiResource.DependsOn(containerResourceBuilder.ContainerResource);
        webApiResource.WaitFor(containerResourceBuilder.ContainerResource);
        webApiResource.AddHealthCheck();

        logger.Information("Distributed application {AppBuilder}", distributedApplicationBuilder);

        await using var distributedApplication = await distributedApplicationBuilder.BuildAsync(TestContext.Current.CancellationToken);

        var apiClient = distributedApplication.GetHttpClient("api");

        await distributedApplication.StartAsync(TestContext.Current.CancellationToken);

        logger.Information("Before cache response: {Body}", await apiClient.GetStringAsync("/", TestContext.Current.CancellationToken));

        containerResourceBuilder.ContainerResource.EndPoints.Should().ContainSingle();

        var redisEndPoint = containerResourceBuilder.ContainerResource.EndPoints[0];

        var redisCacheOptions = new RedisCacheOptions { Configuration = $"localhost:{redisEndPoint.HostPort}" };
        IDistributedCache distributedCache =
            new RedisCache(new OptionsWrapper<RedisCacheOptions>(redisCacheOptions));

        await distributedCache.SetStringAsync("test", "hello world", token: TestContext.Current.CancellationToken);

        string? cachedValue = await distributedCache.GetStringAsync("test", token: TestContext.Current.CancellationToken);

        Assert.Equal("hello world", cachedValue);

        logger.Information("After cache response: {Body}", await apiClient.GetStringAsync("/", TestContext.Current.CancellationToken));

        await distributedApplication.StopAsync(TestContext.Current.CancellationToken);
    }
}