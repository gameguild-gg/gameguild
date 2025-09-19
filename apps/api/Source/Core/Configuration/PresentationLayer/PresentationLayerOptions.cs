using GameGuild.Core.Configuration;

namespace GameGuild;

public class PresentationLayerOptions {
  public bool EnableRateLimiting { get; set; }

  public bool EnableOpenApi { get; set; } = true;

  public bool EnableApiVersioning { get; set; } = true;

  public bool EnableApiExplorer { get; set; } = true;

  public CorsOptions? Cors { get; set; }

  public ApiVersioningOptions? ApiVersioning { get; set; }

  public OpenApiOptions? OpenApi { get; set; }

  public AuthenticationOptions? Authentication { get; set; }

  public HttpLoggingOptions? HttpLogging { get; set; }

  public ProblemDetailsOptions? ProblemDetails { get; set; }

  public LocalizationOptions? Localization { get; set; }

  public ResponseCompressionOptions? ResponseCompression { get; set; }

  public RequestContextOptions? RequestContext { get; set; }

  public AuthorizationOptions? Authorization { get; set; }

  public RateLimitingOptions? RateLimiting { get; set; }

  public ModelValidationOptions? ModelValidation { get; set; }

  public ApiExplorerOptions? ApiExplorer { get; set; }

  public HealthChecksOptions? HealthChecks { get; set; }

  public ResponseCachingOptions? ResponseCaching { get; set; }

  public MemoryCachingOptions? MemoryCaching { get; set; }

  public SignalROptions? SignalR { get; set; }

  public GraphQlOptions? GraphQl { get; set; }

  public bool EnableCors { get; set; } = true;

  public bool EnableAuthentication { get; set; } = true;

  /// <summary>
  /// Enable cookie security features (secure cookie configuration, HttpOnly, SameSite policies)
  /// </summary>
  public bool EnableCookieSecurity { get; set; } = true;

  /// <summary>
  /// Cookie security configuration options
  /// </summary>
  public CookieSecurityOptions CookieSecurity { get; set; } = new();

  public bool EnableAuthorization { get; set; } = true;

  public bool EnableResponseCompression { get; set; } = true;

  public bool EnableHttpLogging { get; set; }

  public bool EnableProblemDetails { get; set; } = true;

  public bool EnableLocalization { get; set; }

  public bool EnableModelValidation { get; set; } = true;

  /// <summary>
  /// Enable FluentValidation features (validation pipeline behavior)
  /// </summary>
  public bool EnableFluentValidation { get; set; } = true;

  /// <summary>
  /// FluentValidation configuration options
  /// </summary>
  public FluentValidationOptions FluentValidation { get; set; } = new();

  /// <summary>
  /// Enable error handling features (global exception handling, ProblemDetails)
  /// </summary>
  public bool EnableErrorHandling { get; set; } = true;

  /// <summary>
  /// Error handling configuration options
  /// </summary>
  public ErrorHandlingOptions ErrorHandling { get; set; } = new();

  public bool EnableHealthChecks { get; set; } = true;

  public bool EnableRequestContext { get; set; } = true;

  public bool EnableResponseCaching { get; set; } = true;

  public bool EnableMemoryCaching { get; set; } = true;

  public bool EnableSignalR { get; set; }

  public bool EnableGraphQl { get; set; } = true;

  // Feature Flags (OpenFeature)
  public bool EnableFeatureFlags { get; set; }

  public FeatureFlagsOptions? FeatureFlags { get; set; }

  public void Validate() {
    ApiVersioning?.Validate();
    OpenApi?.Validate();
    Authentication?.Validate();
    CookieSecurity?.Validate();
    FeatureFlags?.Validate();
    FluentValidation?.Validate();
    ErrorHandling?.Validate();
  }
}
