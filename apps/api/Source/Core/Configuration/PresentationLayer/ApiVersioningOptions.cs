using Asp.Versioning;


namespace GameGuild;

/// <summary> Configuration options for API versioning. </summary>
public class ApiVersioningOptions {
  /// <summary> The default API version to use when no version is specified. </summary>
  public ApiVersion DefaultApiVersion { get; set; } = new ApiVersion(1, 0);

  /// <summary> Whether to assume the default version when no version is specified. </summary>
  public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;

  /// <summary> The API version reading strategy. </summary>
  public ApiVersionReadingStrategy ReadingStrategy { get; set; } = ApiVersionReadingStrategy.UrlSegmentAndQueryString;

  /// <summary> Custom query parameter name for version (default: "version"). </summary>
  public string QueryParameterName { get; set; } = "version";

  /// <summary> Custom header name for version (default: "X-Version"). </summary>
  public string HeaderName { get; set; } = "X-Version";

  /// <summary> Media type parameter name for version (default: "ver"). </summary>
  public string MediaTypeParameterName { get; set; } = "ver";

  /// <summary> Format for API explorer group names (default: "'v'VVV"). </summary>
  public string GroupNameFormat { get; set; } = "'v'VVV";

  /// <summary> Whether to substitute API version in URL for API explorer. </summary>
  public bool SubstituteApiVersionInUrl { get; set; } = true;

  /// <summary> Validates the API versioning options. </summary>
  /// <exception cref="ArgumentNullException"> </exception>
  public void Validate() {
    if (DefaultApiVersion == null) { throw new ArgumentNullException(nameof(DefaultApiVersion), "Default API version cannot be null."); }

    if (string.IsNullOrWhiteSpace(QueryParameterName)) { throw new ArgumentException("Query parameter name cannot be null or empty.", nameof(QueryParameterName)); }

    if (string.IsNullOrWhiteSpace(HeaderName)) { throw new ArgumentException("Header name cannot be null or empty.", nameof(HeaderName)); }

    if (string.IsNullOrWhiteSpace(MediaTypeParameterName)) { throw new ArgumentException("Media type parameter name cannot be null or empty.", nameof(MediaTypeParameterName)); }

    if (string.IsNullOrWhiteSpace(GroupNameFormat)) { throw new ArgumentException("Group name format cannot be null or empty.", nameof(GroupNameFormat)); }
  }
}
