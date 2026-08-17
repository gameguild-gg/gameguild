using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GameGuild.API.Setup;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddOpenTelemetryObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var productName = ApiProductComposition.Instance.ApplicationName;
        var options = builder.Configuration.GetSection(OpenTelemetryRuntimeOptions.SectionName)
            .Get<OpenTelemetryRuntimeOptions>() ?? new OpenTelemetryRuntimeOptions();

        if (!options.Enabled)
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: string.IsNullOrWhiteSpace(options.ServiceName)
                        ? $"{productName}.API"
                        : options.ServiceName.Trim(),
                    serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version!.ToString(),
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                    ["service.namespace"] = productName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                        instrumentation.Filter = context =>
                            !IsHealthPath(context.Request.Path);
                    })
                    .AddHttpClientInstrumentation(instrumentation => instrumentation.RecordException = true)
                    .AddEntityFrameworkCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.SetDbStatementForText = options.IncludeSqlStatements;
                        instrumentation.SetDbStatementForStoredProcedure = options.IncludeSqlStatements;
                    })
                    .AddSource(
                        "GameGuild.Resources.QuotaManagement",
                        "GameGuild.Resources.Alerts",
                        "GameGuild.Analytics.Warehouse");

                if (options.ConsoleExporterEnabled)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = new Uri(options.OtlpEndpoint.Trim());
                        exporter.Protocol = ResolveProtocol(options.OtlpProtocol);
                    });
                }
            });

        return builder;
    }

    private static OtlpExportProtocol ResolveProtocol(string? protocol)
        => string.Equals(protocol, "grpc", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

    private static bool IsHealthPath(PathString path)
        => path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/live", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/ready", StringComparison.OrdinalIgnoreCase);
}

public sealed class OpenTelemetryRuntimeOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? OtlpEndpoint { get; set; }
    public string? OtlpProtocol { get; set; } = "http/protobuf";
    public bool ConsoleExporterEnabled { get; set; }
    public bool IncludeSqlStatements { get; set; }
}
