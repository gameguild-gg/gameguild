using GameGuild.Configuration;

namespace GameGuild.API.Setup;

/// <summary>
///     Options for configuring the infrastructure layer services during startup.
/// </summary>
public sealed class InfrastructureLayerSetupOptions : BaseOptions
{
    /// <summary>
    ///     Gets or sets whether to use in-memory database (for testing).
    /// </summary>
    public bool UseInMemoryDatabase { get; set; }

    /// <summary>
    ///     Gets or sets the database connection string override.
    /// </summary>
    public string? ConnectionStringOverride { get; set; }
}
