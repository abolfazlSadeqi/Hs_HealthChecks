
using Hs_HealthChecks.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace Hs_HealthChecks.Services
{
  

   
        public class StartupHealthCheckRunner
        {
            private readonly HealthCheckService _healthCheckService;

            public StartupHealthCheckRunner(
                HealthCheckService healthCheckService)
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
                        Log.Information("All dependencies are healthy!");
                        return;
                    }

                    if (attempt < options.RetryCount)
                    {
                        Log.Information(
                            "Attempt {Attempt}/{RetryCount} failed. Retrying in {DelaySeconds}s...",
                            attempt, options.RetryCount, options.DelaySeconds);

                        await Task.Delay(TimeSpan.FromSeconds(options.DelaySeconds), cancellationToken);
                    }
                    else
                    {
                        Log.Information(
                            " Startup HealthCheck failed after {RetryCount} attempts. Application cannot start.",
                            options.RetryCount);

                        // لاگ کامل هر سرویس قبل از پرتاب Exception
                        foreach (var entry in result.Entries)
                        {
                            Log.Error(
                                "Service: {Name}, Status: {Status}, Description: {Description}, Exception: {Exception}, StackTrace: {StackTrace}",
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
    }



