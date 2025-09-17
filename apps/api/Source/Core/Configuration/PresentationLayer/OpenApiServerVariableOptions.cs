namespace GameGuild;

/// <summary>
/// Configuration for OpenAPI server variables.
/// </summary>
public class OpenApiServerVariableOptions {
  /// <summary>
  /// The default value for the variable.
  /// </summary>
  public string Default { get; set; } = string.Empty;

  /// <summary>
  /// The description of the variable.
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Possible values for the variable.
  /// </summary>
  public string[ ] Enum { get; set; } = [];

  /// <summary>
  /// Validates the server variable options.
  /// </summary>
  public void Validate() {
    if (string.IsNullOrWhiteSpace(Default)) { throw new ArgumentException("Server variable default value cannot be null or empty.", nameof(Default)); }
  }
}
