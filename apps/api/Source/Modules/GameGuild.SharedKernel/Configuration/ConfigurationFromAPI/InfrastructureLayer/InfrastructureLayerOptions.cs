using GameGuild.Configuration.InfrastructureLayer.MemoryCaching;

namespace GameGuild.Configuration.ConfigurationFromAPI.InfrastructureLayer;

/// <summary>
///     Core infrastructure layer configuration options for repositories, database, caching, and external services.
/// </summary>
public sealed class InfrastructureLayerOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "InfrastructureLayer";

    /// <summary>
    ///     Enable database registration (ApplicationDbContext with PostgreSQL).
    /// </summary>
    public bool EnableDatabase { get; set; } = true;

    /// <summary>
    ///     Enable memory caching (foundation for other services).
    /// </summary>
    public bool EnableMemoryCaching { get; set; } = true;

    /// <summary>
    ///     Database configuration options.
    /// </summary>
    public DatabaseOptions? Database { get; set; }

    /// <summary>
    ///     Memory caching configuration options.
    /// </summary>
    public MemoryCachingOptions? MemoryCaching { get; set; }

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        
        // Validate nested options
        Database?.Validate();
        MemoryCaching?.Validate();
    }

    /// <summary>
    ///     Creates an instance with default values and nested options initialized.
    /// </summary>
    public static InfrastructureLayerOptions CreateDefault()
    {
        return new InfrastructureLayerOptions
        {
            EnableDatabase = true,
            EnableMemoryCaching = true,
            Database = DatabaseOptions.CreateDefault(),
            MemoryCaching = MemoryCachingOptions.CreateDefault()
        };
    }
}

/// <summary>
///     Database configuration options for PostgreSQL.
/// </summary>
public sealed class DatabaseOptions : BaseOptions
{
    /// <summary>
    ///     Connection string name in configuration.
    /// </summary>
    public string ConnectionStringName { get; set; } = "DefaultConnection";

    /// <summary>
    ///     Enable retry on failure for transient errors.
    /// </summary>
    public bool EnableRetryOnFailure { get; set; } = true;

    /// <summary>
    ///     Maximum retry count for transient failures.
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    ///     Maximum retry delay in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    ///     Command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     Maximum connection pool size. Npgsql default is 100.
    ///     Set based on expected concurrent database operations.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    ///     Minimum connection pool size. Npgsql default is 0.
    ///     Set to maintain warm connections for better latency.
    /// </summary>
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    ///     Connection idle lifetime in seconds before being closed.
    ///     Npgsql default is 300 (5 minutes).
    /// </summary>
    public int ConnectionIdleLifetimeSeconds { get; set; } = 300;

    /// <summary>
    ///     Connection lifetime in seconds before being recycled.
    ///     Helps with load balancer rotation. Default is 0 (no limit).
    /// </summary>
    public int ConnectionLifetimeSeconds { get; set; } = 0;

    /// <summary>
    ///     Enable sensitive data logging (development only).
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>
    ///     Enable detailed errors (development only).
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    ///     Creates an instance with default values.
    /// </summary>
    public static DatabaseOptions CreateDefault()
    {
        return new DatabaseOptions();
    }
}
