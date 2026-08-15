using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring Serilog logging.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    ///     Configures structured logging with Serilog as the logging provider.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <returns>The web application builder for chaining</returns>
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Configure Serilog from configuration and code
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            var structuredLogPath = context.Configuration["StructuredLogging:Path"]
                                    ?? "logs/game-guild-.ndjson";

            configuration
                // Read from appsettings.json if configured
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)

                // Enrich logs with useful context
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "GameGuild.API")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)

                // Set minimum levels
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)

                // Default minimum level based on environment
                .MinimumLevel.Is(context.HostingEnvironment.IsDevelopment()
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information)

                // Console sink with structured output
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}      {Message:lj}{NewLine}{Exception}",
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.File(
                    formatter: new JsonFormatter(renderMessage: true),
                    path: structuredLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
        });

        return builder;
    }

    /// <summary>
    ///     Gets the default Serilog request logging options for the API.
    /// </summary>
    public static void ConfigureRequestLogging(Serilog.AspNetCore.RequestLoggingOptions options)
    {
        // Customize the message template
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

        // Attach additional properties to the request completion event
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            // Add correlation ID if available
            if (httpContext.Items.TryGetValue("X-Correlation-Id", out var correlationId) && correlationId is not null)
            {
                diagnosticContext.Set("CorrelationId", correlationId.ToString() ?? "unknown");
            }
            else
            {
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
            }

            // Add user info if authenticated
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "unknown");
                diagnosticContext.Set("UserName", httpContext.User.Identity.Name ?? "unknown");
            }

            // Add tenant info if available
            var tenantId = Identity.Tenants.Utilities.TenantIdExtractor.FromHeader(httpContext);
            if (tenantId.HasValue)
            {
                diagnosticContext.Set("TenantId", tenantId.Value.ToString());
            }
        };

        // Customize the log level based on status code
        options.GetLevel = (httpContext, _, exception) =>
        {
            if (exception != null)
            {
                return LogEventLevel.Error;
            }

            if (httpContext.Response.StatusCode >= 500)
            {
                return LogEventLevel.Error;
            }

            if (httpContext.Response.StatusCode >= 400)
            {
                return LogEventLevel.Warning;
            }

            // Log health checks and swagger as Debug to reduce noise
            var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
            if (path.StartsWith("/health") || path.StartsWith("/documentation") || path.StartsWith("/swagger"))
            {
                return LogEventLevel.Debug;
            }

            return LogEventLevel.Information;
        };
    }
}
