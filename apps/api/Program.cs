using GameGuild;
using GameGuild.Core.Configuration;
using Serilog;

// Bootstrap Serilog early to capture startup logs
Log.Logger = LoggingConfiguration.CreateBootstrapLogger();

try {
    Log.Information("Starting GameGuild API");

    var builder = WebApplication.CreateBuilder(args);

    // Early pipeline configuration (order matters)
    // 1. Configuration sources (must be first)
    builder.AddAppSettings();
    builder.AddEnvironmentVariables();

    // 2. Logging (must be early to capture logs from layer initialization)
    builder.AddLogging();

    // Add services to the container
    // Order matters: Infrastructure -> Application -> Presentation.
    builder.AddInfrastructureLayer();
    builder.AddApplicationLayer();
    builder.AddPresentationLayer();

    var app = builder.Build();

    app.ConfigurePipeline();

    await app.ApplyDatabaseMigrationsAsync();

    Log.Information("GameGuild API starting on {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException) {
    Log.Fatal(exception, "GameGuild API terminated unexpectedly");
}
finally {
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

// REMARK: Required for functional and integration tests to work.
namespace GameGuild {
    public class Program { }
}
