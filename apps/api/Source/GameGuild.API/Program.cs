using GameGuild.API;
using GameGuild.API.Setup;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

// ===========================================================================================
// GameGuild API - Entry Point
// ===========================================================================================
// This file configures the web application builder, registers all required services
// across the Infrastructure, Application, and Presentation layers, and sets up the
// HTTP request pipeline.
//
// Service Registration Order:
//   1. Infrastructure Layer: Database context, repositories, external services, caching
//   2. Application Layer: CQRS handlers, validators, business logic, module registration
//   3. Presentation Layer: Controllers, GraphQL, authentication, CORS, Swagger/OpenAPI
// ===========================================================================================

// Create the web application builder with default configuration
var builder = WebApplication.CreateBuilder(args);

// Configure JSON serializer options for all layers
// Note: JsonSerializerOptions.Default is read-only; configure via DI options instead.

// Configure HTTP JSON options (minimal APIs)
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.MaxDepth = 128;
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Persist Data Protection keys to a stable location so encrypted settings and cookies
// survive container restarts when a volume is mounted at the configured path.
{
    var dpKeysPath = builder.Configuration["DataProtection:KeysPath"]
        ?? Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH")
        ?? "/home/appuser/.aspnet/DataProtection-Keys";

    try
    {
        Directory.CreateDirectory(dpKeysPath);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
            .SetApplicationName("GameGuild");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DataProtection] Failed to configure persistent keys at '{dpKeysPath}': {ex.Message}. Falling back to defaults.");
        builder.Services.AddDataProtection().SetApplicationName("GameGuild");
    }
}

// Configure MVC JSON options with the same settings
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.MaxDepth = 128;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Configure application settings from appsettings.json and environment-specific files
builder.AddAppSettings();

// Load environment variables for sensitive configuration (e.g., connection strings, secrets)
builder.AddEnvironmentVariables();

// Configure structured logging with Serilog for comprehensive application logging
builder.AddStructuredLogging();

// Add services to the container
// Order matters: Infrastructure -> Application -> Presentation

// Infrastructure layer: Database context, repositories, external services, caching
builder.AddInfrastructureLayer();

// Application layer: CQRS handlers, validators, business logic, module registration
builder.AddApplicationLayer();

// Presentation layer: Controllers, GraphQL, authentication, CORS, Swagger/OpenAPI
builder.AddPresentationLayer();

// Build the configured web application
var app = builder.Build();

// Apply pending EF Core migrations automatically before starting the service
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GameGuild.API.Database.ApplicationDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Database migration failed — API will start without a database. Swagger/OpenAPI will still be available.");
}

// Configure the HTTP request pipeline (middleware, routing, endpoints)
app.ConfigurePipeline();

// Start the application and listen for incoming requests
await app.RunAsync().ConfigureAwait(false);

// REMARK: Required for functional and integration tests to work.
namespace GameGuild.API
{
    /// <summary>
    /// Exposes the Program class to integration tests (e.g. TestHost).
    /// </summary>
    public class Program { }
}
