using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.HealthChecks;

/// <summary>
/// A readiness health check that becomes Healthy only after the application
/// has fully initialised.  Use this with Kubernetes readiness probes so the
/// pod is not added to the load-balancer until the app is ready to serve traffic.
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady;

    /// <summary>
    /// Call this from a background hosted service or application startup logic
    /// once all warm-up tasks are complete.
    /// </summary>
    public void MarkReady() => _isReady = true;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_isReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "Application has completed startup and is ready to serve requests."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            "Application is still warming up. Retry in a few seconds."));
    }
}
