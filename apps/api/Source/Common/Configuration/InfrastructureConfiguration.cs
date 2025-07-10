using Microsoft.EntityFrameworkCore;


namespace GameGuild.Common.Configuration;

/// <summary>
/// Contains infrastructure-specific configuration methods for dependency injection.
/// This class follows the builder pattern and provides strongly-typed configuration options.
/// </summary>
public static class InfrastructureConfiguration {
  /// <summary>
  /// Configuration options for database setup.
  /// </summary>
  public class DatabaseOptions {
    public string ConnectionString { get; set; } = string.Empty;

    public bool UseInMemoryDatabase { get; set; } = false;

    public bool EnableSensitiveDataLogging { get; set; } = false;

    public bool EnableDetailedErrors { get; set; } = false;

    public string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    public string SchemaName { get; set; } = "dbo";

    public void Validate() {
      if (!UseInMemoryDatabase && string.IsNullOrEmpty(ConnectionString)) {
        throw new InvalidOperationException(
          "ConnectionString must be provided when not using in-memory database."
        );
      }
    }
  }

  /// <summary>
  /// Configuration options for health checks.
  /// </summary>
  public class HealthCheckOptions {
    public bool EnableDatabaseCheck { get; set; } = true;

    public bool EnableApiHealthCheck { get; set; } = true;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public Dictionary<string, string> Tags { get; set; } = new();

    public HealthCheckOptions() {
      Tags.Add("database", "infrastructure");
      Tags.Add("api", "readiness");
    }
  }

  /// <summary>
  /// Configuration options for authentication and authorization.
  /// </summary>
  public class AuthenticationOptions {
    public bool EnableAuthentication { get; set; } = true;

    public bool EnableAuthorization { get; set; } = true;

    public bool EnableDACAuthorization { get; set; } = true;

    public string JwtSecretKey { get; set; } = string.Empty;

    public string JwtIssuer { get; set; } = string.Empty;

    public string JwtAudience { get; set; } = string.Empty;

    public TimeSpan JwtExpiration { get; set; } = TimeSpan.FromHours(24);

    public void Validate() {
      if (EnableAuthentication) {
        if (string.IsNullOrEmpty(JwtSecretKey)) throw new InvalidOperationException("JWT secret key must be configured when authentication is enabled.");

        if (string.IsNullOrEmpty(JwtIssuer)) throw new InvalidOperationException("JWT issuer must be configured when authentication is enabled.");

        if (string.IsNullOrEmpty(JwtAudience)) throw new InvalidOperationException("JWT audience must be configured when authentication is enabled.");
      }
    }
  }

  /// <summary>
  /// Creates database options from configuration and environment variables.
  /// </summary>
  public static DatabaseOptions CreateDatabaseOptions(IConfiguration configuration) {
    var options = new DatabaseOptions {
      ConnectionString = GetDatabaseConnectionString(configuration), UseInMemoryDatabase = ShouldUseInMemoryDatabase(), EnableSensitiveDataLogging = IsEnvironment("Development"), EnableDetailedErrors = IsEnvironment("Development")
    };

    options.Validate();

    return options;
  }

  /// <summary>
  /// Creates health check options from configuration.
  /// </summary>
  public static HealthCheckOptions CreateHealthCheckOptions(IConfiguration configuration) {
    var options = new HealthCheckOptions();
    configuration.GetSection("HealthChecks").Bind(options);

    return options;
  }

  /// <summary>
  /// Creates authentication options from configuration.
  /// </summary>
  public static AuthenticationOptions CreateAuthenticationOptions(IConfiguration configuration) {
    var options = new AuthenticationOptions {
      JwtSecretKey = configuration["JWT_SECRET_KEY"] ?? configuration["Authentication:JwtSecretKey"] ?? string.Empty,
      JwtIssuer = configuration["JWT_ISSUER"] ?? configuration["Authentication:JwtIssuer"] ?? "GameGuild",
      JwtAudience = configuration["JWT_AUDIENCE"] ?? configuration["Authentication:JwtAudience"] ?? "GameGuild",
    };

    if (TimeSpan.TryParse(configuration["JWT_EXPIRATION"] ?? configuration["Authentication:JwtExpiration"], out var expiration)) { options.JwtExpiration = expiration; }

    options.Validate();

    return options;
  }

  /// <summary>
  /// Gets the database connection string from configuration or environment variables.
  /// Environment variables take precedence for security in production.
  /// </summary>
  private static string GetDatabaseConnectionString(IConfiguration configuration) {
    // Try environment variable first (production security best practice)
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

    // Fallback to configuration
    connectionString ??= configuration.GetConnectionString("DB_CONNECTION_STRING");
    connectionString ??= configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString)) {
      throw new InvalidOperationException(
        "Database connection string not found. Please set DB_CONNECTION_STRING environment variable " +
        "or configure 'ConnectionStrings:DB_CONNECTION_STRING' in appSettings.json"
      );
    }

    return connectionString;
  }

  /// <summary>
  /// Determines whether to use in-memory database based on environment configuration.
  /// </summary>
  private static bool ShouldUseInMemoryDatabase() {
    var useInMemoryEnv = Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB");

    return !string.IsNullOrEmpty(useInMemoryEnv) &&
           useInMemoryEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Checks if the current environment matches the specified environment name.
  /// </summary>
  private static bool IsEnvironment(string environmentName) {
    var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    return environmentName.Equals(currentEnvironment, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Configures Entity Framework DbContext with the specified options.
  /// </summary>
  public static void ConfigureDbContext(DbContextOptionsBuilder options, DatabaseOptions dbOptions) {
    if (dbOptions.UseInMemoryDatabase) { ConfigureInMemoryDatabase(options); }
    else { ConfigureSqliteDatabase(options, dbOptions); }

    ConfigureDatabaseLogging(options, dbOptions);
  }

  /// <summary>
  /// Configures in-memory database for testing.
  /// </summary>
  private static void ConfigureInMemoryDatabase(DbContextOptionsBuilder options) {
    var databaseName = $"GameGuild_TestDb_{Guid.NewGuid()}";
    options.UseInMemoryDatabase(databaseName);
  }

  /// <summary>
  /// Configures SQLite database for development and production.
  /// </summary>
  private static void ConfigureSqliteDatabase(DbContextOptionsBuilder options, DatabaseOptions dbOptions) {
    options.UseSqlite(dbOptions.ConnectionString, sqliteOptions => { sqliteOptions.MigrationsHistoryTable(dbOptions.MigrationsHistoryTable, dbOptions.SchemaName); });

    // Suppress SQLite pragma warnings that are not actionable
    options.ConfigureWarnings(warnings =>
                                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)
    );
  }

  /// <summary>
  /// Configures database logging based on options.
  /// </summary>
  private static void ConfigureDatabaseLogging(DbContextOptionsBuilder options, DatabaseOptions dbOptions) {
    if (dbOptions.EnableSensitiveDataLogging) { options.EnableSensitiveDataLogging(); }

    if (dbOptions.EnableDetailedErrors) { options.EnableDetailedErrors(); }

    // Add console logging in development
    if (IsEnvironment("Development")) { options.LogTo(Console.WriteLine, LogLevel.Information); }
  }
}
