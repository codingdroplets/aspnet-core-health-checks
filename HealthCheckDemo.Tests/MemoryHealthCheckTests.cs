using HealthCheckDemo.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.Tests;

/// <summary>
/// Unit tests for <see cref="MemoryHealthCheck"/>.
/// </summary>
public class MemoryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithVeryHighThreshold_ReturnsHealthy()
    {
        // Arrange: threshold so high we can never exceed it in a unit test
        var check = new MemoryHealthCheck(thresholdMegabytes: 1024 * 100); // 100 GB
        var context = CreateContext(check);

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.True(result.Data.ContainsKey("working_set_bytes"));
        Assert.True(result.Data.ContainsKey("threshold_mb"));
    }

    [Fact]
    public async Task CheckHealthAsync_WithVeryLowThreshold_ReturnsDegradedOrUnhealthy()
    {
        // Arrange: threshold of 1 byte — the process is always over this
        var check = new MemoryHealthCheck(thresholdMegabytes: 0);
        var context = CreateContext(check);

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert: must be Degraded or Unhealthy (never Healthy)
        Assert.True(
            result.Status == HealthStatus.Degraded || result.Status == HealthStatus.Unhealthy,
            $"Expected Degraded or Unhealthy but got {result.Status}");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsData_WithExpectedKeys()
    {
        // Arrange
        var check = new MemoryHealthCheck();
        var context = CreateContext(check);

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert all expected data keys are present
        Assert.True(result.Data.ContainsKey("allocated_bytes"));
        Assert.True(result.Data.ContainsKey("working_set_bytes"));
        Assert.True(result.Data.ContainsKey("threshold_bytes"));
        Assert.True(result.Data.ContainsKey("threshold_mb"));
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static HealthCheckContext CreateContext(IHealthCheck check)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                name: "memory",
                instance: check,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }
}
