using GameGuild;
using GameGuild.Core.Configuration;
using GameGuild.Database;
using GameGuild.Database.Extensions;
using Serilog;

// Bootstrap Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try {
    Log.Information("Starting GameGuild API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the logging provider
    builder.Host.UseSerilog((context, services, configuration) => configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.With<GameGuild.Core.Logging.ModuleEnricher>()
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
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    );

    builder.AddAppSettings();
    builder.Configuration.AddEnvironmentVariables();

    // Add services to the container
    // Order matters: Infrastructure -> Application -> Presentation.
    // Add audit and performance monitoring services
    builder.Services.AddSingleton<GameGuild.Core.Services.IAuditService, GameGuild.Core.Services.AuditService>();
    builder.Services.AddSingleton<GameGuild.Core.Services.IPerformanceMonitoringService, GameGuild.Core.Services.PerformanceMonitoringService>();

    builder.AddInfrastructureLayer();
    builder.AddApplicationLayer();
    builder.AddPresentationLayer();

    var app = builder.Build();

    // Use tenant logging middleware
    app.UseTenantLogging();

    // Use structured logging middleware
    app.UseStructuredLogging();

    app.ConfigurePipeline();

    // Migrate and seed the database (skip in Testing environment for integration tests)
    if (!app.Environment.IsEnvironment("Testing")) {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Log.Information("Applying database migrations and seeding initial data...");
        await context.MigrateAndSeedAsync(scope.ServiceProvider);
        Log.Information("Database migration and seeding completed");
    }

    Log.Information("GameGuild API starting on {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) {
    Log.Fatal(ex, "GameGuild API application terminated unexpectedly");
}
finally {
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

// REMARK: Required for functional and integration tests to work.
namespace GameGuild {
    public partial class WebApplicationEntryPoint { }
}
