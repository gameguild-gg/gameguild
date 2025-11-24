namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for database setup
/// </summary>
public class DatabaseOptions : BaseOptions
{
    /// <summary>
    ///     Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    ///     Whether to use in-memory database (for testing/development)
    /// </summary>
    public bool UseInMemoryDatabase { get; set; }

    /// <summary>
    ///     Enable sensitive data logging (for development only)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>
    ///     Enable detailed errors (for development only)
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    ///     Name of the migrations history table
    /// </summary>
    public string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

    /// <summary>
    ///     Database schema name
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    public override void Validate()
    {
        base.Validate();

        if (!UseInMemoryDatabase && string.IsNullOrEmpty(ConnectionString)) throw new InvalidOperationException("ConnectionString must be provided when not using in-memory database.");

        if (string.IsNullOrWhiteSpace(MigrationsHistoryTable)) throw new InvalidOperationException("MigrationsHistoryTable cannot be empty.");

        if (string.IsNullOrWhiteSpace(SchemaName)) throw new InvalidOperationException("SchemaName cannot be empty.");
    }

    /// <summary>
    ///     Creates default database options.
    /// </summary>
    public static DatabaseOptions CreateDefault() { return new DatabaseOptions(); }
}
