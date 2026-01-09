namespace Arbor.Docker;

public enum HealthCheckStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3
}