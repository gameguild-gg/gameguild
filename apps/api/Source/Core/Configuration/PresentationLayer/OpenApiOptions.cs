namespace GameGuild;

/// <summary>
/// Configuration options for OpenAPI/Swagger.
/// </summary>
public class OpenApiOptions {
  /// <summary>
  /// The API title shown in Swagger UI.
  /// </summary>
  public string Title { get; set; } = "GameGuild API";

  /// <summary>
  /// The API version shown in Swagger UI.
  /// </summary>
  public string Version { get; set; } = "v1";

  /// <summary>
  /// The API description shown in Swagger UI.
  /// </summary>
  public string Description { get; set; } = "A comprehensive SaaS management platform API";

  /// <summary>
  /// Contact name for the API.
  /// </summary>
  public string ContactName { get; set; } = "GameGuild";

  /// <summary>
  /// Contact email for the API.
  /// </summary>
  public string ContactEmail { get; set; } = "support@GameGuild.com";

  /// <summary>
  /// Contact URL for the API.
  /// </summary>
  public string? ContactUrl { get; set; }

  /// <summary>
  /// License name for the API.
  /// </summary>
  public string? LicenseName { get; set; }

  /// <summary>
  /// License URL for the API.
  /// </summary>
  public string? LicenseUrl { get; set; }

  /// <summary>
  /// Terms of service URL for the API.
  /// </summary>
  public string? TermsOfServiceUrl { get; set; }

  /// <summary>
  /// Whether to include XML comments in the documentation.
  /// </summary>
  public bool IncludeXmlComments { get; set; } = true;

  /// <summary>
  /// Whether to add security definitions for JWT Bearer tokens.
  /// </summary>
  public bool EnableBearerSecurity { get; set; } = true;

  /// <summary>
  /// Custom servers to include in the OpenAPI document.
  /// </summary>
  public OpenApiServerOptions[ ] Servers { get; set; } = Array.Empty<OpenApiServerOptions>();

  /// <summary>
  /// Validates the OpenAPI options.
  /// </summary>
  public void Validate() {
    if (string.IsNullOrWhiteSpace(Title)) { throw new ArgumentException("API title cannot be null or empty.", nameof(Title)); }

    if (string.IsNullOrWhiteSpace(Version)) { throw new ArgumentException("API version cannot be null or empty.", nameof(Version)); }

    if (string.IsNullOrWhiteSpace(ContactName)) { throw new ArgumentException("Contact name cannot be null or empty.", nameof(ContactName)); }

    if (string.IsNullOrWhiteSpace(ContactEmail)) { throw new ArgumentException("Contact email cannot be null or empty.", nameof(ContactEmail)); }

    foreach (var server in Servers) { server?.Validate(); }
  }
}
