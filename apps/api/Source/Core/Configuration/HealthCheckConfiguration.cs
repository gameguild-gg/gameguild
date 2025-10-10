using GameGuild.Database;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for application health checks
/// </summary>
public static class HealthCheckConfiguration
{
    /// <summary>
    /// Adds health checks to the service collection
    /// </summary>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration, HealthCheckOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind configuration to options if not provided
        if (options == null)
        {
            options = new HealthCheckOptions();
            configuration.GetSection("HealthChecks").Bind(options);

            // Override with environment variables if available
            var redisConnectionFromEnv = configuration.GetValue<string>("REDIS_CONNECTION_STRING");

            if (!string.IsNullOrEmpty(redisConnectionFromEnv)) { options.RedisConnectionString = redisConnectionFromEnv; }

            // Allow disabling Redis health check via environment variable
            var disableRedisCheck = configuration.GetValue<bool?>("DISABLE_REDIS_HEALTH_CHECK");

            if (disableRedisCheck == true) { options.EnableRedisCheck = false; }
        }

        options.Validate();

        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        // if (options.EnableDatabaseCheck) {
        //     healthChecksBuilder.AddDbContextCheck<GameGuild.Database.ApplicationDbContext>(
        //         name: "database",
        //         failureStatus: HealthStatus.Unhealthy,
        //         tags: new[] { "db", "sql", "ready" }
        //     );
        // }

        // Redis cache health check
        if (options.EnableRedisCheck && !string.IsNullOrEmpty(options.RedisConnectionString))
        {
            // Register Redis connection multiplexer for health check
            services.AddSingleton<IConnectionMultiplexer>(provider =>
                {
                    try { return ConnectionMultiplexer.Connect(options.RedisConnectionString); }
                    catch (Exception ex)
                    {
                        var logger = provider.GetService<ILogger<RedisHealthCheck>>();
                        logger?.LogWarning(ex, "Failed to connect to Redis for health check: {ConnectionString}", options.RedisConnectionString);

                        // Return null to allow graceful degradation
                        return null!;
                    }
                }
            );

            // For now, use a custom Redis health check
            healthChecksBuilder.AddCheck<RedisHealthCheck>(name : "redis", failureStatus : HealthStatus.Unhealthy, tags : new[ ] { "cache", "redis", "ready" });
        }

        // Payment provider health checks
        if (options.EnablePaymentProviderChecks)
        {
            healthChecksBuilder.AddCheck<PaymentProviderHealthCheck>(name : "payment-providers", failureStatus : HealthStatus.Degraded, tags : new[ ] { "external", "payment", "live" });
        }

        // KYC provider health checks
        if (options.EnableKycProviderChecks) { healthChecksBuilder.AddCheck<KycProviderHealthCheck>(name : "kyc-providers", failureStatus : HealthStatus.Degraded, tags : new[ ] { "external", "kyc", "live" }); }

        // Memory health check
        if (options.EnableMemoryCheck) { healthChecksBuilder.AddCheck<MemoryHealthCheck>(name : "memory", failureStatus : HealthStatus.Unhealthy, tags : new[ ] { "memory", "ready" }); }

        // Disk space health check
        if (options.EnableDiskSpaceCheck) { healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(name : "disk-space", failureStatus : HealthStatus.Unhealthy, tags : new[ ] { "disk", "ready" }); }

        // Register HttpClient for health checks
        services.AddHttpClient();

        // Register custom health check implementations
        // Only register RedisHealthCheck if Redis is enabled and connection available
        if (options.EnableRedisCheck) { services.AddSingleton<RedisHealthCheck>(); }

        services.AddSingleton<PaymentProviderHealthCheck>();
        services.AddSingleton<KycProviderHealthCheck>();
        services.AddSingleton<MemoryHealthCheck>();
        services.AddSingleton<DiskSpaceHealthCheck>();

        // Configure health check options
        services.Configure<HealthCheckOptions>(opt =>
            {
                opt.RedisConnectionString = options.RedisConnectionString;
                opt.MemoryThresholdMb = options.MemoryThresholdMb;
                opt.DiskSpaceThresholdGb = options.DiskSpaceThresholdGb;
            }
        );

        return services;
    }

    /// <summary>
    /// Configures health check endpoints
    /// </summary>
    public static IApplicationBuilder UseApplicationHealthChecks(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Map health check endpoints
        app.UseRouting();

        app.UseEndpoints(endpoints =>
            {
                // Readiness check - can the service handle requests?
                endpoints.MapHealthChecks(
                    "/health/ready",
                    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WriteHealthCheckResponse }
                );

                // Liveness check - is the service alive?
                endpoints.MapHealthChecks(
                    "/health/live",
                    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("live"), ResponseWriter = WriteHealthCheckResponse }
                );

                // Complete health check - all checks
                endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { ResponseWriter = WriteHealthCheckResponse });
            }
        );

        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = healthReport.Status.ToString(),
            duration = healthReport.TotalDuration.TotalMilliseconds,
            checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key, status = entry.Value.Status.ToString(), exception = entry.Value.Exception?.Message, duration = entry.Value.Duration.TotalMilliseconds, data = entry.Value.Data
                }
            )
        };

        await context.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, WriteIndented = true })
        );
    }
}
