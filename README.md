# Arbor.Docker

Arbor.Docker is a lightweight .NET library for orchestrating Docker containers and ASP.NET Core web API applications, primarily designed for integration testing and local development. It is inspired by, but intentionally simpler than, [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

For a detailed side-by-side comparison with .NET Aspire, see [ASPIRE_COMPARISON.md](ASPIRE_COMPARISON.md).

## What It Does

Arbor.Docker lets you define a set of *resources* (Docker containers, ASP.NET Core web APIs) that need to run together, declare the dependencies and start-order between them, and then start/stop them as a unit. It is particularly useful for integration tests that require real infrastructure such as Redis, message brokers, or SMTP servers.

## How It Works

### Core Concepts

| Concept | Description |
|---|---|
| `DistributedApplication` | The running orchestrator that manages the lifecycle of all resources. |
| `DistributedApplicationBuilder` | Fluent builder used to register resources before building the application. |
| `ContainerResource` | A Docker container resource described by a `ContainerSettings` object. |
| `WebApiResource` | An in-process ASP.NET Core web API resource. |
| `ContainerSettings` | Configuration for a Docker container: image name, container name, port mappings, environment variables, entry point overrides, and additional arguments. |
| `PortMapping` / `DynamicContainerPortMapping` | Static or dynamically allocated host-to-container port mappings. |
| `PortHelper` | Scans the OS for free TCP/UDP ports and allocates them dynamically to avoid conflicts when running tests in parallel. |
| `ApplicationEvents` / `ResourceEvents` | Observable event streams for application-level and resource-level lifecycle events. |

### Lifecycle

```
CreateBuilder() → AddContainer() / AddWebApi() → BuildAsync() → StartAsync() → RunAsync() → StopAsync() → DisposeAsync()
```

1. **Builder phase** – Register resources and their dependencies using `AddContainer`, `AddWebApi`, or the generic `AddResource`. Specify dependency ordering with `DependsOn` (must start before) and `WaitFor` (must start *and* be healthy before this resource starts).
2. **Build phase** – `BuildAsync()` validates the dependency graph for cycles and initialises each resource (e.g. allocates dynamic ports for web API resources).
3. **Start phase** – `StartAsync()` starts resources in dependency order. Health checks run in the background until all resources that have one report `Healthy`.
4. **Run phase** – `RunAsync()` keeps the application running until it is stopped or all resources have finished.
5. **Stop/Dispose phase** – `StopAsync()` / `DisposeAsync()` stops resources in reverse start order and removes Docker containers.

### Dependency Ordering

```csharp
var redis = builder.AddContainer("redis", settings);
var api = builder.AddWebApi("api", webApiResource);

// api will not start until redis has started
api.DependsOn(redis.ContainerResource);

// api will not start until redis has started *and* is healthy
api.WaitFor(redis.ContainerResource);
```

Circular dependencies are detected at build time and throw an `InvalidOperationException`.

### Health Checks

A resource can have a single health check. The built-in `HttpHealthCheck` polls an HTTP endpoint until it returns a success status.

```csharp
var api = builder.AddWebApi("api", webApiResource);
api.AddHealthCheck(); // polls GET /health
```

Custom health checks can be implemented via `IResourceHealthCheck`.

### Events

Subscribe to application-level and resource-level events to react to state changes:

```csharp
distributedApplication.Events.Subscribe(async (@event, ct) =>
{
    // ApplicationStarting, ApplicationStarted, ApplicationStopping, ApplicationStopped, ApplicationFailed
});

builder.ResourceEvents.Subscribe(async (@event, ct) =>
{
    // ResourceInitialized, ResourceStarting, ResourceStarted, ResourceHealthy, EndPointAllocated
});
```

You can also subscribe to events on an individual resource reference:

```csharp
var redis = builder.AddContainer("redis", settings);
redis.ContainerResource.Events.Subscribe(async (@event, ct) =>
{
    if (@event is EndPointAllocated endPointAllocated)
    {
        // configure downstream services with the allocated port
    }
});
```

### Dynamic Port Allocation

Use `DynamicContainerPortMapping` to let Arbor.Docker pick a free host port for a container port. This avoids conflicts when running multiple test suites concurrently.

```csharp
var redis = builder.AddContainer("redis", settings);
redis.WithEndPoint(new DynamicContainerPortMapping(6379));
```

After `BuildAsync()` the allocated host port is available via `EndPointAllocated` events and the `ContainerResource.EndPoints` collection.

## Quick Start

### 1. Install

```xml
<PackageReference Include="Arbor.Docker" Version="*" />
```

For xUnit integration:

```xml
<PackageReference Include="Arbor.Docker.Xunit" Version="*" />
```

### 2. Run a Container in a Test

```csharp
var portMappings = new[] { PortMapping.MapSinglePort(36379, 6379) };
var settings = new ContainerSettings(
    imageName: "redis",
    containerName: "redistest",
    ports: portMappings,
    entryPoint: ["redis-server", "--appendonly yes"]
);

var builder = DistributedApplication.CreateBuilder([]);
builder.AddResource("redis", new ContainerResource(settings));

await using var app = await builder.BuildAsync();
await app.StartAsync();

// ... run your test against localhost:36379 ...

await app.StopAsync();
```

### 3. Use the xUnit Base Class

For a simpler xUnit experience, inherit from `DockerTest`:

```csharp
public class RedisTest(ITestOutputHelper output) : DockerTest(output.ToLogger())
{
    protected override async IAsyncEnumerable<ContainerSettings> AddContainersAsync()
    {
        yield return new ContainerSettings("redis", "redistest",
            new[] { PortMapping.MapSinglePort(36379, 6379) },
            entryPoint: ["redis-server", "--appendonly yes"]);
    }

    [Fact]
    public async Task CacheRoundTrip()
    {
        // Context is already started when this runs
        var cache = new RedisCache(new OptionsWrapper<RedisCacheOptions>(
            new RedisCacheOptions { Configuration = "localhost:36379" }));
        await cache.SetStringAsync("key", "value");
        Assert.Equal("value", await cache.GetStringAsync("key"));
    }
}
```

### 4. Combine a Container and an In-Process Web API

```csharp
var builder = DistributedApplication.CreateBuilder([]);

var redis = builder.AddContainer("redis",
    new ContainerSettings("redis", "redistest", [],
        entryPoint: ["redis-server", "--appendonly yes"]));
redis.WithEndPoint(new DynamicContainerPortMapping(6379));

// Wire up the allocated Redis port to the API before it starts
redis.ContainerResource.Events.Subscribe(async (@event, _) =>
{
    if (@event is EndPointAllocated ep)
        apiBuilder.Services.AddStackExchangeRedisCache(o =>
            o.Configuration = $"localhost:{ep.EndPoint.HostPort}");
});

var api = builder.AddWebApi("api", new WebApiResource(apiBuilder));
api.DependsOn(redis.ContainerResource);
api.WaitFor(redis.ContainerResource);
api.AddHealthCheck();

await using var app = await builder.BuildAsync();
await app.StartAsync();

var client = app.GetHttpClient("api");
// ... test the API ...

await app.StopAsync();
```

## Projects in This Repository

| Project | Description |
|---|---|
| `Arbor.Docker` | Core library – container/resource model, orchestrator, port allocation, events. |
| `Arbor.Docker.Xunit` | xUnit integration – `DockerTest` base class that starts containers for the test class and disposes them after. |
| `Arbor.Docker.WebApi` | Extension methods for registering ASP.NET Core web API resources (`AddWebApi`). |
| `Arbor.Hosting` | Thin hosting abstraction (`CustomApplicationBuilder`, `ICustomApplication`) used by `WebApiResource` to wire up and start in-process ASP.NET Core apps. |
| `Arbor.Docker.SampleApi` | Sample ASP.NET Core API used in integration tests. |
| `Arbor.Docker.Xunit.Tests.Integration` | Integration tests demonstrating all major scenarios. |

## Requirements

- .NET 9+
- Docker (for container resources)
- [Serilog](https://serilog.net/) (used for logging throughout)
