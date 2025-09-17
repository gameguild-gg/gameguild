using System.Globalization;
using GameGuild.Modules.Features.Infrastructure;
using GameGuild.Modules.Features.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using OpenFeature;


namespace GameGuild;

/// <summary>
/// Extension methods for service collection to add layer services.
/// </summary>
public static class ServiceCollectionExtensions {
  /// <summary>
  /// Adds the presentation layer services to the service collection.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The configuration</param>
  /// <returns>The service collection for chaining</returns>
  public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration) { return DependencyInjection.AddPresentationLayer(services, configuration); }

  /// <summary>
  /// Adds the presentation layer services to the service collection with custom options.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The configuration</param>
  /// <param name="options">Custom presentation layer options</param>
  /// <returns>The service collection for chaining</returns>
  public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration, PresentationLayerOptions options) { return DependencyInjection.AddPresentationLayer(services, configuration, options); }

  public static IServiceCollection SetupHttpLogging(this IServiceCollection services, IConfiguration configuration, HttpLoggingOptions? options) {
    options ??= HttpLoggingOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddHttpLogging(loggingOptions => {
        loggingOptions.LoggingFields = HttpLoggingFields.All;

        if (options.LogRequestHeaders) loggingOptions.LoggingFields |= HttpLoggingFields.RequestHeaders;
        if (options.LogResponseHeaders) loggingOptions.LoggingFields |= HttpLoggingFields.ResponseHeaders;
        if (options.LogRequestBody) loggingOptions.LoggingFields |= HttpLoggingFields.RequestBody;
        if (options.LogResponseBody) loggingOptions.LoggingFields |= HttpLoggingFields.ResponseBody;
      }
    );

    return services;
  }

  public static IServiceCollection SetupProblemDetails(this IServiceCollection services, IConfiguration configuration, ProblemDetailsOptions? options) {
    options ??= ProblemDetailsOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddProblemDetails(problemDetailsOptions => {
        problemDetailsOptions.CustomizeProblemDetails = context => {
          context.ProblemDetails.Instance = context.HttpContext.Request.Path;
          context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

          if (options.IncludeExceptionDetails && context.Exception != null) context.ProblemDetails.Extensions["exception"] = context.Exception.ToString();
        };
      }
    );

    return services;
  }

  public static IServiceCollection SetupLocalization(this IServiceCollection services, IConfiguration configuration, LocalizationOptions? options) {
    options ??= LocalizationOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddLocalization(localizationOptions => { localizationOptions.ResourcesPath = "Resources"; });

    services.Configure<RequestLocalizationOptions>(requestLocalizationOptions => {
        var supportedCultures = options.SupportedCultures;
        requestLocalizationOptions.DefaultRequestCulture = new RequestCulture(options.DefaultCulture);
        requestLocalizationOptions.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
        requestLocalizationOptions.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
      }
    );

    return services;
  }

  public static IServiceCollection SetupResponseCompression(this IServiceCollection services, IConfiguration configuration, ResponseCompressionOptions? options) {
    options ??= ResponseCompressionOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddResponseCompression(compressionOptions => {
        compressionOptions.MimeTypes = options.MimeTypes;
        compressionOptions.EnableForHttps = true;
      }
    );

    return services;
  }

  public static IServiceCollection SetupCors(this IServiceCollection services, IConfiguration configuration, CorsOptions? options) {
    options ??= CorsOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddCors(corsOptions => {
        corsOptions.AddDefaultPolicy(policyBuilder => {
            if (options.AllowedOrigins.Length > 0)
              policyBuilder.WithOrigins(options.AllowedOrigins);
            else
              policyBuilder.AllowAnyOrigin();

            if (options.AllowedMethods.Length > 0)
              policyBuilder.WithMethods(options.AllowedMethods);
            else
              policyBuilder.AllowAnyMethod();

            if (options.AllowedHeaders.Length > 0)
              policyBuilder.WithHeaders(options.AllowedHeaders);
            else
              policyBuilder.AllowAnyHeader();
          }
        );
      }
    );

    return services;
  }

  public static IServiceCollection SetupAuthentication(this IServiceCollection services, IConfiguration configuration, AuthenticationOptions? options) {
    options ??= AuthenticationOptionsBuilder.Create(configuration);
    options.Validate();

    if (!options.EnableAuthentication) return services;

    // Add basic authentication services - the application will configure specific schemes
    services.AddAuthentication();

    // Add authorization if enabled
    if (options.EnableAuthorization) { services.AddAuthorization(); }

    return services;
  }

  public static IServiceCollection SetupRequestContext(this IServiceCollection services, IConfiguration configuration, RequestContextOptions? options) {
    options ??= RequestContextOptionsBuilder.Create(configuration);
    options.Validate();

    // Placeholder for unified request context registration
    // Future implementation will handle user, tenant, location, and feature flag contexts
    return services;
  }

  public static IServiceCollection SetupFeatureFlags(this IServiceCollection services, IConfiguration configuration, FeatureFlagsOptions? options) {
    options ??= FeatureFlagsOptionsBuilder.Create(configuration);
    options.Validate();

    // Register OpenFeature API singleton and services
    services.AddSingleton(Api.Instance);
    // TODO: 
    // services.AddSingleton<FeatureClient>(provider => provider.GetRequiredService<Api>().GetFeatureClient());

    // Register the main feature flag service
    services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

    // Register OpenFeature-specific service for advanced scenarios
    services.AddSingleton<IOpenFeatureFlagService, FeatureFlagOpenFeatureService>();

    // Add hosted service to initialize OpenFeature provider during startup
    services.AddHostedService<OpenFeatureHostedInitializer>();

    return services;
  }

  public static IServiceCollection SetupAuthorization(this IServiceCollection services, IConfiguration configuration, AuthorizationOptions? options) {
    options ??= AuthorizationOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddAuthorization();

    return services;
  }

  public static IServiceCollection SetupRateLimiting(this IServiceCollection services, IConfiguration configuration, RateLimitingOptions? options) {
    options ??= RateLimitingOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddRateLimiter(rateLimiterOptions => {
        // TODO: Fix rate limiter configuration for .NET 9
        // These methods may not be available in .NET 9
        // Fixed window for internal API calls and the Console application.
        // rateLimiterOptions.AddFixedWindowLimiter("InternalPolicy", policyOptions => { ... });
        // Sliding window for public API calls with rate limiting.
        // rateLimiterOptions.AddSlidingWindowLimiter("PublicPolicy", policyOptions => { ... });
      }
    );

    return services;
  }

  public static IServiceCollection SetupModelValidation(this IServiceCollection services, IConfiguration configuration, ModelValidationOptions? options) {
    options ??= ModelValidationOptionsBuilder.Create(configuration);
    options.Validate();

    services.Configure<ApiBehaviorOptions>(behaviorOptions => { behaviorOptions.SuppressModelStateInvalidFilter = options.SuppressModelStateInvalidFilter; });

    return services;
  }

  public static IServiceCollection SetupHealthChecks(this IServiceCollection services, IConfiguration configuration, HealthChecksOptions? options) {
    options ??= HealthChecksOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddHealthChecks();

    return services;
  }

  public static IServiceCollection SetupApiVersioning(this IServiceCollection services, IConfiguration configuration, ApiVersioningOptions? options) {
    options ??= ApiVersioningOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddApiVersioning(setup => {
                setup.AssumeDefaultVersionWhenUnspecified = options.AssumeDefaultVersionWhenUnspecified;
                setup.DefaultApiVersion = options.DefaultApiVersion;
                setup.ApiVersionReader = ApiVersioningOptionsBuilder.CreateReader(options.ReadingStrategy, options);
              }
            )
            .AddApiExplorer(setup => {
                setup.GroupNameFormat = options.GroupNameFormat;
                setup.SubstituteApiVersionInUrl = options.SubstituteApiVersionInUrl;
              }
            );

    return services;
  }

  public static IServiceCollection SetupApiExplorer(this IServiceCollection services, IConfiguration configuration, ApiVersioningOptions? options) {
    options ??= ApiVersioningOptionsBuilder.Create(configuration);
    options.Validate();

    // API Explorer is now configured as part of API Versioning setup
    services.AddEndpointsApiExplorer();

    return services;
  }

  public static IServiceCollection SetupOpenApi(this IServiceCollection services, IConfiguration configuration, OpenApiOptions? options) {
    options ??= OpenApiOptionsBuilder.Create(configuration);
    options.Validate();

    // Add native .NET 9 OpenAPI support
    services.AddOpenApi();

    // Add Swashbuckle for Swagger UI
    services.AddSwaggerGen(genOptions => {
        genOptions.SwaggerDoc(
          options.Version,
          new OpenApiInfo {
            Title = options.Title,
            Version = options.Version,
            Description = options.Description,
            Contact = new OpenApiContact { Name = options.ContactName, Email = options.ContactEmail, Url = !string.IsNullOrEmpty(options.ContactUrl) ? new Uri(options.ContactUrl) : null },
            License = BuildLicense(options),
            TermsOfService = !string.IsNullOrEmpty(options.TermsOfServiceUrl) ? new Uri(options.TermsOfServiceUrl) : null
          }
        );

        // Use fully-qualified type names (without root namespace) to avoid collisions for nested record types
        // e.g. TenantsController+ArchiveRequest vs ResourcesController+ArchiveRequest
        genOptions.CustomSchemaIds(t => {
            var fullName = t.FullName ?? t.Name;
            // Strip common root namespace to keep schema ids concise
            fullName = fullName.Replace("GameGuild.", string.Empty, StringComparison.Ordinal);

            // Replace '+' (nested type separator) with '.' for readability
            return fullName.Replace('+', '.');
          }
        );
      }
    );

    return services;

    static OpenApiLicense? BuildLicense(OpenApiOptions options) {
      if (string.IsNullOrWhiteSpace(options.LicenseName)) return null;

      var license = new OpenApiLicense { Name = options.LicenseName };
      if (!string.IsNullOrWhiteSpace(options.LicenseUrl)) license.Url = new Uri(options.LicenseUrl);

      return license;
    }
  }

  public static IServiceCollection SetupMemoryCaching(this IServiceCollection services, IConfiguration configuration, MemoryCachingOptions? options) {
    options ??= MemoryCachingOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddMemoryCache(cacheOptions => {
        cacheOptions.SizeLimit = options.SizeLimit;
        cacheOptions.CompactionPercentage = options.CompactionPercentage;
        cacheOptions.ExpirationScanFrequency = options.ExpirationScanFrequency;
      }
    );

    return services;
  }

  public static IServiceCollection SetupResponseCaching(this IServiceCollection services, IConfiguration configuration, ResponseCachingOptions? options) {
    options ??= ResponseCachingOptionsBuilder.Create(configuration);
    options.Validate();

    services.AddResponseCaching(cachingOptions => {
        cachingOptions.MaximumBodySize = options.MaximumBodySize;
        cachingOptions.UseCaseSensitivePaths = options.UseCaseSensitivePaths;
      }
    );

    return services;
  }

  public static IServiceCollection SetupSignalR(this IServiceCollection services, IConfiguration configuration, SignalROptions? options) {
    options ??= SignalROptionsBuilder.Create(configuration);
    options.Validate();

    services.AddSignalR(signalROptions => {
        signalROptions.EnableDetailedErrors = options.EnableDetailedErrors;
        signalROptions.KeepAliveInterval = options.KeepAliveInterval;
        signalROptions.ClientTimeoutInterval = options.ClientTimeoutInterval;
        signalROptions.MaximumReceiveMessageSize = options.MaximumReceiveMessageSize;
      }
    );

    return services;
  }

  public static IServiceCollection SetupGraphQl(this IServiceCollection services, IConfiguration configuration, GraphQlOptions? options) {
    options ??= GraphQlOptionsBuilder.Create(configuration);
    options.Validate();

    // GraphQL services can be configured by the application layer if enabled.

    return services;
  }
}
