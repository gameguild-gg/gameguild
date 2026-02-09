using GameGuild.Configuration.InfrastructureLayer.MemoryCaching;
using GameGuild.Configuration.PresentationLayer.ApiExplorer;
using GameGuild.Configuration.PresentationLayer.ApiVersioning;
using GameGuild.Configuration.PresentationLayer.Authentication;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Configuration.PresentationLayer.Controllers;
using GameGuild.Configuration.PresentationLayer.CORS;
using GameGuild.Configuration.PresentationLayer.Endpoints;
using GameGuild.Configuration.PresentationLayer.FeatureFlags;
using GameGuild.Configuration.PresentationLayer.GraphQL;
using GameGuild.Configuration.PresentationLayer.HealthChecks;
using GameGuild.Configuration.PresentationLayer.HttpLogging;
using GameGuild.Configuration.PresentationLayer.Localization;
using GameGuild.Configuration.PresentationLayer.ModelValidation;
using GameGuild.Configuration.PresentationLayer.OpenAPI;
using GameGuild.Configuration.PresentationLayer.ProblemDetails;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Configuration.PresentationLayer.RequestContext;
using GameGuild.Configuration.PresentationLayer.ResponseCaching;
using GameGuild.Configuration.PresentationLayer.ResponseCompression;
using GameGuild.Configuration.PresentationLayer.SignalR;

namespace GameGuild.Configuration.PresentationLayer;

/// <summary>
///     Core presentation layer configuration options that can be shared across modules
/// </summary>
public sealed class PresentationLayerOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "PresentationLayer";

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

    public bool EnableGraphQL { get; set; }

    public bool EnableFeatureFlags { get; set; }

    public bool EnableControllers { get; set; } = true;

    public bool EnableEndpoints { get; set; } = true;

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

    public GraphQLOptions? GraphQL { get; set; }

    public OpenApiOptions? OpenApi { get; set; }

    public ApiExplorerOptions? ApiExplorer { get; set; }

    public ControllersOptions? Controllers { get; set; }

    public EndpointsOptions? Endpoints { get; set; }

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
        Controllers?.Validate();
        Endpoints?.Validate();
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
            GraphQL = GraphQLOptions.CreateDefault(),
            OpenApi = OpenApiOptions.CreateDefault(),
            ApiExplorer = ApiExplorerOptions.CreateDefault(),
            Controllers = ControllersOptions.CreateDefault(),
            Endpoints = EndpointsOptions.CreateDefault()
        };
    }
}
