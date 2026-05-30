namespace HealthCheckDemo.Models;

/// <summary>
/// A structured, serialisable representation of the application's health
/// report — suitable for REST API consumption and monitoring dashboards.
/// </summary>
public sealed class HealthCheckResponse
{
    /// <summary>Overall status: Healthy | Degraded | Unhealthy</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Total time taken to evaluate all registered health checks.</summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>Per-check results.</summary>
    public IReadOnlyList<HealthCheckEntry> Entries { get; init; } = [];
}

/// <summary>Result for a single named health check.</summary>
public sealed class HealthCheckEntry
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TimeSpan Duration { get; init; }

    /// <summary>Exception message, if the check threw.</summary>
    public string? Exception { get; init; }

    /// <summary>Arbitrary key/value data reported by the check.</summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>Tags registered for this check (e.g. "liveness", "readiness").</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
