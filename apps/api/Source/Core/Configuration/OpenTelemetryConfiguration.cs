using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for OpenTelemetry tracing and metrics
/// </summary>
public static class OpenTelemetryConfiguration {
    /// <summary>
    /// Application name for telemetry
    /// </summary>
    public const string ServiceName = "GameGuild.API";

    /// <summary>
    /// Application version for telemetry
    /// </summary>
    public static readonly string ServiceVersion = typeof(OpenTelemetryConfiguration).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Custom activity source for CQRS operations
    /// </summary>
    public static readonly ActivitySource CqrsActivitySource = new(ServiceName + ".CQRS", ServiceVersion);

    /// <summary>
    /// Custom activity source for permission operations
    /// </summary>
    public static readonly ActivitySource PermissionActivitySource = new(ServiceName + ".Permissions", ServiceVersion);

    /// <summary>
    /// Custom meter for application metrics
    /// </summary>
    public static readonly Meter ApplicationMeter = new(ServiceName, ServiceVersion);

    // Define custom metrics
    public static readonly Counter<long> RequestCounter = ApplicationMeter.CreateCounter<long>(
        "gameguild_requests_total",
        "requests",
        "Total number of HTTP requests");

    public static readonly Histogram<double> RequestDuration = ApplicationMeter.CreateHistogram<double>(
        "gameguild_request_duration_ms",
        "milliseconds",
        "Duration of HTTP requests in milliseconds");

    public static readonly Counter<long> CommandCounter = ApplicationMeter.CreateCounter<long>(
        "gameguild_commands_total",
        "commands",
        "Total number of CQRS commands executed");

    public static readonly Counter<long> QueryCounter = ApplicationMeter.CreateCounter<long>(
        "gameguild_queries_total",
        "queries",
        "Total number of CQRS queries executed");

    public static readonly Histogram<double> CommandDuration = ApplicationMeter.CreateHistogram<double>(
        "gameguild_command_duration_ms",
        "milliseconds",
        "Duration of CQRS commands in milliseconds");

    public static readonly Histogram<double> QueryDuration = ApplicationMeter.CreateHistogram<double>(
        "gameguild_query_duration_ms",
        "milliseconds",
        "Duration of CQRS queries in milliseconds");

    public static readonly Histogram<double> PermissionCheckDuration = ApplicationMeter.CreateHistogram<double>(
        "gameguild_permission_check_duration_ms",
        "milliseconds",
        "Duration of permission checks in milliseconds");

    public static readonly Counter<long> PermissionCheckCounter = ApplicationMeter.CreateCounter<long>(
        "gameguild_permission_checks_total",
        "checks",
        "Total number of permission checks performed");

    public static readonly Histogram<double> DatabaseOperationDuration = ApplicationMeter.CreateHistogram<double>(
        "gameguild_database_operation_duration_ms",
        "milliseconds",
        "Duration of database operations in milliseconds");

    /// <summary>
    /// Adds OpenTelemetry tracing and metrics to the service collection
    /// </summary>
    public static IServiceCollection AddOpenTelemetryObservability(this IServiceCollection services, IConfiguration configuration, OpenTelemetryOptions? options = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        options ??= new OpenTelemetryOptions();
        options.Validate();

        // Configure resource information
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(ServiceName, ServiceVersion)
            .AddAttributes(new[] {
                new KeyValuePair<string, object>("deployment.environment", options.Environment ?? "development"),
                new KeyValuePair<string, object>("service.instance.id", Environment.MachineName),
                new KeyValuePair<string, object>("service.namespace", "GameGuild")
            });

        services.AddOpenTelemetry()
            .ConfigureResource(builder => {
                // Simplified resource configuration without merge
                builder.AddService(ServiceName, ServiceVersion)
                       .AddAttributes([
                           new KeyValuePair<string, object>("service.instance.id", Environment.MachineName),
                           new KeyValuePair<string, object>("service.namespace", "GameGuild")
                       ]);
            })
            .WithTracing(tracingBuilder => {
                tracingBuilder
                    .AddSource(CqrsActivitySource.Name)
                    .AddSource(PermissionActivitySource.Name)
                    .AddAspNetCoreInstrumentation(opts => {
                        opts.RecordException = true;
                        opts.EnrichWithHttpRequest = (activity, request) => {
                            activity.SetTag("http.request.correlation_id", request.HttpContext.Items["CorrelationId"]?.ToString());
                            activity.SetTag("http.request.user_id", request.HttpContext.User?.FindFirst("sub")?.Value);
                        };
                        opts.EnrichWithHttpResponse = (activity, response) => {
                            activity.SetTag("http.response.correlation_id", response.HttpContext.Items["CorrelationId"]?.ToString());
                        };
                    })
                    .AddHttpClientInstrumentation(opts => {
                        opts.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation(opts => {
                        opts.SetDbStatementForText = true;
                        opts.SetDbStatementForStoredProcedure = true;
                        opts.EnrichWithIDbCommand = (activity, command) => {
                            activity.SetTag("db.operation.correlation_id", Activity.Current?.GetBaggageItem("CorrelationId"));
                        };
                    });

                // Configure exporters based on options
                if (options.EnableConsoleExporter) {
                    tracingBuilder.AddConsoleExporter();
                }

                if (options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint)) {
                    tracingBuilder.AddOtlpExporter(opts => {
                        opts.Endpoint = new Uri(options.OtlpEndpoint);
                        if (!string.IsNullOrEmpty(options.OtlpHeaders)) {
                            opts.Headers = options.OtlpHeaders;
                        }
                    });
                }
            })
            .WithMetrics(metricsBuilder => {
                metricsBuilder
                    .AddMeter(ApplicationMeter.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                // .AddRuntimeInstrumentation()  // DISABLED: Method doesn't exist in current OpenTelemetry version
                // .AddProcessInstrumentation();  // DISABLED: Method doesn't exist in current OpenTelemetry version

                // Configure exporters based on options
                if (options.EnableConsoleExporter) {
                    metricsBuilder.AddConsoleExporter();
                }

                if (options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint)) {
                    metricsBuilder.AddOtlpExporter(opts => {
                        opts.Endpoint = new Uri(options.OtlpEndpoint);
                        if (!string.IsNullOrEmpty(options.OtlpHeaders)) {
                            opts.Headers = options.OtlpHeaders;
                        }
                    });
                }
            });

        return services;
    }
}

/// <summary>
/// Configuration options for OpenTelemetry
/// </summary>
public class OpenTelemetryOptions {
    /// <summary>
    /// Application environment name
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Enable console exporter for development
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = true;

    /// <summary>
    /// Enable OTLP exporter for production
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = false;

    /// <summary>
    /// OTLP endpoint URL
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// OTLP headers for authentication
    /// </summary>
    public string? OtlpHeaders { get; set; }

    /// <summary>
    /// Validates the options
    /// </summary>
    public void Validate() {
        if (EnableOtlpExporter && string.IsNullOrEmpty(OtlpEndpoint)) {
            throw new InvalidOperationException("OtlpEndpoint must be specified when EnableOtlpExporter is true");
        }
    }
}
