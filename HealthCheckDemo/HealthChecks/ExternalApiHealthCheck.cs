using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheckDemo.HealthChecks;

/// <summary>
/// Simulates an external API dependency health check.
/// In production this would call a real HTTP endpoint; here we use a mock
/// so the project builds and tests without network dependencies.
/// </summary>
public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalApiHealthCheck> _logger;

    public ExternalApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExternalApiHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Read target URL from configuration; fall back to a well-known placeholder
        var targetUrl = _configuration["HealthChecks:ExternalApiUrl"]
                        ?? "https://httpbin.org/status/200";

        var data = new Dictionary<string, object>
        {
            { "target_url", targetUrl }
        };

        try
        {
            using var client = _httpClientFactory.CreateClient("health-check");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await client.GetAsync(targetUrl, cts.Token);

            data["status_code"] = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy(
                    description: $"External API responded with {(int)response.StatusCode}",
                    data: data);
            }

            _logger.LogWarning("External API health check returned {StatusCode}", response.StatusCode);
            return HealthCheckResult.Degraded(
                description: $"External API returned non-success status {(int)response.StatusCode}",
                data: data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API health check failed with HttpRequestException");
            data["error"] = ex.Message;
            return HealthCheckResult.Unhealthy(
                description: "External API is unreachable",
                exception: ex,
                data: data);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "External API health check timed out");
            data["error"] = "Request timed out after 5 seconds";
            return HealthCheckResult.Unhealthy(
                description: "External API health check timed out",
                exception: ex,
                data: data);
        }
    }
}
