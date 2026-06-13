using GameGuild.API;
using GameGuild.API.Database;
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
var importSnapshotCourses = args.Any(argument => string.Equals(argument, "--import-snapshot-courses", StringComparison.OrdinalIgnoreCase));

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

builder.Services.AddSingleton<DatabaseConnectivityProbe>();

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

var databaseConnectivityProbe = app.Services.GetRequiredService<DatabaseConnectivityProbe>();
var databaseReachable = await databaseConnectivityProbe.IsReachableAsync().ConfigureAwait(false);

// Apply pending EF Core migrations automatically before starting the service
if (!databaseReachable)
{
    app.Logger.LogInformation(
        "Database host is unreachable. Starting API without migrations or seeding; database-backed jobs and endpoints will wait until connectivity is restored.");
}
else
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameGuild.API.Database.ApplicationDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync().ConfigureAwait(false);
        }
    }
    catch (Exception ex)
    {
        databaseReachable = false;
        app.Logger.LogWarning(ex, "Database migration failed — API will start without a database. Swagger/OpenAPI will still be available.");
    }

    if (databaseReachable)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await DatabaseSeeder.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Database seeding failed — default roles and admin user may not exist.");
        }

        if (ShouldImportSnapshotCourses(app.Configuration))
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var result = await SnapshotCourseSeeder.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
                app.Logger.LogInformation(
                    "Snapshot course startup import complete. Parsed {ParsedPrograms} programs and {ParsedContents} contents from {CoursesRoot}. Created {CreatedPrograms} new programs and {CreatedContents} new contents. DbContext sees {PublicProgramCount} published/public programs in database {DatabaseName}.",
                    result.ParsedPrograms,
                    result.ParsedContents,
                    result.CoursesRoot,
                    result.CreatedPrograms,
                    result.CreatedContents,
                    result.PublicProgramCount,
                    result.DatabaseName);
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Snapshot course startup import failed. Public fallback pages can still render, but API-backed course management may be empty.");
            }
        }
    }
}

if (importSnapshotCourses)
{
    using var scope = app.Services.CreateScope();
    var result = await SnapshotCourseSeeder.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
    app.Logger.LogInformation(
        "Snapshot course import complete. Parsed {ParsedPrograms} programs and {ParsedContents} contents from {CoursesRoot}. Created {CreatedPrograms} new programs and {CreatedContents} new contents. DbContext sees {PublicProgramCount} published/public programs in database {DatabaseName}.",
        result.ParsedPrograms,
        result.ParsedContents,
        result.CoursesRoot,
        result.CreatedPrograms,
        result.CreatedContents,
        result.PublicProgramCount,
        result.DatabaseName);
    return;
}

// Configure the HTTP request pipeline (middleware, routing, endpoints)
app.ConfigurePipeline();

// Start the application and listen for incoming requests
await app.RunAsync().ConfigureAwait(false);

static bool ShouldImportSnapshotCourses(IConfiguration configuration)
{
    var configuredValue = configuration["SeedData:ImportSnapshotCourses"]
        ?? Environment.GetEnvironmentVariable("SEED_SNAPSHOT_COURSES");

    return bool.TryParse(configuredValue, out var enabled) && enabled;
}

// REMARK: Required for functional and integration tests to work.
namespace GameGuild.API
{
    /// <summary>
    /// Exposes the Program class to integration tests (e.g. TestHost).
    /// </summary>
    public class Program { }
}
