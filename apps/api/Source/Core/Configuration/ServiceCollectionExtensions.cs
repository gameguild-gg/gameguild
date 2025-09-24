using System.Globalization;
using GameGuild.Core.REST;
using GameGuild.Database;
using GameGuild.Modules.Features.Infrastructure;
using GameGuild.Modules.Features.Services;
using GameGuild.Modules.Posts;
using GameGuild.Modules.Products;
using GameGuild.Modules.Programs;
using GameGuild.Modules.UserAchievements;
using GameGuild.Source.GraphQL;
using GameGuild.Source.Modules.Programs.GraphQL;
using HotChocolate;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenFeature;


namespace GameGuild;

/// <summary> Extension methods for service collection to add layer services. </summary>
public static class ServiceCollectionExtensions {
  /// <summary> Adds the presentation layer services to the service collection. </summary>
  /// <param name="services"> The service collection </param>
  /// <param name="configuration"> The configuration </param>
  /// <returns> The service collection for chaining </returns>
  public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration) { return DependencyInjection.AddPresentationLayer(services, configuration); }

  /// <summary> Adds the presentation layer services to the service collection with custom options. </summary>
  /// <param name="services"> The service collection </param>
  /// <param name="configuration"> The configuration </param>
  /// <param name="options"> Custom presentation layer options </param>
  /// <returns> The service collection for chaining </returns>
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

    // Debug logging to see what CORS options are being applied
    Console.WriteLine($"CORS Configuration - AllowedOrigins: [{string.Join(", ", options.AllowedOrigins)}]");
    Console.WriteLine($"CORS Configuration - AllowCredentials: {options.AllowCredentials}");
    Console.WriteLine($"CORS Configuration - AllowedOrigins Length: {options.AllowedOrigins.Length}");

    services.AddCors(corsOptions => {
      corsOptions.AddDefaultPolicy(policyBuilder => {
        if (options.AllowedOrigins.Length > 0 && !options.AllowedOrigins.Contains("*")) {
          Console.WriteLine("CORS: Using specific origins with credentials");
          policyBuilder.WithOrigins(options.AllowedOrigins);
          if (options.AllowCredentials) {
            policyBuilder.AllowCredentials();
          }
        }
        else if (options.AllowedOrigins.Length > 0 && options.AllowedOrigins.Contains("*")) {
          Console.WriteLine("CORS: Using AllowAnyOrigin (no credentials)");
          policyBuilder.AllowAnyOrigin();
        }
        else {
          Console.WriteLine("CORS: No origins configured, using AllowAnyOrigin (no credentials)");
          policyBuilder.AllowAnyOrigin();
        }

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

    // Register our custom database provider
    services.AddSingleton<DatabaseFeatureFlagProvider>();

    // Register OpenFeature API singleton  
    services.AddSingleton(Api.Instance);

    // Register FeatureClient with our database provider
    services.AddSingleton<FeatureClient>(provider => {
      var api = provider.GetRequiredService<Api>();
      var databaseProvider = provider.GetRequiredService<DatabaseFeatureFlagProvider>();

      // Set the database provider as the default provider for OpenFeature
      api.SetProviderAsync(databaseProvider).GetAwaiter().GetResult();

      return api.GetClient();
    });    // Register the main feature flag service (now uses OpenFeature with database provider)
    services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

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

    // Register rate limiting options
    services.Configure<RateLimitingOptions>(config => {
      config.RequestsPerMinute = options.RequestsPerMinute;
      config.BurstSize = options.BurstSize;
      config.ExemptPaths = options.ExemptPaths;
      config.AuthRequestsPerMinute = options.AuthRequestsPerMinute;
      config.GraphQLRequestsPerMinute = options.GraphQLRequestsPerMinute;
      config.PaymentRequestsPerMinute = options.PaymentRequestsPerMinute;
      config.RequestsPerMinutePerIP = options.RequestsPerMinutePerIP;
      config.BurstSizePerIP = options.BurstSizePerIP;
      config.RequestsPerMinutePerUser = options.RequestsPerMinutePerUser;
      config.BurstSizePerUser = options.BurstSizePerUser;
      config.RedisConnectionString = options.RedisConnectionString;
      config.UseDistributedRateLimiting = options.UseDistributedRateLimiting;
      config.EndpointSpecificLimits = options.EndpointSpecificLimits;
    });

    // Register rate limiting service
    services.AddScoped<GameGuild.Core.Services.IRateLimitingService, GameGuild.Core.Services.RateLimitingService>();

    // Add distributed cache if Redis is configured
    if (options.UseDistributedRateLimiting && !string.IsNullOrWhiteSpace(options.RedisConnectionString)) {
      services.AddStackExchangeRedisCache(redisOptions => {
        redisOptions.Configuration = options.RedisConnectionString;
        redisOptions.InstanceName = "GameGuild_RateLimit";
      });
    }
    else {
      // Add in-memory distributed cache as fallback
      services.AddDistributedMemoryCache();
    }

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

  /// <summary>
  /// Setup REST conventions including status codes, ETag support, and response standardization
  /// </summary>
  public static IServiceCollection SetupRestConventions(this IServiceCollection services, IConfiguration configuration) {
    // TODO: Restore REST conventions after fixing corrupted files
    // services.AddRestConventions();

    // Configure enhanced API versioning with REST patterns
    /*
    services.AddRestApiVersioning(options => {
      options.Strategy = ApiVersionStrategy.UrlSegmentAndQuery;
      options.SupportedVersions = new List<Asp.Versioning.ApiVersion>
      {
        new(1, 0),
        new(1, 1),
        new(2, 0)
      };
      options.DeprecatedVersions = new List<Asp.Versioning.ApiVersion>();
      options.IncludeVersionInSwagger = true;
      options.AutoDeprecateOldVersions = false;
    });

    // Add versioned Swagger documentation
    services.AddVersionedSwagger();
    */

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
          TermsOfService = !string.IsNullOrEmpty(options.TermsOfServiceUrl) ? new Uri(options.TermsOfServiceUrl) : null,
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
    })
    .AddJsonProtocol(jsonOptions => {
      // Configure SignalR JSON options to match application-wide settings
      jsonOptions.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
      jsonOptions.PayloadSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
      jsonOptions.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
      jsonOptions.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
      jsonOptions.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });

    return services;
  }

  public static IServiceCollection SetupGraphQl(this IServiceCollection services, IConfiguration configuration, GraphQlOptions? options) {
    options ??= GraphQlOptionsBuilder.Create(configuration);
    options.Validate();

    // Add GraphQL server with HotChocolate
    var builder = services
      .AddGraphQLServer()
      .AddFiltering()
      .AddSorting()
      .AddProjections()
      // Add security middleware for depth analysis
      .AddMaxExecutionDepthRule(options.MaxDepth)
      // Add HotChocolate authorization support
      .AddAuthorization()
      // Add DAC authorization
      .AddDACAuthorization()
      // Add root types
      .AddQueryType<GameGuild.GraphQL.Query>()
      .AddMutationType<GameGuild.GraphQL.Mutation>()
      // Add modules' GraphQL types
      // .AddPostsGraphQL()  // DISABLED: Extension method doesn't exist
      .AddUserAchievementsGraphQL()
      .AddProductGraphQl()
      .AddProgramGraphQL()
      .AddProgramContentGraphQL()
      .AddContentInteractionGraphQL();

    // Configure GraphQL HTTP options
    services.Configure<HotChocolate.AspNetCore.GraphQLHttpOptions>(httpOptions => {
      var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;
      httpOptions.EnableGetRequests = options.EnableIntrospection && !isProduction;
      httpOptions.EnableMultipartRequests = true;
    });

    // Configure GraphQL server options
    services.Configure<HotChocolate.Execution.Options.RequestExecutorOptions>(executorOptions => {
      executorOptions.ExecutionTimeout = options.Timeout;
      var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;
      executorOptions.IncludeExceptionDetails = !isProduction;
    });

    return services;
  }

  /// <summary> Adds database context with proper configuration and pooling </summary>
  /// <param name="services"> The service collection </param>
  /// <param name="configuration"> The application configuration </param>
  /// <returns> The service collection for chaining </returns>
  public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration) {
    // Build database options from configuration
    var dbOptions = InfrastructureConfiguration.CreateDatabaseOptions(configuration);

    // Configure pooling size based on environment
    var poolSize = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch {
      "Development" => 16,  // Lower for development
      "Testing" => 8,       // Even lower for tests
      _ => 64               // Production default
    };

    // Add DbContextPool for improved performance and reduced allocations
    services.AddDbContextPool<ApplicationDbContext>(options => {
      InfrastructureConfiguration.ConfigureDbContext(options, dbOptions);

      // Enable sensitive data logging only in development for pooled contexts
      if (dbOptions.EnableSensitiveDataLogging) {
        options.EnableSensitiveDataLogging();
      }

      // Enable query splitting for better performance with complex includes
      // options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);  // DISABLED: Method doesn't exist in current EF version
    }, poolSize);

    // Add DbContextFactory for GraphQL DataLoaders (compatible with pooling)
    services.AddDbContextFactory<ApplicationDbContext>(options => {
      InfrastructureConfiguration.ConfigureDbContext(options, dbOptions);

      // DataLoader contexts also benefit from query splitting
      // options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);  // DISABLED: Method doesn't exist in current EF version
    });

    return services;
  }
}
