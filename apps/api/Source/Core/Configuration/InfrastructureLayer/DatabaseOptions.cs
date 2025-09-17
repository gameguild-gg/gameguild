namespace GameGuild;

/// <summary>
/// Configuration options for database setup.
/// </summary>
public class DatabaseOptions {
  public string ConnectionString { get; set; } = string.Empty;

  public bool UseInMemoryDatabase { get; set; }

  public bool EnableSensitiveDataLogging { get; set; }

  public bool EnableDetailedErrors { get; set; }

  public string MigrationsHistoryTable { get; set; } = "__EFMigrationsHistory";

  public string SchemaName { get; set; } = "dbo";

  public void Validate() {
    if (!UseInMemoryDatabase && string.IsNullOrEmpty(ConnectionString)) throw new InvalidOperationException("ConnectionString must be provided when not using in-memory database.");
  }
}
