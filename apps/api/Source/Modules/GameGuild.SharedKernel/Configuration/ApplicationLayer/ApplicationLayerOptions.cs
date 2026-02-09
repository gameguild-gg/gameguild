namespace GameGuild.Configuration.ApplicationLayer;

/// <summary>
///     Configuration options for the Application Layer services.
/// </summary>
public sealed class ApplicationLayerOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "Application";

    /// <summary>
    ///     Enables the CQRS pipeline (commands, queries, handlers, validators).
    /// </summary>
    public bool EnableCqrs { get; set; } = true;

    /// <summary>
    ///     Enables FluentValidation for input validation.
    /// </summary>
    public bool EnableFluentValidation { get; set; } = true;

    /// <summary>
    ///     Creates default application layer options.
    /// </summary>
    public static ApplicationLayerOptions CreateDefault() => new();
}
