using GameGuild.API;
using GameGuild.API.Email;
using GameGuild.API.Database;
using GameGuild.API.Integration;
using GameGuild.API.Setup;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

// WebApplication.CreateBuilder already loads JSON settings, environment variables,
// and host-provided overrides in the correct precedence order.
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

// Cross-module background jobs (composition-root level)
builder.Services.Configure<EmailDeliveryOptions>(builder.Configuration.GetSection("EmailDelivery"));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.Configure<SubscriptionNotificationLinkOptions>(builder.Configuration.GetSection("SubscriptionNotifications"));
builder.Services.AddScoped<IMonthlyStatementMailSender, MonthlyStatementMailSenderAdapter>();
builder.Services.AddScoped<IMonthlyStatementDataProvider, MonthlyStatementDataProvider>();
builder.Services.AddScoped<IMonthlyStatementAttachmentBuilder, MonthlyStatementAttachmentBuilder>();
builder.Services.AddSingleton<IMonthlyStatementLinkBuilder, MonthlyStatementLinkBuilder>();

// Build the configured web application
var app = builder.Build();
DatabaseStartupConfiguration.ThrowIfInvalid(app.Configuration, app.Environment.EnvironmentName);

if (importSnapshotCourses)
{
    await ImportSnapshotCoursesAsync(app, "Snapshot course import complete").ConfigureAwait(false);
    return;
}

// Trigger fail-closed value-movement option validation before migrations or HTTP listeners.
var stripeGatewayOptions = app.Services.GetRequiredService<IOptions<StripeGatewayOptions>>().Value;
var billingConfiguration = app.Services.GetRequiredService<IOptions<BillingConfiguration>>().Value;
StripeProviderConfigurationGuard.ThrowIfInvalid(
    stripeGatewayOptions,
    billingConfiguration,
    app.Environment.EnvironmentName,
    app.Logger);

if (app.Configuration.GetValue<bool?>("Database:RunStartupInitialization") ?? true)
{
    await RunDatabaseInitializationAsync(app).ConfigureAwait(false);
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

static async Task RunDatabaseInitializationAsync(WebApplication app)
{
    var databaseConnectivityProbe = app.Services.GetRequiredService<DatabaseConnectivityProbe>();
    var databaseReachable = await databaseConnectivityProbe.IsReachableAsync().ConfigureAwait(false);

    if (!databaseReachable)
    {
        app.Logger.LogWarning(
            "Initial database probe failed. Continuing into the migration retry loop so transient database startup delays do not leave the schema unapplied.");
    }

    databaseReachable = await TryApplyDatabaseMigrationsAsync(app).ConfigureAwait(false);

    if (!databaseReachable)
    {
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        var failStartupOnSeedFailure = app.Configuration.GetValue<bool?>("Database:FailStartupOnSeedFailure")
            ?? !DatabaseStartupConfiguration.AllowsRuntimeFallback(app.Environment.EnvironmentName);
        if (failStartupOnSeedFailure)
        {
            app.Logger.LogCritical(ex, "Database seeding failed. Refusing to open the HTTP listener.");
            throw;
        }

        app.Logger.LogWarning(ex, "Database seeding failed - default roles and admin user may not exist.");
    }

    if (ShouldImportSnapshotCourses(app.Configuration))
    {
        await ImportSnapshotCoursesAsync(app, "Snapshot course startup import complete").ConfigureAwait(false);
    }
}

static async Task ImportSnapshotCoursesAsync(WebApplication app, string message)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var result = await SnapshotCourseSeeder.SeedAsync(scope.ServiceProvider).ConfigureAwait(false);
        app.Logger.LogInformation(
            "{Message}. Parsed {ParsedPrograms} programs and {ParsedContents} contents from {CoursesRoot}. Created {CreatedPrograms} new programs and {CreatedContents} contents. DbContext sees {PublicProgramCount} published/public programs in database {DatabaseName}.",
            message,
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
        app.Logger.LogWarning(ex, "{Message} failed. Public fallback pages can still render, but API-backed course management may be empty.", message);
    }
}

static async Task<bool> TryApplyDatabaseMigrationsAsync(WebApplication app)
{
    var maxAttempts = Math.Clamp(app.Configuration.GetValue<int?>("Database:MigrationMaxAttempts") ?? 5, 1, 20);
    var failStartupOnMigrationFailure = app.Configuration.GetValue<bool?>("Database:FailStartupOnMigrationFailure")
        ?? !app.Environment.IsDevelopment();

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var migrationConnectionString = DatabaseStartupConfiguration.ResolveMigrationConnectionString(app.Configuration);
            var ownsMigrationContext = !string.IsNullOrWhiteSpace(migrationConnectionString);
            await using var db = ownsMigrationContext
                ? CreateMigrationDbContext(migrationConnectionString!)
                : scope.ServiceProvider.GetRequiredService<GameGuild.API.Database.ApplicationDbContext>();

            if (!db.Database.IsRelational())
            {
                return true;
            }

            var pendingMigrations = (await db.Database
                    .GetPendingMigrationsAsync(app.Lifetime.ApplicationStopping)
                    .ConfigureAwait(false))
                .ToArray();

            if (pendingMigrations.Length == 0)
            {
                app.Logger.LogInformation("Database schema is current.");
                return true;
            }

            app.Logger.LogInformation(
                "Applying {PendingMigrationCount} pending database migrations. Attempt {Attempt}/{MaxAttempts}.",
                pendingMigrations.Length,
                attempt,
                maxAttempts);

            await db.Database
                .MigrateAsync(app.Lifetime.ApplicationStopping)
                .ConfigureAwait(false);

            app.Logger.LogInformation("Database migrations applied successfully.");

            return true;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var retryDelay = TimeSpan.FromSeconds(Math.Min(30, attempt * 2));
            app.Logger.LogWarning(
                ex,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {RetryDelaySeconds}s.",
                attempt,
                maxAttempts,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (failStartupOnMigrationFailure)
            {
                app.Logger.LogCritical(
                    ex,
                    "Database migrations failed after {MaxAttempts} attempts. Refusing to start because this environment requires a ready schema.",
                    maxAttempts);
                throw;
            }

            app.Logger.LogWarning(
                ex,
                "Database migrations failed after {MaxAttempts} attempts. API will start without database-backed readiness.",
                maxAttempts);
            return false;
        }
    }

    return false;
}

static GameGuild.API.Database.ApplicationDbContext CreateMigrationDbContext(string connectionString)
{
    var optionsBuilder = new DbContextOptionsBuilder<GameGuild.API.Database.ApplicationDbContext>();
    optionsBuilder.UseNpgsql(PostgresConnectionString.Normalize(connectionString) ?? connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(GameGuild.API.Database.ApplicationDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(120);
    });
    optionsBuilder.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

    return new GameGuild.API.Database.ApplicationDbContext(optionsBuilder.Options);
}

// REMARK: Required for functional and integration tests to work.
namespace GameGuild.API
{
    /// <summary>
    /// Exposes the Program class to integration tests (e.g. TestHost).
    /// </summary>
    public class Program { }
}
