namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Core presentation layer configuration options that can be shared across modules
/// </summary>
public class PresentationLayerOptions : BaseOptions
{
    public bool EnableRateLimiting { get; set; }

    public bool EnableOpenApi { get; set; } = true;

    public bool EnableApiVersioning { get; set; } = true;

    public bool EnableApiExplorer { get; set; } = true;

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

    public bool EnableGraphQl { get; set; }

    public bool EnableFeatureFlags { get; set; }

    // Configuration object properties - these would reference specific option types
    public CorsOptions? Cors { get; set; }

    public HttpLoggingOptions? HttpLogging { get; set; }

    public ProblemDetailsOptions? ProblemDetails { get; set; }

    public LocalizationOptions? Localization { get; set; }

    public MemoryCachingOptions? MemoryCaching { get; set; }

    public ResponseCachingOptions? ResponseCaching { get; set; }

    public ResponseCompressionOptions? ResponseCompression { get; set; }

    public AuthenticationOptions? Authentication { get; set; }

    public AuthorizationOptions? Authorization { get; set; }

    public RequestContextOptions? RequestContext { get; set; }

    public RateLimitingOptions? RateLimiting { get; set; }

    public ModelValidationOptions? ModelValidation { get; set; }

    public FeatureFlagsOptions? FeatureFlags { get; set; }

    public ApiVersioningOptions? ApiVersioning { get; set; }

    public HealthChecksOptions? HealthChecks { get; set; }

    public SignalROptions? SignalR { get; set; }

    public GraphQlOptions? GraphQL { get; set; }

    public OpenApiOptions? OpenApi { get; set; }

    public ApiExplorerOptions? ApiExplorer { get; set; }

    public override void Validate()
    {
        base.Validate();
        // Validate nested options
        Cors?.Validate();
        HttpLogging?.Validate();
        ProblemDetails?.Validate();
        Localization?.Validate();
        MemoryCaching?.Validate();
        ResponseCaching?.Validate();
        ResponseCompression?.Validate();
        Authentication?.Validate();
        Authorization?.Validate();
        RequestContext?.Validate();
        RateLimiting?.Validate();
        ModelValidation?.Validate();
        FeatureFlags?.Validate();
        ApiVersioning?.Validate();
        HealthChecks?.Validate();
        SignalR?.Validate();
        GraphQL?.Validate();
        OpenApi?.Validate();
        ApiExplorer?.Validate();
    }

    public static PresentationLayerOptions CreateDefault()
    {
        return new PresentationLayerOptions
        {
            Cors = CorsOptions.CreateDefault(),
            HttpLogging = HttpLoggingOptions.CreateDefault(),
            ProblemDetails = ProblemDetailsOptions.CreateDefault(),
            Localization = LocalizationOptions.CreateDefault(),
            MemoryCaching = MemoryCachingOptions.CreateDefault(),
            ResponseCaching = ResponseCachingOptions.CreateDefault(),
            ResponseCompression = ResponseCompressionOptions.CreateDefault(),
            Authentication = AuthenticationOptions.CreateDefault(),
            Authorization = AuthorizationOptions.CreateDefault(),
            RequestContext = RequestContextOptions.CreateDefault(),
            RateLimiting = RateLimitingOptions.CreateDefault(),
            ModelValidation = ModelValidationOptions.CreateDefault(),
            FeatureFlags = FeatureFlagsOptions.CreateDefault(),
            ApiVersioning = ApiVersioningOptions.CreateDefault(),
            HealthChecks = HealthChecksOptions.CreateDefault(),
            SignalR = SignalROptions.CreateDefault(),
            GraphQL = GraphQlOptions.CreateDefault(),
            OpenApi = OpenApiOptions.CreateDefault(),
            ApiExplorer = ApiExplorerOptions.CreateDefault()
        };
    }
}
