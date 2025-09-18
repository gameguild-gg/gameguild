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

  public bool EnableAuthorization { get; set; } = true;

  public bool EnableResponseCompression { get; set; } = true;

  public bool EnableHttpLogging { get; set; }

  public bool EnableProblemDetails { get; set; } = true;

  public bool EnableLocalization { get; set; }

  public bool EnableModelValidation { get; set; } = true;

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
    FeatureFlags?.Validate();
  }
}
