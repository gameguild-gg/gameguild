namespace GameGuild;

public static class PresentationLayerOptionsBuilder
{
    public static PresentationLayerOptions Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new PresentationLayerOptions();

        var section = configuration.GetSection("PresentationLayer");

        if (section.Exists()) section.Bind(options);

        options.ApiVersioning ??= ApiVersioningOptionsBuilder.Create(configuration);
        options.OpenApi ??= OpenApiOptionsBuilder.Create(configuration);
        options.Authentication ??= AuthenticationOptionsBuilder.Create(configuration);
        options.HttpLogging ??= HttpLoggingOptionsBuilder.Create(configuration);
        options.ProblemDetails ??= ProblemDetailsOptionsBuilder.Create(configuration);
        options.Localization ??= LocalizationOptionsBuilder.Create(configuration);
        options.ResponseCompression ??= ResponseCompressionOptionsBuilder.Create(configuration);
        options.Cors ??= CorsOptionsBuilder.Create(configuration);
        options.RequestContext ??= RequestContextOptionsBuilder.Create(configuration);
        options.Authorization ??= AuthorizationOptionsBuilder.Create(configuration);
        options.RateLimiting ??= RateLimitingOptionsBuilder.Create(configuration);
        options.ModelValidation ??= ModelValidationOptionsBuilder.Create(configuration);
        options.ApiExplorer ??= ApiExplorerOptionsBuilder.Create(configuration);
        options.HealthChecks ??= HealthChecksOptionsBuilder.Create(configuration);
        options.ResponseCaching ??= ResponseCachingOptionsBuilder.Create(configuration);
        options.MemoryCaching ??= MemoryCachingOptionsBuilder.Create(configuration);
        options.SignalR ??= SignalROptionsBuilder.Create(configuration);
        options.GraphQl ??= GraphQlOptionsBuilder.Create(configuration);
        options.FeatureFlags ??= FeatureFlagsOptionsBuilder.Create(configuration);

        return options;
    }
}
