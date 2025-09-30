namespace GameGuild.Core.Configuration;

/// <summary>
/// Extension methods for WebApplication health checks
/// </summary>
public static class WebApplicationHealthCheckExtensions
{
    /// <summary>
    /// Configures health check endpoints for WebApplication
    /// </summary>
    public static WebApplication UseApplicationHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Map health check endpoints
        app.MapHealthChecks(
            "/health/ready",
            new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            status = report.Status.ToString(),
                            checks = report.Entries.Select(entry => new
                                {
                                    name = entry.Key, status = entry.Value.Status.ToString(), exception = entry.Value.Exception?.Message, duration = entry.Value.Duration.ToString()
                                }
                            )
                        }
                    );
                    await context.Response.WriteAsync(response);
                }
            }
        );

        // Liveness check - is the service alive?
        app.MapHealthChecks(
            "/health/live",
            new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = System.Text.Json.JsonSerializer.Serialize(new { status = report.Status.ToString(), timestamp = DateTime.UtcNow });
                    await context.Response.WriteAsync(response);
                }
            }
        );

        // General health check
        app.MapHealthChecks(
            "/health",
            new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            status = report.Status.ToString(),
                            checks = report.Entries.Select(entry => new
                                {
                                    name = entry.Key, status = entry.Value.Status.ToString(), exception = entry.Value.Exception?.Message, duration = entry.Value.Duration.ToString()
                                }
                            ),
                            totalDuration = report.TotalDuration.ToString()
                        }
                    );
                    await context.Response.WriteAsync(response);
                }
            }
        );

        return app;
    }
}
