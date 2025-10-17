using Hs_HealthChecks.Models;
using Hs_HealthChecks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hs_HealthChecks.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStartupHealthChecks(
            this IServiceCollection services,
            Action<IHealthChecksBuilder> configureHealthChecks)
        {
            var builder = services.AddHealthChecks();
            configureHealthChecks(builder);

            services.AddSingleton<StartupHealthCheckRunner>();

            return services;
        }

        public static async Task ValidateStartupHealthChecksAsync(
            this IServiceProvider services,
            HealthCheckRunnerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<StartupHealthCheckRunner>();
            await runner.RunAsync(options, cancellationToken);
        }
    }

}
