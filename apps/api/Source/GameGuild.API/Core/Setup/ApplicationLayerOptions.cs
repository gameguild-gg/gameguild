using GameGuild.Configuration;

namespace GameGuild.API.Setup;

/// <summary>
///     Options for configuring the application layer services during startup.
/// </summary>
public sealed class ApplicationLayerSetupOptions : BaseOptions
{
    /// <summary>
    ///     Gets or sets the module configuration for handler discovery.
    /// </summary>
    public ModuleConfiguration ModuleConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets whether to log handler statistics during startup.
    /// </summary>
    public bool LogHandlerStatistics { get; set; } = true;
}
