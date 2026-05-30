using HealthCheckDemo.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.Tests;

/// <summary>
/// Unit tests for <see cref="DiskSpaceHealthCheck"/>.
/// </summary>
public class DiskSpaceHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_OnMachineWithDisk_DoesNotThrow()
    {
        // Arrange
        var check = new DiskSpaceHealthCheck();
        var context = CreateContext(check);

        // Act — must not throw regardless of disk state
        var result = await check.CheckHealthAsync(context);

        // Assert: result is one of the three valid statuses
        Assert.True(
            result.Status is HealthStatus.Healthy or HealthStatus.Degraded or HealthStatus.Unhealthy,
            $"Unexpected status: {result.Status}");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsData_WithExpectedKeys()
    {
        // Arrange
        var check = new DiskSpaceHealthCheck();
        var context = CreateContext(check);

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("free_bytes"));
        Assert.True(result.Data.ContainsKey("total_bytes"));
        Assert.True(result.Data.ContainsKey("free_gb"));
        Assert.True(result.Data.ContainsKey("total_gb"));
        Assert.True(result.Data.ContainsKey("used_percent"));
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static HealthCheckContext CreateContext(IHealthCheck check)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                name: "disk_space",
                instance: check,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }
}
