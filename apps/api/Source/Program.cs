using GameGuild;
using GameGuild.Core.Configuration;
using Serilog;

// Bootstrap Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting GameGuild API");

try {
  var builder = WebApplication.CreateBuilder(args);

  // Configure Serilog as the logging provider
  builder.Host.UseSerilog((context, services, configuration) =>
      configuration.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .Enrich.WithMachineName()
          .Enrich.WithProcessId()
          .Enrich.WithProcessName()
          .Enrich.WithThreadId()
          .Enrich.WithEnvironmentName()
          .Enrich.WithProperty("Application", "GameGuild.API")
          .Enrich.WithProperty("Version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown")
          .MinimumLevel.Information()
          .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
          .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
          .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
          .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning));

  builder.AddAppSettings();
  builder.Configuration.AddEnvironmentVariables();

  // Add services to the container
  // Order matters: Infrastructure -> Application -> Presentation.
  builder.AddInfrastructureLayer();
  builder.AddApplicationLayer();
  builder.AddPresentationLayer();

  var app = builder.Build();

  // Use structured logging middleware
  app.UseStructuredLogging();

  app.ConfigurePipeline();

  Log.Information("GameGuild API starting on {Environment}", app.Environment.EnvironmentName);

  await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex) {
  Log.Fatal(ex, "GameGuild API terminated unexpectedly");
}
finally {
  Log.Information("GameGuild API shut down complete");
  Log.CloseAndFlush();
}

// REMARK: Required for functional and integration tests to work.
namespace GameGuild {
  internal class Program { };
}
