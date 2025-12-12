namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for HTTP logging
/// </summary>
public class HttpLoggingOptions : BaseOptions
{
    public bool LogRequestHeaders { get; set; } = true;

    public bool LogResponseHeaders { get; set; } = true;

    public bool LogRequestBody { get; set; }

    public bool LogResponseBody { get; set; }

    public override void Validate()
    {
        base.Validate();
        // HTTP logging options are generally valid with any boolean values
    }

    /// <summary>
    ///     Creates default HTTP logging options.
    /// </summary>
    public static HttpLoggingOptions CreateDefault() { return new HttpLoggingOptions(); }
}
