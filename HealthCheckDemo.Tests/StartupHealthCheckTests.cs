using HealthCheckDemo.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.Tests;

/// <summary>
/// Unit tests for <see cref="StartupHealthCheck"/>.
/// </summary>
public class StartupHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_BeforeMarkReady_ReturnsUnhealthy()
    {
        // Arrange
        var check = new StartupHealthCheck();
        var context = CreateContext(check);

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains("warming up", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_AfterMarkReady_ReturnsHealthy()
    {
        // Arrange
        var check = new StartupHealthCheck();
        check.MarkReady();
        var context = CreateContext(check);

        // Act
        var result = await check.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains("ready", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkReady_IsIdempotent()
    {
        // Arrange
        var check = new StartupHealthCheck();

        // Act — call MarkReady twice; second call must not throw or reset state
        check.MarkReady();
        check.MarkReady();

        var context = CreateContext(check);
        var result = await check.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static HealthCheckContext CreateContext(IHealthCheck check)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                name: "startup",
                instance: check,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }
}
