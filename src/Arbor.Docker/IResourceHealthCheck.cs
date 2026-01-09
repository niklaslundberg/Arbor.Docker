using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Docker;


public interface IResourceHealthCheck
{
    public Task<HealthCheckStatus> CheckHealthAsync(CancellationToken cancellationToken);

}