# Arbor.Docker vs .NET Aspire – Similarities and Differences

This document compares Arbor.Docker with [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview), Microsoft's opinionated stack for building observable, production-ready, cloud-native distributed applications.

---

## TL;DR

| Dimension | Arbor.Docker | .NET Aspire |
|---|---|---|
| Primary goal | Lightweight test / local-dev container orchestration | Full cloud-native developer inner loop + observability |
| Setup complexity | Add a NuGet package | Dedicated AppHost project + Aspire SDK |
| Dashboard UI | ❌ | ✅ (rich real-time dashboard) |
| Cloud deployment | ❌ | ✅ (manifest → Azure, AWS, Kubernetes) |
| Built-in integrations | Manual (any Docker image) | Extensive component library (Redis, Postgres, RabbitMQ, …) |
| Service discovery | Manual (events + port info) | Automatic (env vars injected at startup) |
| Telemetry / OTel | ❌ (uses Serilog only) | ✅ (OpenTelemetry traces, metrics, logs) |
| xUnit test support | ✅ first-class | Community packages / workarounds |
| Dependencies | Minimal | Many |

---

## Similarities

### 1. `DistributedApplication` / `DistributedApplicationBuilder` pattern

Both libraries use the same conceptual entry points: a `DistributedApplicationBuilder` to register resources and a `DistributedApplication` that is built from it and then started/stopped.

```csharp
// Arbor.Docker
var builder = DistributedApplication.CreateBuilder(args);
await using var app = await builder.BuildAsync();
await app.StartAsync();

// .NET Aspire
var builder = DistributedApplication.CreateBuilder(args);
using var app = builder.Build();
await app.RunAsync();
```

### 2. Container resources

Both support running arbitrary Docker images as named resources with port mappings and environment variables.

### 3. Dependency ordering – `DependsOn` / `WaitFor`

Both libraries allow you to express that one resource must start before another (`DependsOn`) or that a resource must start and pass a health check before a dependent resource starts (`WaitFor`).

### 4. Health checks

Both support polling a resource for health before allowing dependents to proceed. Aspire uses the Microsoft health-check abstractions; Arbor.Docker provides its own `IResourceHealthCheck` / `HttpHealthCheck`.

### 5. Lifecycle events

Both expose observable streams of events for application-level and resource-level state changes (starting, started, stopping, stopped, healthy, …).

### 6. Dynamic port allocation

Both can allocate free host ports at runtime so that containers do not conflict with each other or with other processes on the developer machine.

### 7. ASP.NET Core integration

Both can manage ASP.NET Core applications as orchestrated resources. Aspire does this by launching separate processes; Arbor.Docker hosts them in-process via `WebApiResource`.

---

## Differences

### 1. Scope and goal

| | Arbor.Docker | .NET Aspire |
|---|---|---|
| **Primary audience** | Developers writing integration tests | Developers building and running cloud-native apps locally and in the cloud |
| **Deployment** | Local / CI only | Local development → cloud (Azure Container Apps, Kubernetes, etc.) |
| **Maturity** | Small, focused utility library | Microsoft-supported platform component |

### 2. Setup

**Arbor.Docker** is a plain NuGet package added to any existing project.

**Aspire** requires:
- A dedicated `*.AppHost` project using the Aspire SDK
- `*.ServiceDefaults` project for shared configuration
- The Aspire workload (`dotnet workload install aspire`)

### 3. Dashboard

**Aspire** ships with a real-time web dashboard showing resource statuses, structured logs, distributed traces, and metrics (powered by OpenTelemetry).

**Arbor.Docker** has no dashboard; output goes to Serilog sinks (console, test output, etc.).

### 4. Built-in component library

**Aspire** provides pre-built, opinionated integrations for dozens of infrastructure components (Redis, PostgreSQL, MongoDB, RabbitMQ, Azure Service Bus, NATS, Elasticsearch, …). Each integration configures the client libraries automatically.

**Arbor.Docker** ships with no pre-built integrations. Any Docker image can be used by constructing a `ContainerSettings` manually, but there is no auto-configuration of the corresponding .NET client.

### 5. Service discovery

**Aspire** injects well-known environment variables (e.g. `ConnectionStrings__redis`) into all registered projects so they can discover each other without hard-coded ports.

**Arbor.Docker** exposes allocated ports through `EndPointAllocated` events and the `ContainerResource.EndPoints` collection. Wiring a port into a downstream service's configuration is done explicitly in test/application code.

### 6. In-process vs. out-of-process hosting

**Aspire** always launches projects as separate OS processes.

**Arbor.Docker** can run ASP.NET Core web APIs *in the same process* via `WebApiResource` / `CustomApplicationBuilder`. This is particularly useful for integration tests where you want to share the test process's service container or avoid cross-process communication overhead.

### 7. Telemetry

**Aspire** includes first-class OpenTelemetry support. All managed resources emit traces, metrics, and structured logs that flow into the dashboard or any OTel-compatible backend.

**Arbor.Docker** uses [Serilog](https://serilog.net/) for logging and has no built-in tracing or metrics support.

### 8. Cloud deployment / manifest

**Aspire** can generate a deployment manifest from the `AppHost` project that tools like the Azure Developer CLI (`azd`) use to provision and deploy the entire application to the cloud.

**Arbor.Docker** has no concept of deployment or manifests; it is a purely local/CI orchestration tool.

### 9. xUnit integration

**Arbor.Docker** ships `Arbor.Docker.Xunit` with a `DockerTest` base class designed specifically for xUnit integration tests. Containers are started in `InitializeAsync` and stopped/removed in `DisposeAsync`, following xUnit's `IAsyncLifetime` contract.

**Aspire** does not have a first-party xUnit package. The community has created helpers (e.g. `Aspire.Hosting.Testing`), but they are heavier and not designed for the per-test-class lifecycle that `DockerTest` provides.

### 10. Footprint and dependencies

**Arbor.Docker** is a small library with a minimal dependency surface (Serilog, `Arbor.Processing` for running docker CLI commands).

**Aspire** is a comprehensive platform component with a large transitive dependency graph including gRPC, OpenTelemetry SDKs, Azure SDKs, and more.

---

## When to Choose Arbor.Docker

- You primarily need Docker containers for integration tests and want the simplest possible setup.
- You want in-process hosting of ASP.NET Core applications in tests.
- You are not building a cloud-native application and don't need a dashboard, telemetry, or cloud deployment.
- You want to minimise dependencies and build times.
- Your project already uses xUnit and you want first-class test lifecycle integration.

## When to Choose .NET Aspire

- You are building a full cloud-native, distributed application.
- You want a development dashboard with live logs, traces, and metrics.
- You want pre-built, opinionated integrations for common infrastructure components.
- You need to deploy to Azure or Kubernetes using a manifest-driven workflow.
- Automatic service discovery between projects is important to you.
