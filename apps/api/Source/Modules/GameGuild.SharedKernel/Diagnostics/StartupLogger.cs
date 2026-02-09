using Microsoft.Extensions.Logging;

namespace GameGuild;

/// <summary>
///     Provides a shared startup logger for use during application bootstrapping,
///     before the DI container and full logging pipeline are configured.
/// </summary>
/// <remarks>
///     This replaces the <c>CreateStartupLogger()</c> method that was duplicated identically
///     in <c>PresentationLayerExtensions</c>, <c>InfrastructureLayerExtensions</c>,
///     and <c>ApplicationLayerExtensions</c>.
/// </remarks>
public static class StartupLogger
{
    private static readonly Lazy<ILoggerFactory> Factory = new(() =>
        LoggerFactory.Create(builder => builder.AddConsole()));

    /// <summary>
    ///     Creates a logger for use during application startup.
    ///     The underlying <see cref="ILoggerFactory" /> is lazily initialized and shared.
    /// </summary>
    /// <param name="categoryName">Logger category name. Defaults to "GameGuild.API.Startup".</param>
    public static ILogger Create(string categoryName = "GameGuild.API.Startup")
        => Factory.Value.CreateLogger(categoryName);

    /// <summary>
    ///     Creates a typed startup logger.
    /// </summary>
    public static ILogger<T> Create<T>()
        => Factory.Value.CreateLogger<T>();
}
