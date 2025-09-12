namespace GameGuild;

/// <summary>
/// Contains infrastructure-specific configuration methods for dependency injection.
/// This class follows the static builder pattern and provides strongly-typed configuration options.
/// </summary>
public static class InfrastructureConfiguration
{
    /// <summary>
    /// Configures EntityBase Framework DbContext with the specified options.
    /// </summary>
    public static void ConfigureDbContext(DbContextOptionsBuilder options, DatabaseOptions dbOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dbOptions);

        if (dbOptions.UseInMemoryDatabase)
        {
            ConfigureInMemoryDatabase(options);
        }
        else
        {
            ConfigureSqliteDatabase(options, dbOptions);
        }

        ConfigureDatabaseLogging(options, dbOptions);
    }

    /// <summary>
    /// Configures in-memory database for testing.
    /// </summary>
    private static void ConfigureInMemoryDatabase(DbContextOptionsBuilder options)
    {
        // TODO: Add reference to Microsoft.EntityFrameworkCore.InMemory package and implement
        throw new NotImplementedException("In-memory database configuration requires EntityFrameworkCore.InMemory package reference");
    }

    /// <summary>
    /// Configures SQLite database for development and production.
    /// </summary>
    private static void ConfigureSqliteDatabase(DbContextOptionsBuilder options, DatabaseOptions dbOptions)
    {
        // TODO: Add reference to Microsoft.EntityFrameworkCore.Sqlite package and implement
        throw new NotImplementedException("SQLite database configuration requires EntityFrameworkCore.Sqlite package reference");
    }

    /// <summary>
    /// Configures database logging based on options.
    /// </summary>
    private static void ConfigureDatabaseLogging(DbContextOptionsBuilder options, DatabaseOptions dbOptions)
    {
        if (dbOptions.EnableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        if (dbOptions.EnableDetailedErrors)
        {
            options.EnableDetailedErrors();
        }

        // Add console logging in development
        if (IsEnvironment("Development"))
        {
            options.LogTo(Console.WriteLine, LogLevel.Information);
        }
    }

    /// <summary>
    /// Checks if the current environment matches the specified environment name.
    /// </summary>
    private static bool IsEnvironment(string environmentName)
    {
        var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return environmentName.Equals(currentEnvironment, StringComparison.OrdinalIgnoreCase);
    }
}
