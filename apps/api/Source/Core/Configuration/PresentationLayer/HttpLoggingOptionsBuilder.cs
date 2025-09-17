namespace GameGuild;

/// <summary> Builder for HTTP logging options following the standard Create/Validate/Build pattern. </summary>
public static class HttpLoggingOptionsBuilder {
  /// <summary> Creates HTTP logging options with default values. </summary>
  /// <returns> Default HTTP logging options </returns>
  public static HttpLoggingOptions Create() { return new HttpLoggingOptions { LogRequestHeaders = true, LogResponseHeaders = true, LogRequestBody = false, LogResponseBody = false }; }

  /// <summary> Creates HTTP logging options from configuration. </summary>
  /// <param name="configuration"> The configuration to bind from </param>
  /// <param name="sectionName"> The configuration section name (defaults to "HttpLogging") </param>
  /// <returns> Configured HTTP logging options </returns>
  public static HttpLoggingOptions Create(IConfiguration configuration, string sectionName = "HttpLogging") {
    ArgumentNullException.ThrowIfNull(configuration);

    var options = Create();
    var section = configuration.GetSection(sectionName);

    if (section.Exists()) { section.Bind(options); }

    return options;
  }

  /// <summary> Builds and validates HTTP logging options. </summary>
  /// <param name="options"> The options to validate and return </param>
  /// <returns> Validated HTTP logging options </returns>
  public static HttpLoggingOptions Build(this HttpLoggingOptions options) {
    ArgumentNullException.ThrowIfNull(options);

    options.Validate();

    return options;
  }
}
