namespace GameGuild;

/// <summary> Builder for problem details options. </summary>
public static class ProblemDetailsOptionsBuilder {
  /// <summary> Creates problem details options with default values. </summary>
  /// <returns> Default problem details options </returns>
  public static ProblemDetailsOptions Create() { return new ProblemDetailsOptions { IncludeExceptionDetails = false, DefaultTitle = "An error occurred" }; }

  /// <summary> Creates problem details options from a specific configuration section. </summary>
  /// <param name="configuration"> The configuration to bind from </param>
  /// <param name="sectionName"> The configuration section name </param>
  /// <returns> Configured problem details options </returns>
  public static ProblemDetailsOptions Create(IConfiguration configuration, string sectionName = "ProblemDetails") {
    ArgumentNullException.ThrowIfNull(configuration);

    var options = Create();
    var section = configuration.GetSection(sectionName);

    if (section.Exists()) { section.Bind(options); }

    return options;
  }

  /// <summary> Validates the provided problem details options. </summary>
  /// <param name="options"> The options to validate </param>
  /// <exception cref="ArgumentNullException"> Thrown when options is null </exception>
  /// <exception cref="InvalidOperationException"> Thrown when configuration is invalid </exception>
  public static void Validate(ProblemDetailsOptions options) {
    ArgumentNullException.ThrowIfNull(options);

    if (string.IsNullOrWhiteSpace(options.DefaultTitle)) throw new InvalidOperationException("Default title cannot be null or empty.");
  }

  /// <summary> Creates and validates problem details options with default values. </summary>
  /// <returns> Validated problem details options with default configuration </returns>
  public static ProblemDetailsOptions Build() {
    var options = Create();
    Validate(options);

    return options;
  }

  /// <summary> Creates and validates problem details options from configuration. </summary>
  /// <param name="configuration"> The configuration to bind from </param>
  /// <param name="sectionName"> The configuration section name </param>
  /// <returns> Validated problem details options </returns>
  public static ProblemDetailsOptions Build(IConfiguration configuration, string sectionName = "ProblemDetails") {
    var options = Create(configuration, sectionName);
    Validate(options);

    return options;
  }
}
