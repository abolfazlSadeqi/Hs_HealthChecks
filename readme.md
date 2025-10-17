# Hs_HealthChecks

**Hs_HealthChecks** is a lightweight .NET Core framework for performing **startup health checks** in your application.  
It allows you to validate critical dependencies such as databases, caches, message brokers, and external APIs **before the app starts**. This ensures that your application does not start if essential services are not available.

---

## Features

- Health checks for **SQL Server**, **MongoDB**, **Redis**, and external APIs.
- Configurable **retry mechanism**, **timeout**, and **parallelism**.
- Detailed **logging with Serilog** (including exceptions and stack traces).
- Fail-fast startup: stops the application if critical dependencies fail.
- Extensible: easy to add new health checks like RabbitMQ, PostgreSQL, etc.

---

## How to Use

1. Install packages:

2.Register startup health checks in Program.cs:

```
using Hs_HealthChecks.Extensions;
using Hs_HealthChecks.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure health checks
builder.Services.AddStartupHealthChecks(hc =>
{
    hc.AddSqlServer(builder.Configuration.GetConnectionString("SqlMain")!, name: "SQL:MainDb");
    hc.AddSqlServer(builder.Configuration.GetConnectionString("SqlReporting")!, name: "SQL:ReportingDb");
    hc.AddMongoDb(sp => builder.Configuration.GetConnectionString("MongoLogs")!, name: "Mongo:Logs");
    hc.AddRedis(builder.Configuration.GetConnectionString("RedisCache")!, name: "Redis:Cache");
    hc.AddUrlGroup(new Uri("https://example.com/api/ping"), name: "ExternalAPI");
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run health checks before app starts
await app.Services.ValidateStartupHealthChecksAsync(new HealthCheckRunnerOptions
{
    RetryCount = 3,
    DelaySeconds = 5,
    TimeoutSeconds = 10,
    MaxParallelism = 4
});
```

Models
HealthCheckRunnerOptions

Defines how startup health checks should behave:

```

public class HealthCheckRunnerOptions
{
    public int RetryCount { get; set; } = 3;        // Number of retry attempts
    public int DelaySeconds { get; set; } = 5;      // Delay between retries
    public int TimeoutSeconds { get; set; } = 10;   // Timeout per health check
    public int MaxParallelism { get; set; } = 4;    // Max parallel checks
}
```


HealthCheckResultInfo

Represents detailed results for each dependency:

```

public class HealthCheckResultInfo
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string? Description { get; set; }
    public Exception? Exception { get; set; }
}
```


Services
StartupHealthCheckRunner

The main service that executes all registered health checks with retries and logging:

```

public class StartupHealthCheckRunner
{
    private readonly HealthCheckService _healthCheckService;

    public StartupHealthCheckRunner(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task RunAsync(HealthCheckRunnerOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new HealthCheckRunnerOptions();

        for (int attempt = 1; attempt <= options.RetryCount; attempt++)
        {
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken: cancellationToken);

            if (result.Status == HealthStatus.Healthy)
            {
                Log.Information("✅ All dependencies are healthy!");
                return;
            }

            if (attempt < options.RetryCount)
            {
                Log.Information("Attempt {Attempt}/{RetryCount} failed. Retrying in {DelaySeconds}s...", attempt, options.RetryCount, options.DelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(options.DelaySeconds), cancellationToken);
            }
            else
            {
                Log.Error("❌ Startup HealthCheck failed after {RetryCount} attempts.", options.RetryCount);

                foreach (var entry in result.Entries)
                {
                    Log.Error("Service: {Name}, Status: {Status}, Description: {Description}, Exception: {Exception}, StackTrace: {StackTrace}",
                        entry.Key,
                        entry.Value.Status,
                        entry.Value.Description,
                        entry.Value.Exception?.Message,
                        entry.Value.Exception?.StackTrace);
                }

                throw new Exception("Startup HealthCheck failed. Application cannot start.");
            }
        }
    }
}
```


Extensions
ServiceCollectionExtensions

Registers the runner and provides a helper method to validate health checks:

```

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStartupHealthChecks(this IServiceCollection services, Action<IHealthChecksBuilder> configureHealthChecks)
    {
        var builder = services.AddHealthChecks();
        configureHealthChecks(builder);

        services.AddSingleton<StartupHealthCheckRunner>();
        return services;
    }

    public static async Task ValidateStartupHealthChecksAsync(this IServiceProvider services, HealthCheckRunnerOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<StartupHealthCheckRunner>();
        await runner.RunAsync(options, cancellationToken);
    }
}

```
