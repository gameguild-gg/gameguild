namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     Basic API versioning configuration options
/// </summary>
public sealed class ApiVersioningOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "ApiVersioning";

    /// <summary>
    ///     The default API version string (e.g., "1.0")
    /// </summary>
    public string DefaultVersion { get; set; } = "1.0";

    /// <summary>
    ///     Whether to assume the default version when no version is specified
    /// </summary>
    public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;

    /// <summary>
    ///     Custom query parameter name for version (default: "version")
    /// </summary>
    public string QueryParameterName { get; set; } = "version";

    /// <summary>
    ///     Custom header name for version (default: "X-Version")
    /// </summary>
    public string HeaderName { get; set; } = "X-Version";

    /// <summary>
    ///     Media type parameter name (for media type versioning)
    /// </summary>
    public string MediaTypeParameterName { get; set; } = "ver";

    /// <summary>
    ///     Format for API explorer group names.
    ///     The recommended format for Asp.Versioning is: 'v'VVV (e.g., v1, v1.1)
    /// </summary>
    public string GroupNameFormat { get; set; } = "'v'VVV";

    // Compatibility properties expected by API wiring
    public ApiVersionReadingStrategy ReadingStrategy { get; set; } = ApiVersionReadingStrategy.UrlSegment;

    public bool SubstituteApiVersionInUrl { get; set; } = true;

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(DefaultVersion)) throw new ArgumentException("Default version cannot be null or empty.", nameof(DefaultVersion));

        if (string.IsNullOrWhiteSpace(QueryParameterName)) throw new ArgumentException("Query parameter name cannot be null or empty.", nameof(QueryParameterName));

        if (string.IsNullOrWhiteSpace(HeaderName)) throw new ArgumentException("Header name cannot be null or empty.", nameof(HeaderName));

        if (string.IsNullOrWhiteSpace(GroupNameFormat)) throw new ArgumentException("Group name format cannot be null or empty.", nameof(GroupNameFormat));
    }

    public static ApiVersioningOptions CreateDefault() { return new ApiVersioningOptions(); }
}
