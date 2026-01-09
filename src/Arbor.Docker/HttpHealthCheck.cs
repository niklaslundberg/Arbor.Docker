using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;

public class HttpHealthCheck(Uri uri) : IResourceHealthCheck
{
    public HttpClient? Client { get; internal set; }

    public async Task<HealthCheckStatus> CheckHealthAsync(CancellationToken cancellationToken)
    {
        if (Client is null)
        {
            throw new InvalidOperationException("HttpClient is not initialized.");
        }

        using var response = await Client.GetAsync(uri, cancellationToken);

        return response.IsSuccessStatusCode
            ? HealthCheckStatus.Healthy
            : HealthCheckStatus.Unhealthy;
    }
}