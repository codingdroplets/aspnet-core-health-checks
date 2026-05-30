# aspnet-core-health-checks

> A production-ready guide to implementing **ASP.NET Core Health Checks** — with custom IHealthCheck classes, liveness/readiness probes, Kubernetes-style endpoints, and a rich JSON response writer.

[![Visit CodingDroplets](https://img.shields.io/badge/Website-codingdroplets.com-blue?style=for-the-badge&logo=google-chrome&logoColor=white)](https://codingdroplets.com/)
[![YouTube](https://img.shields.io/badge/YouTube-CodingDroplets-red?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/@CodingDroplets)
[![Patreon](https://img.shields.io/badge/Patreon-Support%20Us-orange?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/CodingDroplets)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-Support%20Us-yellow?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/codingdroplets)
[![GitHub](https://img.shields.io/badge/GitHub-codingdroplets-black?style=for-the-badge&logo=github&logoColor=white)](http://github.com/codingdroplets/)

---

## 🚀 Support the Channel — Join on Patreon

If this sample saved you time, consider joining our Patreon community.
You'll get **exclusive .NET tutorials, premium code samples, and early access** to new content — all for the price of a coffee.

👉 **[Join CodingDroplets on Patreon](https://www.patreon.com/CodingDroplets)**

Prefer a one-time tip? [Buy us a coffee ☕](https://buymeacoffee.com/codingdroplets)

---

## 🎯 What You'll Learn

- How to register and use the built-in **ASP.NET Core Health Checks middleware**
- How to write **custom `IHealthCheck` implementations** (memory, disk, startup, external API)
- How to expose **separate liveness, readiness, and detail endpoints** — Kubernetes-ready
- How to produce a **rich, structured JSON health report** using a custom `ResponseWriter`
- How to use **tags** to group health checks by probe type
- How to implement a **readiness gate** (`StartupHealthCheck`) that blocks traffic until warm-up completes
- How to **unit test** health check logic and **integration test** HTTP endpoints with `WebApplicationFactory`

---

## 🗺️ Architecture Overview

```
HTTP Client / Kubernetes
        │
        ├──► GET /health           → Plain text summary (200 OK or 503)
        │
        ├──► GET /health/live      → Liveness probe (system checks only)
        │         └── MemoryHealthCheck   (tags: liveness, system)
        │         └── DiskSpaceHealthCheck (tags: liveness, system)
        │
        ├──► GET /health/ready     → Readiness probe (dependency checks)
        │         └── StartupHealthCheck  (tags: readiness)
        │         └── ExternalApiHealthCheck (tags: readiness, dependencies)
        │
        └──► GET /health/detail    → All checks, full JSON report
                  └── All four checks
                  └── Custom JSON ResponseWriter
                            │
                            └── HealthCheckResponse
                                  ├── status
                                  ├── total_duration
                                  └── entries[]
                                        ├── name
                                        ├── status
                                        ├── description
                                        ├── duration
                                        ├── data { }
                                        └── tags []
```

---

## 📋 Health Check Summary

| Check | Class | Tags | Reports Unhealthy When |
|---|---|---|---|
| Memory | `MemoryHealthCheck` | liveness, system | Working set ≥ 2× threshold |
| Disk Space | `DiskSpaceHealthCheck` | liveness, system | Free disk < 500 MB |
| Startup Gate | `StartupHealthCheck` | readiness | `MarkReady()` not yet called |
| External API | `ExternalApiHealthCheck` | readiness, dependencies | HTTP call fails or times out |

| Status | HTTP Code | Meaning |
|---|---|---|
| `Healthy` | 200 | All checks passed |
| `Degraded` | 200 | Partial issues, still serving |
| `Unhealthy` | 503 | Critical failure |

---

## 📁 Project Structure

```
aspnet-core-health-checks/
├── aspnet-core-health-checks.sln
│
├── HealthCheckDemo/                        # ASP.NET Core Web API
│   ├── HealthChecks/
│   │   ├── MemoryHealthCheck.cs            # Checks GC + working-set memory
│   │   ├── DiskSpaceHealthCheck.cs         # Checks available disk space
│   │   ├── StartupHealthCheck.cs           # Readiness gate (MarkReady pattern)
│   │   └── ExternalApiHealthCheck.cs       # Checks an external HTTP endpoint
│   ├── Models/
│   │   ├── HealthCheckResponse.cs          # Serialisable health report DTO
│   │   └── CheckStatus.cs                  # Status string constants
│   ├── Program.cs                          # DI registration + endpoint mapping
│   ├── appsettings.json                    # External API URL configuration
│   └── Properties/
│       └── launchSettings.json             # VS launch profiles
│
└── HealthCheckDemo.Tests/                  # xUnit test project
    ├── MemoryHealthCheckTests.cs           # Unit tests for memory check
    ├── DiskSpaceHealthCheckTests.cs        # Unit tests for disk check
    ├── StartupHealthCheckTests.cs          # Unit tests for startup gate
    └── HealthEndpointIntegrationTests.cs   # Integration tests via WebApplicationFactory
```

---

## 🛠️ Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0+ |
| IDE | Visual Studio 2022 / VS Code / Rider |

---

## ⚡ Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/codingdroplets/aspnet-core-health-checks.git
cd aspnet-core-health-checks

# 2. Build
dotnet build -c Release

# 3. Run the API
cd HealthCheckDemo
dotnet run

# 4. Open health endpoints in your browser
# http://localhost:5289/health
# http://localhost:5289/health/live
# http://localhost:5289/health/ready
# http://localhost:5289/health/detail
```

Visual Studio users: press **F5** — the browser opens automatically to the Swagger UI.

---

## 🔧 How It Works

### 1. Register health checks in `Program.cs`

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<MemoryHealthCheck>(
        name: "memory",
        failureStatus: HealthStatus.Degraded,
        tags: ["liveness", "system"])
    .AddCheck<DiskSpaceHealthCheck>(
        name: "disk_space",
        failureStatus: HealthStatus.Degraded,
        tags: ["liveness", "system"])
    .AddCheck<StartupHealthCheck>(
        name: "startup",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["readiness"])
    .AddCheck<ExternalApiHealthCheck>(
        name: "external_api",
        failureStatus: HealthStatus.Degraded,
        tags: ["readiness", "dependencies"]);
```

### 2. Map tagged endpoints

```csharp
// Kubernetes liveness probe — only system checks
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("liveness"),
    ResponseWriter = WriteJsonResponse
});

// Kubernetes readiness probe — only dependency checks
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("readiness"),
    ResponseWriter = WriteJsonResponse
});

// Full detail — all checks, rich JSON response
app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteJsonResponse
});
```

### 3. Implement `IHealthCheck`

```csharp
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _thresholdBytes;

    public MemoryHealthCheck(long thresholdMegabytes = 512)
        => _thresholdBytes = thresholdMegabytes * 1024L * 1024L;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        long totalBytes = Environment.WorkingSet;

        if (totalBytes >= _thresholdBytes * 2)
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Critical memory: {totalBytes / (1024 * 1024)} MB"));

        if (totalBytes >= _thresholdBytes)
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Elevated memory: {totalBytes / (1024 * 1024)} MB"));

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory OK: {totalBytes / (1024 * 1024)} MB"));
    }
}
```

### 4. Use the Startup gate pattern for readiness

```csharp
// Register as singleton so the flag persists
builder.Services.AddSingleton<StartupHealthCheck>();

// After your warm-up tasks (DB migrations, cache load, etc.):
var startupCheck = app.Services.GetRequiredService<StartupHealthCheck>();
startupCheck.MarkReady();
```

### 5. Custom JSON response writer

```csharp
static async Task WriteJsonResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new HealthCheckResponse
    {
        Status = report.Status.ToString(),
        TotalDuration = report.TotalDuration,
        Entries = report.Entries
            .Select(e => new HealthCheckEntry
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                Data = e.Value.Data.Count > 0
                    ? e.Value.Data.ToDictionary(k => k.Key, v => v.Value)
                    : null
            }).ToList()
    };

    await context.Response.WriteAsync(
        JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }));
}
```

---

## 📡 API Endpoints

| Method | Endpoint | Description | Checks Included |
|---|---|---|---|
| GET | `/health` | Plain-text summary | All |
| GET | `/health/live` | Liveness probe (JSON) | Memory, Disk Space |
| GET | `/health/ready` | Readiness probe (JSON) | Startup Gate, External API |
| GET | `/health/detail` | Full detail (JSON) | All |

### Example `/health/detail` response

```json
{
  "status": "Healthy",
  "total_duration": "00:00:00.0152345",
  "entries": [
    {
      "name": "disk_space",
      "status": "Healthy",
      "description": "Disk space is fine: 42.3 GB free on /",
      "duration": "00:00:00.0012300",
      "data": {
        "free_gb": 42.3,
        "total_gb": 100.0,
        "used_percent": 57.7
      },
      "tags": ["liveness", "system"]
    },
    {
      "name": "memory",
      "status": "Healthy",
      "description": "Memory usage is within limits: 48 MB",
      "duration": "00:00:00.0003100",
      "data": {
        "allocated_bytes": 12345678,
        "working_set_bytes": 50331648,
        "threshold_bytes": 536870912,
        "threshold_mb": 512
      },
      "tags": ["liveness", "system"]
    }
  ]
}
```

---

## 🧪 Running Tests

```bash
dotnet test -c Release
```

| Test Class | Tests | Covers |
|---|---|---|
| `MemoryHealthCheckTests` | 3 | Healthy / Degraded-or-Unhealthy / data keys |
| `DiskSpaceHealthCheckTests` | 2 | No-throw on real disk / data keys |
| `StartupHealthCheckTests` | 3 | Before MarkReady / after MarkReady / idempotency |
| `HealthEndpointIntegrationTests` | 7 | HTTP status codes + JSON body for all 4 endpoints |
| **Total** | **15** | All core scenarios covered |

---

## 🤔 Key Concepts

### Why use health checks?

| Without Health Checks | With Health Checks |
|---|---|
| Load balancer sends traffic to unhealthy pod | Kubernetes removes unhealthy pod from rotation |
| OOM crash causes 5xx storm | Degraded status lets ops team act proactively |
| Slow external dependency silently causes timeouts | Readiness probe holds traffic until dependency recovers |
| Manual uptime monitoring | Automated, granular observability |

### Liveness vs Readiness vs Startup

| Probe Type | Kubernetes Use | What It Checks | Failure Action |
|---|---|---|---|
| **Liveness** | Is the container dead/stuck? | Memory, CPU, deadlocks | Restart the container |
| **Readiness** | Is the app ready for traffic? | Dependencies, warm-up | Remove from load balancer |
| **Startup** | Has the app finished starting? | One-time init gate | Postpone liveness checks |

### `HealthStatus` values

| Status | Numeric | Meaning |
|---|---|---|
| `Healthy` | 2 | All good |
| `Degraded` | 1 | Partial issues, still functioning |
| `Unhealthy` | 0 | Critical failure |

> The **worst** status across all registered checks determines the overall report status.

---

## 🏷️ Technologies Used

- **ASP.NET Core 10** — Web framework
- **Microsoft.Extensions.Diagnostics.HealthChecks** — Built-in health check middleware
- **xUnit** — Unit testing framework
- **Microsoft.AspNetCore.Mvc.Testing** — In-process integration testing
- **System.Text.Json** — Custom JSON response serialisation

---

## 📚 References

- [Health checks in ASP.NET Core — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [IHealthCheck interface — Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.ihealthcheck)
- [Kubernetes liveness and readiness probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🔗 Connect with CodingDroplets

| Platform | Link |
|----------|------|
| 🌐 Website | https://codingdroplets.com/ |
| 📺 YouTube | https://www.youtube.com/@CodingDroplets |
| 🎁 Patreon | https://www.patreon.com/CodingDroplets |
| ☕ Buy Me a Coffee | https://buymeacoffee.com/codingdroplets |
| 💻 GitHub | http://github.com/codingdroplets/ |

> **Want more samples like this?** [Support us on Patreon](https://www.patreon.com/CodingDroplets) or [buy us a coffee ☕](https://buymeacoffee.com/codingdroplets) — every bit helps keep the content coming!
