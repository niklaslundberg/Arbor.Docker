using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;

namespace Arbor.Docker;

internal static class DisposeExtensions
{
    public static async Task SafeDisposeAsync(this object? instance, ILogger? logger = null)
    {
        logger ??= Logger.None ?? throw new ArgumentException("Missing logger");

        if (instance is null)
        {
            return;
        }

        if (instance is IAsyncDisposable asyncDisposable)
        {
            try
            {
                await asyncDisposable.DisposeAsync();

                return;
            }
            catch (TaskCanceledException ex)
            {
                logger.Warning(ex, "Could not async dispose {Instance}, cancelled", instance);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Could not async dispose {Instance}", instance);
            }
        }

        if (instance is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Could not dispose {Instance}", instance);
            }
        }
    }
}