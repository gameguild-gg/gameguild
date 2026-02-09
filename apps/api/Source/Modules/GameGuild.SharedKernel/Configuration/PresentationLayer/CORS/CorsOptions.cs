namespace GameGuild.Configuration.PresentationLayer.CORS;

/// <summary>
///     Configuration options for Cross-Origin Resource Sharing (CORS) policies.
/// </summary>
public sealed class CorsOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>Origins allowed to make cross-origin requests (e.g. <c>"https://example.com"</c>). Use <c>"*"</c> for any origin.</summary>
    public string[ ] AllowedOrigins { get; set; } = [];

    /// <summary>HTTP methods allowed in cross-origin requests (e.g. <c>"GET"</c>, <c>"POST"</c>).</summary>
    public string[ ] AllowedMethods { get; set; } = [];

    /// <summary>HTTP headers allowed in cross-origin requests (e.g. <c>"Content-Type"</c>, <c>"Authorization"</c>).</summary>
    public string[ ] AllowedHeaders { get; set; } = [];

    public override void Validate()
    {
        base.Validate();

        // Validate CORS configuration
        if (AllowedOrigins.Contains("*") && AllowedOrigins.Length > 1) { throw new InvalidOperationException("When using wildcard '*' for AllowedOrigins, it must be the only origin specified."); }

        // Check for potential security issues
        if (AllowedOrigins.Contains("*") && (AllowedMethods.Contains("*") || AllowedHeaders.Contains("*")))
        {
            // This is a very permissive CORS configuration - consider if this is intentional
        }
    }

    /// <summary>
    ///     Creates default CORS options.
    /// </summary>
    public static CorsOptions CreateDefault() { return new CorsOptions(); }
}
