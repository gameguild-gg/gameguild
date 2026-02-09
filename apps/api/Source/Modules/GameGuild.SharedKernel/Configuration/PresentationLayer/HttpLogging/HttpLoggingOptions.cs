namespace GameGuild.Configuration.PresentationLayer.HttpLogging;

/// <summary>
///     Configuration options for HTTP logging
/// </summary>
public sealed class HttpLoggingOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "HttpLogging";

    public bool LogRequestHeaders { get; set; } = true;

    public bool LogResponseHeaders { get; set; } = true;

    public bool LogRequestBody { get; set; }

    public bool LogResponseBody { get; set; }

    /// <summary>
    ///     Creates default HTTP logging options.
    /// </summary>
    public static HttpLoggingOptions CreateDefault() { return new HttpLoggingOptions(); }
}
