namespace HealthCheckDemo.Models;

/// <summary>Strongly-typed status values that mirror <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus"/>.</summary>
public static class CheckStatus
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string Unhealthy = "Unhealthy";
}
