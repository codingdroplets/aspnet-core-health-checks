using System.Text.Json;
using HealthCheckDemo.HealthChecks;
using HealthCheckDemo.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────────────────────
// Services
// ──────────────────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// HttpClient factory — used by ExternalApiHealthCheck
builder.Services.AddHttpClient("health-check");

// Register StartupHealthCheck as a singleton so MarkReady() state is shared
builder.Services.AddSingleton<StartupHealthCheck>();

// ──────────────────────────────────────────────────────────────────────────────
// Health Checks
//
// Tags are used to split the single /health endpoint into three distinct probes
// that Kubernetes and Docker can call independently:
//
//   /health/live    → liveness  (is the process up and not deadlocked?)
//   /health/ready   → readiness (is the app ready to serve traffic?)
//   /health/detail  → detailed  (everything, for dashboards / humans)
// ──────────────────────────────────────────────────────────────────────────────

builder.Services.AddHealthChecks()

    // Liveness: simple memory check — if the process is using too much RAM
    // it may be about to OOM-crash, so restart it.
    .AddCheck<MemoryHealthCheck>(
        name: "memory",
        failureStatus: HealthStatus.Degraded,
        tags: ["liveness", "system"])

    // Liveness: disk space
    .AddCheck<DiskSpaceHealthCheck>(
        name: "disk_space",
        failureStatus: HealthStatus.Degraded,
        tags: ["liveness", "system"])

    // Readiness: startup gate — not ready until MarkReady() is called
    .AddCheck<StartupHealthCheck>(
        name: "startup",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["readiness"])

    // Readiness: external API dependency
    .AddCheck<ExternalApiHealthCheck>(
        name: "external_api",
        failureStatus: HealthStatus.Degraded,
        tags: ["readiness", "dependencies"]);

// ──────────────────────────────────────────────────────────────────────────────
// Pipeline
// ──────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Simulate startup warm-up: mark the app ready immediately in this demo.
// In a real application, call this after completing DB migrations,
// loading caches, or any other warm-up tasks.
var startupCheck = app.Services.GetRequiredService<StartupHealthCheck>();
startupCheck.MarkReady();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ── Health endpoints ──────────────────────────────────────────────────────────

// Kubernetes LIVENESS probe — only checks system-level health
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("liveness"),
    ResponseWriter = WriteJsonResponse
});

// Kubernetes READINESS probe — only checks readiness-tagged checks
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("readiness"),
    ResponseWriter = WriteJsonResponse
});

// Detailed endpoint — all checks, rich JSON response for monitoring dashboards
app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteJsonResponse
});

// Summary endpoint — all checks, plain status only (for quick uptime pings)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();

// ──────────────────────────────────────────────────────────────────────────────
// JSON response writer — returns a structured HealthCheckResponse payload
// ──────────────────────────────────────────────────────────────────────────────

static async Task WriteJsonResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new HealthCheckResponse
    {
        Status = report.Status.ToString(),
        TotalDuration = report.TotalDuration,
        Entries = report.Entries
            .OrderBy(e => e.Key)
            .Select(e => new HealthCheckEntry
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                Exception = e.Value.Exception?.Message,
                Data = e.Value.Data.Count > 0
                    ? e.Value.Data.ToDictionary(k => k.Key, v => v.Value)
                    : null,
                Tags = e.Value.Tags.ToList()
            })
            .ToList()
    };

    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    });

    await context.Response.WriteAsync(json);
}

// Make Program partial for WebApplicationFactory in tests
public partial class Program { }
