using Microsoft.EntityFrameworkCore.Design;

namespace GameGuild.Database;

/// <summary>
/// Factory for creating ApplicationDbContext instances
/// Required for Entity Framework migrations and design-time operations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Creates a new instance of ApplicationDbContext for design-time operations
    /// </summary>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Build configuration from multiple sources
        var configuration = BuildConfiguration();

        // Configure the database connection
        var connectionString = GetConnectionString(configuration);
        optionsBuilder.UseNpgsql(connectionString, options => { options.MigrationsHistoryTable("migrations_history", Schemas.Default); });

        // Enable sensitive data logging in development
        if (!IsDevelopment()) return new ApplicationDbContext(optionsBuilder.Options);

        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Builds configuration from multiple sources
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{GetEnvironment()}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Gets the database connection string from configuration or environment variables
    /// </summary>
    private static string GetConnectionString(IConfiguration configuration)
    {
        // Try environment variable first (production security best practice)
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        // If not found, try to build PostgreSQL connection string from individual components
        if (string.IsNullOrEmpty(connectionString))
        {
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DB_HOST"] ?? configuration["Database:Host"] ?? "localhost";

            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? configuration["DB_PORT"] ?? configuration["Database:Port"] ?? "5432";

            var database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? configuration["DB_DATABASE"] ?? configuration["Database:Database"] ?? "gameguild";

            var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? configuration["DB_USERNAME"] ?? configuration["Database:Username"] ?? "postgres";

            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? configuration["DB_PASSWORD"] ?? configuration["Database:Password"] ?? "postgres";

            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        }

        // Fallback to configuration connection strings
        connectionString ??= configuration.GetConnectionString("DB_CONNECTION_STRING");
        connectionString ??= configuration.GetConnectionString("DefaultConnection");

        // Final fallback for development
        if (!string.IsNullOrEmpty(connectionString)) return connectionString;

        if (IsDevelopment())
        {
            // Use localhost for native development, postgres for Docker
            var defaultHost = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ? "postgres" : "localhost";

            connectionString = $"Host={defaultHost};Port=5432;Database=gameguild;Username=postgres;Password=postgres;";

            Console.WriteLine($"⚠️  Using default development connection string: {defaultHost}/gameguild");
            Console.WriteLine("   To customize, set DB_CONNECTION_STRING environment variable");
        }
        else
        {
            throw new InvalidOperationException(
                "PostgreSQL database connection string not found. Please set DB_CONNECTION_STRING environment variable " +
                "or configure individual DB_HOST, DB_PORT, DB_DATABASE, DB_USERNAME, DB_PASSWORD environment variables " +
                "or configure 'ConnectionStrings:DB_CONNECTION_STRING' in appsettings.json."
            );
        }

        return connectionString;
    }

    /// <summary>
    /// Gets the current environment name
    /// </summary>
    private static string GetEnvironment() { return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"; }

    /// <summary>
    /// Checks if running in development environment
    /// </summary>
    private static bool IsDevelopment() { return GetEnvironment().Equals("Development", StringComparison.OrdinalIgnoreCase); }
}
