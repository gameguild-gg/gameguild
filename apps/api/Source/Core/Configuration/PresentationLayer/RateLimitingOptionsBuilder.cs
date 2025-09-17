namespace GameGuild;

/// <summary> Builder for rate limiting options. </summary>
public static class RateLimitingOptionsBuilder {
  /// <summary> Creates rate limiting options with default values. </summary>
  /// <returns> Default rate limiting options </returns>
  public static RateLimitingOptions Create() { return new RateLimitingOptions { RequestsPerMinute = 60, BurstSize = 10, ExemptPaths = Array.Empty<string>() }; }

  /// <summary> Creates rate limiting options from a specific configuration section. </summary>
  /// <param name="configuration"> The configuration to bind from </param>
  /// <param name="sectionName"> The configuration section name </param>
  /// <returns> Configured rate limiting options </returns>
  public static RateLimitingOptions Create(IConfiguration configuration, string sectionName = "RateLimiting") {
    ArgumentNullException.ThrowIfNull(configuration);

    var options = Create();
    var section = configuration.GetSection(sectionName);

    if (section.Exists()) { section.Bind(options); }

    return options;
  }

  /// <summary> Builds and validates rate limiting options. </summary>
  /// <param name="options"> The options to validate and return </param>
  /// <returns> Validated rate limiting options </returns>
  public static RateLimitingOptions Build(this RateLimitingOptions options) {
    ArgumentNullException.ThrowIfNull(options);

    options.Validate();

    return options;
  }
}
