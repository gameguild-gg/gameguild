using System.Reflection;
using GameGuild.Core.Logging;
using GameGuild.Core.Middleware;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for structured logging with Serilog
/// </summary>
public static class LoggingConfiguration {
  /// <summary>
  /// Configures Serilog with structured logging, enrichers, and per-module filtering
  /// </summary>
  public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration) {
    // Configure Serilog
    Log.Logger = CreateLogger(configuration).CreateLogger();

    // Add Serilog to the service collection
    services.AddSerilog(Log.Logger, dispose: true);

    return services;
  }

  /// <summary>
  /// Configures the web application to use structured logging
  /// </summary>
  public static WebApplication UseStructuredLogging(this WebApplication app) {
    // Add correlation ID middleware (must be early in pipeline)
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Add Serilog request logging middleware
    app.UseSerilogRequestLogging(options => {
      options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
      options.GetLevel = GetLogLevel;
      options.EnrichDiagnosticContext = EnrichFromRequest;
    });

    return app;
  }

  private static LoggerConfiguration CreateLogger(IConfiguration configuration) {
    var loggerConfig = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentName()
        .Enrich.With<ModuleEnricher>()
        .Enrich.WithProperty("Application", "GameGuild.API")
        .Enrich.WithProperty("Version", GetApplicationVersion());

    // Configure minimum log levels per module
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("GameGuild.Modules.Authentication", LogEventLevel.Information)
        .MinimumLevel.Override("GameGuild.Modules.Authorization", LogEventLevel.Information)
        .MinimumLevel.Override("GameGuild.Modules.Permissions", LogEventLevel.Information)
        .MinimumLevel.Override("GameGuild.Core.Behaviors", LogEventLevel.Debug);

    // Configure sinks based on environment
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    if (environment == "Development") {
      loggerConfig.WriteTo.Console(
          outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Module}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    }
    else {
      loggerConfig.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
    }

    // Add file logging for all environments
    loggerConfig.WriteTo.File(
        path: "logs/gameguild-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        restrictedToMinimumLevel: LogEventLevel.Information);

    // Add structured file for errors
    loggerConfig.WriteTo.File(
        path: "logs/gameguild-errors-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        restrictedToMinimumLevel: LogEventLevel.Error);

    return loggerConfig;
  }

  private static LogEventLevel GetLogLevel(HttpContext ctx, double _, Exception? ex) {
    if (ex != null) return LogEventLevel.Error;

    return ctx.Response.StatusCode switch {
      >= 500 => LogEventLevel.Error,
      >= 400 => LogEventLevel.Warning,
      _ => LogEventLevel.Information
    };
  }

  private static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext) {
    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
    diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());

    // Add correlation ID if present
    if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId)) {
      diagnosticContext.Set("CorrelationId", correlationId.FirstOrDefault());
    }

    // Add user context if authenticated
    if (httpContext.User?.Identity?.IsAuthenticated == true) {
      var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
      if (!string.IsNullOrEmpty(userId)) {
        diagnosticContext.Set("UserId", userId);
      }

      var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
      if (!string.IsNullOrEmpty(tenantId)) {
        diagnosticContext.Set("TenantId", tenantId);
      }
    }

    // Add request size
    if (httpContext.Request.ContentLength.HasValue) {
      diagnosticContext.Set("RequestSize", httpContext.Request.ContentLength.Value);
    }

    // Add response size
    if (httpContext.Response.ContentLength.HasValue) {
      diagnosticContext.Set("ResponseSize", httpContext.Response.ContentLength.Value);
    }
  }

  private static string GetApplicationVersion() {
    var assembly = Assembly.GetExecutingAssembly();
    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                 ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version
                 ?? "1.0.0";
    return version;
  }
}
