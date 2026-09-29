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
    ///     Selects the accepted numeric version format. Date-based versions remain supported in either mode.
    /// </summary>
    public ApiVersionFormatKind VersionFormat { get; set; } = ApiVersionFormatKind.Native;

    /// <summary>
    ///     Whether to assume the default version when no version is specified
    /// </summary>
    public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;

    /// <summary>
    ///     Whether responses advertise the supported and deprecated API versions.
    /// </summary>
    public bool ReportApiVersions { get; set; }

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

    /// <summary>
    ///     Sunset policies keyed by a version accepted by the configured API version parser.
    /// </summary>
    public Dictionary<string, ApiVersionSunsetPolicyOptions> SunsetPolicies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(DefaultVersion)) throw new ArgumentException("Default version cannot be null or empty.", nameof(DefaultVersion));

        if (!Enum.IsDefined(VersionFormat))
            throw new ArgumentOutOfRangeException(nameof(VersionFormat), VersionFormat, "The API version format is not supported.");

        var parser = ApiVersioningOptionsBuilder.CreateParser(VersionFormat);

        if (!parser.TryParse(DefaultVersion.AsSpan(), out _))
            throw new ArgumentException("Default version is not supported by the configured API version format.", nameof(DefaultVersion));

        if (string.IsNullOrWhiteSpace(QueryParameterName)) throw new ArgumentException("Query parameter name cannot be null or empty.", nameof(QueryParameterName));

        if (string.IsNullOrWhiteSpace(HeaderName)) throw new ArgumentException("Header name cannot be null or empty.", nameof(HeaderName));

        if (string.IsNullOrWhiteSpace(GroupNameFormat)) throw new ArgumentException("Group name format cannot be null or empty.", nameof(GroupNameFormat));

        if (!Enum.IsDefined(ReadingStrategy))
            throw new ArgumentOutOfRangeException(nameof(ReadingStrategy), ReadingStrategy, "The API version reading strategy is not supported.");

        if (SunsetPolicies is null)
            throw new ArgumentNullException(nameof(SunsetPolicies));

        foreach (var (version, policy) in SunsetPolicies)
        {
            if (string.IsNullOrWhiteSpace(version) || !parser.TryParse(version.AsSpan(), out _))
                throw new ArgumentException($"Sunset policy key '{version}' is not a supported API version.", nameof(SunsetPolicies));

            if (policy is null)
                throw new ArgumentException($"Sunset policy for version '{version}' cannot be null.", nameof(SunsetPolicies));

            if (!string.IsNullOrWhiteSpace(policy.PolicyUrl) &&
                (!Uri.TryCreate(policy.PolicyUrl, UriKind.Absolute, out var policyUri) ||
                 (policyUri.Scheme != Uri.UriSchemeHttps && policyUri.Scheme != Uri.UriSchemeHttp)))
                throw new ArgumentException($"Sunset policy URL for version '{version}' must be an absolute HTTP or HTTPS URL.", nameof(SunsetPolicies));
        }
    }

    public static ApiVersioningOptions CreateDefault() { return new ApiVersioningOptions(); }
}
