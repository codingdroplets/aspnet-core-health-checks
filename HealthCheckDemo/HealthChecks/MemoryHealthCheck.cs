using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.HealthChecks;

/// <summary>
/// Custom health check that reports Unhealthy when the process is consuming
/// more than a configurable amount of memory (default 512 MB).
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _thresholdBytes;

    /// <param name="thresholdMegabytes">
    /// Maximum allowed working-set size in megabytes before the check is
    /// considered Degraded.  Exceeding twice this limit is Unhealthy.
    /// </param>
    public MemoryHealthCheck(long thresholdMegabytes = 512)
    {
        _thresholdBytes = thresholdMegabytes * 1024L * 1024L;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        long usedBytes = GC.GetTotalMemory(forceFullCollection: false);
        long totalBytes = Environment.WorkingSet;

        var data = new Dictionary<string, object>
        {
            { "allocated_bytes", usedBytes },
            { "working_set_bytes", totalBytes },
            { "threshold_bytes", _thresholdBytes },
            { "threshold_mb", _thresholdBytes / (1024 * 1024) }
        };

        if (totalBytes >= _thresholdBytes * 2)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                description: $"Memory usage is critically high: {totalBytes / (1024 * 1024)} MB",
                data: data));
        }

        if (totalBytes >= _thresholdBytes)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                description: $"Memory usage is elevated: {totalBytes / (1024 * 1024)} MB",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            description: $"Memory usage is within limits: {totalBytes / (1024 * 1024)} MB",
            data: data));
    }
}
