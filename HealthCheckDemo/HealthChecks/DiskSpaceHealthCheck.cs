using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.HealthChecks;

/// <summary>
/// Custom health check that monitors available disk space on the default drive.
/// Returns Degraded when free space falls below 1 GB and Unhealthy below 500 MB.
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private const long DegradedThresholdBytes = 1L * 1024 * 1024 * 1024;   // 1 GB
    private const long UnhealthyThresholdBytes = 500L * 1024 * 1024;        // 500 MB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/");
            long freeBytes = drive.AvailableFreeSpace;
            long totalBytes = drive.TotalSize;

            var data = new Dictionary<string, object>
            {
                { "drive_name", drive.Name },
                { "free_bytes", freeBytes },
                { "total_bytes", totalBytes },
                { "free_gb", Math.Round(freeBytes / (1024.0 * 1024 * 1024), 2) },
                { "total_gb", Math.Round(totalBytes / (1024.0 * 1024 * 1024), 2) },
                { "used_percent", Math.Round((totalBytes - freeBytes) * 100.0 / totalBytes, 1) }
            };

            if (freeBytes < UnhealthyThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Critical: only {freeBytes / (1024 * 1024)} MB free on {drive.Name}",
                    data: data));
            }

            if (freeBytes < DegradedThresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {Math.Round(freeBytes / (1024.0 * 1024 * 1024), 2)} GB free on {drive.Name}",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space is fine: {Math.Round(freeBytes / (1024.0 * 1024 * 1024), 2)} GB free on {drive.Name}",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to read disk information",
                exception: ex));
        }
    }
}
