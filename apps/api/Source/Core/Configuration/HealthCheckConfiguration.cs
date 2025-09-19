using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for application health checks
/// </summary>
public static class HealthCheckConfiguration {
    /// <summary>
    /// Adds health checks to the service collection
    /// </summary>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration, HealthCheckOptions? options = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        options ??= new HealthCheckOptions();
        options.Validate();

        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        if (options.EnableDatabaseCheck) {
            healthChecksBuilder.AddDbContextCheck<GameGuild.Database.ApplicationDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" }
            );
        }

        // Redis cache health check
        if (options.EnableRedisCheck && !string.IsNullOrEmpty(options.RedisConnectionString)) {
            // Note: You'll need to add the StackExchange.Redis.Extensions.AspNetCore.HealthCheck package
            // healthChecksBuilder.AddRedis(options.RedisConnectionString, 
            //     name: "redis", 
            //     failureStatus: HealthStatus.Unhealthy,
            //     tags: new[] { "cache", "redis", "ready" });

            // For now, use a custom Redis health check
            healthChecksBuilder.AddCheck<RedisHealthCheck>(
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "cache", "redis", "ready" }
            );
        }

        // Payment provider health checks
        if (options.EnablePaymentProviderChecks) {
            healthChecksBuilder.AddCheck<PaymentProviderHealthCheck>(
                name: "payment-providers",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "payment", "live" }
            );
        }

        // KYC provider health checks
        if (options.EnableKycProviderChecks) {
            healthChecksBuilder.AddCheck<KycProviderHealthCheck>(
                name: "kyc-providers",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "external", "kyc", "live" }
            );
        }

        // Memory health check
        if (options.EnableMemoryCheck) {
            healthChecksBuilder.AddCheck<MemoryHealthCheck>(
                name: "memory",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "memory", "ready" }
            );
        }

        // Disk space health check
        if (options.EnableDiskSpaceCheck) {
            healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(
                name: "disk-space",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "disk", "ready" }
            );
        }

        // Register custom health check implementations
        services.AddSingleton<RedisHealthCheck>();
        services.AddSingleton<PaymentProviderHealthCheck>();
        services.AddSingleton<KycProviderHealthCheck>();
        services.AddSingleton<MemoryHealthCheck>();
        services.AddSingleton<DiskSpaceHealthCheck>();

        // Configure health check options
        services.Configure<HealthCheckOptions>(opt => {
            opt.RedisConnectionString = options.RedisConnectionString;
            opt.MemoryThresholdMB = options.MemoryThresholdMB;
            opt.DiskSpaceThresholdGB = options.DiskSpaceThresholdGB;
        });

        return services;
    }

    /// <summary>
    /// Configures health check endpoints
    /// </summary>
    public static IApplicationBuilder UseApplicationHealthChecks(this IApplicationBuilder app) {
        ArgumentNullException.ThrowIfNull(app);

        // Map health check endpoints
        app.UseRouting();
        app.UseEndpoints(endpoints => {
            // Readiness check - can the service handle requests?
            endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse
            });

            // Liveness check - is the service alive?
            endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = WriteHealthCheckResponse
            });

            // Complete health check - all checks
            endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
                ResponseWriter = WriteHealthCheckResponse
            });
        });

        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport) {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new {
            status = healthReport.Status.ToString(),
            duration = healthReport.TotalDuration.TotalMilliseconds,
            checks = healthReport.Entries.Select(entry => new {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                exception = entry.Value.Exception?.Message,
                duration = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data
            })
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
}

/// <summary>
/// Configuration options for health checks
/// </summary>
public class HealthCheckOptions {
    /// <summary>
    /// Enable database health check
    /// </summary>
    public bool EnableDatabaseCheck { get; set; } = true;

    /// <summary>
    /// Enable Redis cache health check
    /// </summary>
    public bool EnableRedisCheck { get; set; } = true;

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Enable payment provider health checks
    /// </summary>
    public bool EnablePaymentProviderChecks { get; set; } = true;

    /// <summary>
    /// Enable KYC provider health checks
    /// </summary>
    public bool EnableKycProviderChecks { get; set; } = true;

    /// <summary>
    /// Enable memory health check
    /// </summary>
    public bool EnableMemoryCheck { get; set; } = true;

    /// <summary>
    /// Memory threshold in MB
    /// </summary>
    public long MemoryThresholdMB { get; set; } = 1024;

    /// <summary>
    /// Enable disk space health check
    /// </summary>
    public bool EnableDiskSpaceCheck { get; set; } = true;

    /// <summary>
    /// Disk space threshold in GB
    /// </summary>
    public long DiskSpaceThresholdGB { get; set; } = 10;

    /// <summary>
    /// Validates the options
    /// </summary>
    public void Validate() {
        if (EnableRedisCheck && string.IsNullOrEmpty(RedisConnectionString)) {
            throw new InvalidOperationException("RedisConnectionString must be specified when EnableRedisCheck is true");
        }

        if (MemoryThresholdMB <= 0) {
            throw new ArgumentException("MemoryThresholdMB must be positive", nameof(MemoryThresholdMB));
        }

        if (DiskSpaceThresholdGB <= 0) {
            throw new ArgumentException("DiskSpaceThresholdGB must be positive", nameof(DiskSpaceThresholdGB));
        }
    }
}

/// <summary>
/// Extension methods for WebApplication health checks
/// </summary>
public static class WebApplicationHealthCheckExtensions {
    /// <summary>
    /// Configures health check endpoints for WebApplication
    /// </summary>
    public static WebApplication UseApplicationHealthChecks(this WebApplication app) {
        ArgumentNullException.ThrowIfNull(app);

        // Map health check endpoints
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = async (context, report) => {
                context.Response.ContentType = "application/json";
                var response = System.Text.Json.JsonSerializer.Serialize(new {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        exception = entry.Value.Exception?.Message,
                        duration = entry.Value.Duration.ToString()
                    })
                });
                await context.Response.WriteAsync(response);
            }
        });

        // Liveness check - is the service alive?
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = async (context, report) => {
                context.Response.ContentType = "application/json";
                var response = System.Text.Json.JsonSerializer.Serialize(new {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow
                });
                await context.Response.WriteAsync(response);
            }
        });

        // General health check
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
            ResponseWriter = async (context, report) => {
                context.Response.ContentType = "application/json";
                var response = System.Text.Json.JsonSerializer.Serialize(new {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        exception = entry.Value.Exception?.Message,
                        duration = entry.Value.Duration.ToString()
                    }),
                    totalDuration = report.TotalDuration.ToString()
                });
                await context.Response.WriteAsync(response);
            }
        });

        return app;
    }
}
