using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using GameGuild.API.Authorization;
using GameGuild.SharedKernel.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ApiVersioningOptions = GameGuild.SharedKernel.Configuration.ApiVersioningOptions;
using HttpLoggingOptions = GameGuild.SharedKernel.Configuration.HttpLoggingOptions;
using ProblemDetailsOptions = GameGuild.SharedKernel.Configuration.ProblemDetailsOptions;

namespace GameGuild.Core;

/// <summary>
///     Extension methods for service collection to add layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the presentation layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration) { return DependencyInjection.AddPresentationLayer(services, configuration); }

    /// <summary>
    ///     Adds the presentation layer services to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="options">Custom presentation layer options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration, PresentationLayerOptions options)
    {
        return DependencyInjection.AddPresentationLayer(services, configuration, options);
    }

    public static IServiceCollection SetupHttpLogging(this IServiceCollection services, IConfiguration configuration, HttpLoggingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "HttpLogging", HttpLoggingOptions.CreateDefault);
        options.Validate();

        services.AddHttpLogging(loggingOptions =>
            {
                loggingOptions.LoggingFields = HttpLoggingFields.All;

                if (options.LogRequestHeaders) loggingOptions.LoggingFields |= HttpLoggingFields.RequestHeaders;
                if (options.LogResponseHeaders) loggingOptions.LoggingFields |= HttpLoggingFields.ResponseHeaders;
                if (options.LogRequestBody) loggingOptions.LoggingFields |= HttpLoggingFields.RequestBody;
                if (options.LogResponseBody) loggingOptions.LoggingFields |= HttpLoggingFields.ResponseBody;
            }
        );

        return services;
    }

    public static IServiceCollection SetupProblemDetails(this IServiceCollection services, IConfiguration configuration, ProblemDetailsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ProblemDetails", ProblemDetailsOptions.CreateDefault);
        options.Validate();

        services.AddProblemDetails(problemDetailsOptions =>
            {
                problemDetailsOptions.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    if (options.IncludeExceptionDetails && context.Exception != null) { context.ProblemDetails.Extensions["exception"] = context.Exception.ToString(); }
                };
            }
        );

        return services;
    }

    public static IServiceCollection SetupLocalization(this IServiceCollection services, IConfiguration configuration, LocalizationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Localization", LocalizationOptions.CreateDefault);
        options.Validate();

        services.AddLocalization(localizationOptions => { localizationOptions.ResourcesPath = "Resources"; });

        services.Configure<RequestLocalizationOptions>(requestLocalizationOptions =>
            {
                var supportedCultures = options.SupportedCultures;
                requestLocalizationOptions.DefaultRequestCulture = new RequestCulture(options.DefaultCulture);
                requestLocalizationOptions.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
                requestLocalizationOptions.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
            }
        );

        return services;
    }

    public static IServiceCollection SetupResponseCompression(this IServiceCollection services, IConfiguration configuration, ResponseCompressionOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ResponseCompression", ResponseCompressionOptions.CreateDefault);
        options.Validate();

        services.AddResponseCompression(compressionOptions =>
            {
                compressionOptions.MimeTypes = options.MimeTypes;
                compressionOptions.EnableForHttps = true;
            }
        );

        return services;
    }

    public static IServiceCollection SetupCors(this IServiceCollection services, IConfiguration configuration, CorsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Cors", CorsOptions.CreateDefault);
        options.Validate();

        services.AddCors(corsOptions =>
            {
                corsOptions.AddDefaultPolicy(policyBuilder =>
                    {
                        if (options.AllowedOrigins.Length > 0) { policyBuilder.WithOrigins(options.AllowedOrigins); }
                        else { policyBuilder.AllowAnyOrigin(); }

                        if (options.AllowedMethods.Length > 0) { policyBuilder.WithMethods(options.AllowedMethods); }
                        else { policyBuilder.AllowAnyMethod(); }

                        if (options.AllowedHeaders.Length > 0) { policyBuilder.WithHeaders(options.AllowedHeaders); }
                        else { policyBuilder.AllowAnyHeader(); }
                    }
                );
            }
        );

        return services;
    }

    public static IServiceCollection SetupAuthentication(this IServiceCollection services, IConfiguration configuration, AuthenticationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Authentication", AuthenticationOptions.CreateDefault);
        options.Validate();

        if (!options.EnableAuthentication) return services;

        // Enable PII logging for development/testing to see detailed errors
        IdentityModelEventSource.ShowPII = true;

        // Configure JWT Bearer authentication with fallback for missing config
        var jwtSecret = configuration["Jwt:Secret"] ?? configuration["JwtSettings:SecretKey"] ?? "default-secret-key-for-development-only-minimum-32-characters-long";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"] ?? "GameGuild";
        var jwtAudience = configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"] ?? "GameGuild";

        Console.WriteLine($"[Auth Setup] JWT Secret length: {jwtSecret?.Length}, Issuer: {jwtIssuer}, Audience: {jwtAudience}");

        // Create symmetric security key with a KeyId
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret ?? string.Empty)) { KeyId = "GameGuild-jwt-key" };

        services.AddAuthentication(authOptions =>
                {
                    authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    authOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                }
            )
            .AddJwtBearer(jwtOptions =>
                {
                    jwtOptions.SaveToken = true;
                    jwtOptions.RequireHttpsMetadata = false; // Allow HTTP in development/testing

                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = securityKey,
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        // Map JWT claim names to .NET claim types
                        NameClaimType = "sub",
                        RoleClaimType = "role",
                        // Try all keys even if kid doesn't match
                        TryAllIssuerSigningKeys = true
                    };

                    // Add event handlers for debugging
                    jwtOptions.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine($"JWT Token Validated for user: {context.Principal?.Identity?.Name}");

                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            Console.WriteLine($"JWT Token Received: {context.Token?.Substring(0, Math.Min(20, context.Token?.Length ?? 0))}...");

                            return Task.CompletedTask;
                        }
                    };
                }
            );

        // Add authorization if enabled
        if (options.EnableAuthorization) { services.AddAuthorization(); }

        return services;
    }

    public static IServiceCollection SetupRequestContext(this IServiceCollection services, IConfiguration configuration, RequestContextOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "RequestContext", RequestContextOptions.CreateDefault);
        options.Validate();

        // Placeholder for unified request context registration
        // Future implementation will handle user, tenant, location, and feature flag contexts
        return services;
    }

    public static IServiceCollection SetupFeatureFlags(this IServiceCollection services, IConfiguration configuration, FeatureFlagsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "FeatureFlags", FeatureFlagsOptions.CreateDefault);
        options.Validate();

        // TODO: Implement OpenFeature services
        // Register OpenFeature API singleton and a thin service wrapper.
        // services.AddSingleton(Api.Instance);
        // services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        // Add hosted service to initialize OpenFeature provider during startup
        // services.AddHostedService<OpenFeatureHostedInitializer>();

        return services;
    }

    public static IServiceCollection SetupAuthorization(this IServiceCollection services, IConfiguration configuration, AuthorizationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Authorization", AuthorizationOptions.CreateDefault);
        options.Validate();

        services.AddAuthorization();

        // Register permission-based authorization filter
        services.AddScoped<PermissionAuthorizationFilter>();

        return services;
    }

    public static IServiceCollection SetupRateLimiting(this IServiceCollection services, IConfiguration configuration, RateLimitingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "RateLimiting", RateLimitingOptions.CreateDefault);
        options.Validate();

        services.AddRateLimiter(rateLimiterOptions =>
            {
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

    public static IServiceCollection SetupModelValidation(this IServiceCollection services, IConfiguration configuration, ModelValidationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ModelValidation", ModelValidationOptions.CreateDefault);
        options.Validate();

        services.Configure<ApiBehaviorOptions>(behaviorOptions => { behaviorOptions.SuppressModelStateInvalidFilter = options.SuppressModelStateInvalidFilter; });

        return services;
    }

    public static IServiceCollection SetupHealthChecks(this IServiceCollection services, IConfiguration configuration, HealthChecksOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "HealthChecks", HealthChecksOptions.CreateDefault);
        options.Validate();

        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection SetupApiVersioning(this IServiceCollection services, IConfiguration configuration, ApiVersioningOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ApiVersioning", ApiVersioningOptions.CreateDefault);
        options.Validate();

        services.AddApiVersioning(setup =>
                {
                    setup.AssumeDefaultVersionWhenUnspecified = options.AssumeDefaultVersionWhenUnspecified;
                    // Parse DefaultVersion (e.g., "1.0") into ApiVersion
                    var versionParts = options.DefaultVersion?.Split('.') ?? ["1", "0"];
                    var major = int.TryParse(versionParts.ElementAtOrDefault(0) ?? "1", out var mj) ? mj : 1;
                    var minor = int.TryParse(versionParts.ElementAtOrDefault(1) ?? "0", out var mn) ? mn : 0;
                    setup.DefaultApiVersion = new ApiVersion(major, minor);
                    setup.ApiVersionReader = ApiVersioningOptionsBuilder.CreateReader(options.ReadingStrategy, options);
                }
            )
            .AddApiExplorer(setup =>
                {
                    setup.GroupNameFormat = options.GroupNameFormat;
                    setup.SubstituteApiVersionInUrl = options.SubstituteApiVersionInUrl;
                }
            );

        return services;
    }

    public static IServiceCollection SetupApiExplorer(this IServiceCollection services, IConfiguration configuration, ApiVersioningOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ApiVersioning", ApiVersioningOptions.CreateDefault);
        options.Validate();

        // API Explorer is now configured as part of API Versioning setup
        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection SetupOpenApi(this IServiceCollection services, IConfiguration configuration, OpenApiOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "OpenApi", OpenApiOptions.CreateDefault);
        options.Validate();

        // Add native .NET 9 OpenAPI support
        services.AddOpenApi();

        // Add Swashbuckle for Swagger UI
        services.AddSwaggerGen(c =>
            {
                // If API Versioning is enabled, register a Swagger document per discovered API version
                using var providerScope = services.BuildServiceProvider();
                var provider = providerScope.GetService<IApiVersionDescriptionProvider>();

                if (provider is not null)
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        c.SwaggerDoc(
                            description.GroupName,
                            new OpenApiInfo
                            {
                                Title = options.Title,
                                Version = description.ApiVersion.ToString(),
                                Description = options.Description,
                                Contact = new OpenApiContact { Name = options.ContactName, Email = options.ContactEmail, Url = !string.IsNullOrEmpty(options.ContactUrl) ? new Uri(options.ContactUrl) : null }
                            }
                        );
                    }

                    // Ensure only endpoints from the corresponding API version are included in each document
                    // Check API version instead of GroupName to allow custom ApiExplorerSettings GroupName
                    c.DocInclusionPredicate((docName, apiDesc) =>
                        {
                            if (apiDesc.ActionDescriptor is ControllerActionDescriptor cad)
                            {
                                var apiVersionAttr = cad.ControllerTypeInfo.GetCustomAttributes(typeof(ApiVersionAttribute), false).FirstOrDefault() as ApiVersionAttribute;

                                if (apiVersionAttr != null)
                                {
                                    var version = apiVersionAttr.Versions.FirstOrDefault();

                                    return version != null && docName.Equals($"v{version.MajorVersion}", StringComparison.OrdinalIgnoreCase);
                                }
                            }

                            return string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase);
                        }
                    );
                }
                else
                {
                    // Fallback to single document when versioning is not configured
                    c.SwaggerDoc(
                        options.Version,
                        new OpenApiInfo
                        {
                            Title = options.Title,
                            Version = options.Version,
                            Description = options.Description,
                            Contact = new OpenApiContact { Name = options.ContactName, Email = options.ContactEmail, Url = !string.IsNullOrEmpty(options.ContactUrl) ? new Uri(options.ContactUrl) : null }
                        }
                    );
                }

                // Configure schema ID generator with smart naming that avoids namespace pollution
                // Uses simple names when possible, but includes parent namespace segment for disambiguation
                c.CustomSchemaIds(type =>
                {
                    string GetFriendlyTypeName(Type t)
                    {
                        if (!t.IsGenericType)
                        {
                            return t.Name;
                        }

                        // For generic types, include type parameter names
                        var genericTypeName = t.Name.Split('`')[0];
                        var genericArgs = t.GetGenericArguments()
                            .Select(GetFriendlyTypeName)
                            .ToArray();
                        return $"{genericTypeName}Of{string.Join("And", genericArgs)}";
                    }

                    var friendlyName = GetFriendlyTypeName(type);

                    // For Commands, Queries, and DTOs, include the parent namespace segment to avoid conflicts
                    // Example: GrantTenantPermissionCommand exists in both Authentication and Permissions modules
                    if (type.FullName != null &&
                        (friendlyName.EndsWith("Command") ||
                         friendlyName.EndsWith("Query") ||
                         friendlyName.EndsWith("Dto") ||
                         friendlyName.EndsWith("Request") ||
                         friendlyName.EndsWith("Response")))
                    {
                        // Get the module name (e.g., "Authentication", "Permissions", "Users")
                        var parts = type.FullName.Split('.');
                        var moduleIndex = Array.FindIndex(parts, p => p == "GameGuild") + 1;

                        if (moduleIndex > 0 && moduleIndex < parts.Length)
                        {
                            var moduleName = parts[moduleIndex];
                            // Only prefix if it's a module name (not "API", "Core", "Modules", etc.)
                            if (moduleName != "API" && moduleName != "Core" && moduleName != "Modules")
                            {
                                return $"{moduleName}{friendlyName}";
                            }
                        }
                    }

                    return friendlyName;
                });

                // Apply schema filter for enum naming with x-enum-varnames extension
                c.SchemaFilter<EnumSchemaFilter>();

                // Add security definition for JWT Bearer token
                c.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    }
                );

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }, Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header },
                            new List<string>()
                        }
                    }
                );
            }
        );

        return services;
    }

    public static IServiceCollection SetupMemoryCaching(this IServiceCollection services, IConfiguration configuration, MemoryCachingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "MemoryCaching", MemoryCachingOptions.CreateDefault);
        options.Validate();

        services.AddMemoryCache(cacheOptions =>
            {
                cacheOptions.SizeLimit = options.SizeLimit;
                cacheOptions.CompactionPercentage = options.CompactionPercentage;
                cacheOptions.ExpirationScanFrequency = options.ExpirationScanFrequency;
            }
        );

        return services;
    }

    public static IServiceCollection SetupResponseCaching(this IServiceCollection services, IConfiguration configuration, ResponseCachingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ResponseCaching", ResponseCachingOptions.CreateDefault);
        options.Validate();

        services.AddResponseCaching(cachingOptions =>
            {
                cachingOptions.MaximumBodySize = options.MaximumBodySize;
                cachingOptions.UseCaseSensitivePaths = options.UseCaseSensitivePaths;
            }
        );

        return services;
    }

    public static IServiceCollection SetupSignalR(this IServiceCollection services, IConfiguration configuration, SignalROptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "SignalR", SignalROptions.CreateDefault);
        options.Validate();

        services.AddSignalR(signalROptions =>
            {
                signalROptions.EnableDetailedErrors = options.EnableDetailedErrors;
                signalROptions.KeepAliveInterval = options.KeepAliveInterval;
                signalROptions.ClientTimeoutInterval = options.ClientTimeoutInterval;
                signalROptions.MaximumReceiveMessageSize = options.MaximumReceiveMessageSize;
            }
        );

        return services;
    }

    public static IServiceCollection SetupGraphQl(this IServiceCollection services, IConfiguration configuration, GraphQlOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "GraphQL", GraphQlOptions.CreateDefault);
        options.Validate();

        // GraphQL services can be configured by the application layer if enabled.

        return services;
    }
}
