using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HealthCheckDemo.Tests;

/// <summary>
/// Integration tests that verify the health check endpoints respond correctly
/// using the in-process <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public class HealthEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health/detail")]
    public async Task HealthEndpoints_ReturnSuccessOrDegradedStatus(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert: health checks may return 200 (Healthy/Degraded) or 503 (Unhealthy).
        // The external API check might fail in CI, so we accept either.
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status code {response.StatusCode} for {url}");
    }

    [Fact]
    public async Task DetailHealthEndpoint_ReturnsJson_WithStatusField()
    {
        // Act
        var response = await _client.GetAsync("/health/detail");
        var body = await response.Content.ReadAsStringAsync();

        // Assert: body must be valid JSON containing a "status" key
        Assert.False(string.IsNullOrWhiteSpace(body));
        Assert.Contains("\"status\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"entries\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LivenessEndpoint_ReturnsJson()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(body));
        Assert.Contains("\"status\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadinessEndpoint_ReturnsJson()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(body));
        Assert.Contains("\"status\"", body, StringComparison.OrdinalIgnoreCase);
    }
}
