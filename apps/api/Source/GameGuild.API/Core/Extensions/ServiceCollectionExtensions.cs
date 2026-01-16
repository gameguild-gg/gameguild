using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using GameGuild.Commerce.Billing;
using GameGuild.Configuration;
using GameGuild.Configuration.PresentationLayer.Authentication;
using GameGuild.Configuration.PresentationLayer.Controllers;
using GameGuild.Configuration.PresentationLayer.CORS;
using GameGuild.Configuration.PresentationLayer.Endpoints;
using GameGuild.Configuration.PresentationLayer.FeatureFlags;
using GameGuild.Configuration.PresentationLayer.GraphQL;
using GameGuild.Configuration.PresentationLayer.HealthChecks;
using GameGuild.Configuration.PresentationLayer.Localization;
using GameGuild.Configuration.PresentationLayer.ModelValidation;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Configuration.PresentationLayer.RequestContext;
using GameGuild.Configuration.PresentationLayer.ResponseCompression;
using GameGuild.Configuration.PresentationLayer.SignalIR;
using GameGuild.Endpoints;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Products;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Transformers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using AuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;
using HttpLoggingOptions = GameGuild.Configuration.PresentationLayer.HttpLogging.HttpLoggingOptions;
using ProblemDetailsOptions = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsOptions;

namespace GameGuild.API;

/// <summary>
///     Extension methods for service collection to add layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection SetupHttpLogging(this IServiceCollection services, IConfiguration configuration,
        HttpLoggingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "HttpLogging",
            HttpLoggingOptions.CreateDefault);
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

    public static IServiceCollection SetupProblemDetails(this IServiceCollection services, IConfiguration configuration,
        ProblemDetailsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ProblemDetails",
            ProblemDetailsOptions.CreateDefault);
        options.Validate();

        services.AddProblemDetails(problemDetailsOptions =>
            {
                problemDetailsOptions.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    if (options.IncludeExceptionDetails && context.Exception != null)
                    {
                        context.ProblemDetails.Extensions["exception"] = context.Exception.ToString();
                    }
                };
            }
        );

        return services;
    }

    public static IServiceCollection SetupLocalization(this IServiceCollection services, IConfiguration configuration,
        LocalizationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Localization",
            LocalizationOptions.CreateDefault);
        options.Validate();

        services.AddLocalization(localizationOptions => { localizationOptions.ResourcesPath = "Resources"; });

        services.Configure<RequestLocalizationOptions>(requestLocalizationOptions =>
            {
                var supportedCultures = options.SupportedCultures;
                requestLocalizationOptions.DefaultRequestCulture = new RequestCulture(options.DefaultCulture);
                requestLocalizationOptions.SupportedCultures =
                    supportedCultures.Select(c => new CultureInfo(c)).ToList();
                requestLocalizationOptions.SupportedUICultures =
                    supportedCultures.Select(c => new CultureInfo(c)).ToList();
            }
        );

        return services;
    }

    public static IServiceCollection SetupResponseCompression(this IServiceCollection services,
        IConfiguration configuration, ResponseCompressionOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ResponseCompression",
            ResponseCompressionOptions.CreateDefault);
        options.Validate();

        services.AddResponseCompression(compressionOptions =>
            {
                compressionOptions.MimeTypes = options.MimeTypes;
                compressionOptions.EnableForHttps = true;
            }
        );

        return services;
    }

    public static IServiceCollection SetupCors(this IServiceCollection services, IConfiguration configuration,
        CorsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Cors", CorsOptions.CreateDefault);
        options.Validate();

        services.AddCors(corsOptions =>
            {
                corsOptions.AddDefaultPolicy(policyBuilder =>
                    {
                        if (options.AllowedOrigins.Length > 0)
                        {
                            policyBuilder.WithOrigins(options.AllowedOrigins);
                        }
                        else
                        {
                            policyBuilder.AllowAnyOrigin();
                        }

                        if (options.AllowedMethods.Length > 0)
                        {
                            policyBuilder.WithMethods(options.AllowedMethods);
                        }
                        else
                        {
                            policyBuilder.AllowAnyMethod();
                        }

                        if (options.AllowedHeaders.Length > 0)
                        {
                            policyBuilder.WithHeaders(options.AllowedHeaders);
                        }
                        else
                        {
                            policyBuilder.AllowAnyHeader();
                        }
                    }
                );
            }
        );

        return services;
    }

    public static IServiceCollection SetupAuthentication(this IServiceCollection services, IConfiguration configuration,
        AuthenticationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Authentication",
            AuthenticationOptions.CreateDefault);
        options.Validate();

        if (!options.EnableAuthentication) return services;

        // Enable PII logging for development/testing to see detailed errors
        IdentityModelEventSource.ShowPII = true;

        // Configure JWT Bearer authentication with fallback for missing config
        var jwtSecret = configuration["Jwt:Secret"] ?? configuration["JwtSettings:SecretKey"] ??
            "default-secret-key-for-development-only-minimum-32-characters-long";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"] ?? "GameGuild";
        var jwtAudience = configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"] ?? "GameGuild";

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");
        logger.LogInformation(
            "JWT Authentication Setup: Secret length: {SecretLength}, Issuer: {Issuer}, Audience: {Audience}", 
            jwtSecret.Length, jwtIssuer, jwtAudience);

        // Create symmetric security key with a KeyId
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            { KeyId = "GameGuild-jwt-key" };

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
                            using var lf = LoggerFactory.Create(b => b.AddConsole());
                            lf.CreateLogger("GameGuild.API").LogWarning("JWT Authentication Failed: {Message}", context.Exception.Message);

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            using var lf = LoggerFactory.Create(b => b.AddConsole());
                            lf.CreateLogger("GameGuild.API").LogInformation("JWT Token Validated for user: {User}", context.Principal?.Identity?.Name);

                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            // Skip logging for non-API paths to reduce noise
                            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                            if (path == "/" || path.StartsWith("/documentation") || path.StartsWith("/swagger") || 
                                path.StartsWith("/openapi") || path.StartsWith("/health") ||
                                path.StartsWith("/ready") || path.StartsWith("/live") ||
                                path.EndsWith(".ico") || path.EndsWith(".js") || path.EndsWith(".css"))
                            {
                                return Task.CompletedTask;
                            }
                            
                            using var lf = LoggerFactory.Create(b => b.AddConsole());
                            lf.CreateLogger("GameGuild.API").LogDebug("JWT Token Received: {TokenPrefix}...", 
                                context.Token?.Substring(0, Math.Min(20, context.Token?.Length ?? 0)));

                            return Task.CompletedTask;
                        }
                    };
                }
            );

        // Add authorization if enabled
        if (options.EnableAuthorization)
        {
            services.AddAuthorization();
        }

        return services;
    }

    public static IServiceCollection SetupRequestContext(this IServiceCollection services, IConfiguration configuration,
        RequestContextOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "RequestContext",
            RequestContextOptions.CreateDefault);
        options.Validate();

        // Placeholder for unified request context registration
        // Future implementation will handle user, tenant, location, and feature flag contexts
        return services;
    }

    public static IServiceCollection SetupFeatureFlags(this IServiceCollection services, IConfiguration configuration,
        FeatureFlagsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "FeatureFlags",
            FeatureFlagsOptions.CreateDefault);
        options.Validate();

        // TODO: Implement OpenFeature services
        // Register OpenFeature API singleton and a thin service wrapper.
        // services.AddSingleton(Api.Instance);
        // services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        // Add hosted service to initialize OpenFeature provider during startup
        // services.AddHostedService<OpenFeatureHostedInitializer>();

        return services;
    }

    public static IServiceCollection SetupAuthorization(this IServiceCollection services, IConfiguration configuration,
        AuthorizationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Authorization",
            AuthorizationOptions.CreateDefault);
        options.Validate();

        // ===== Configuration Options =====
        services.AddAuthorizationOptions(configuration);

        // ===== Presentation Layer (handlers, tenant context, policy provider) =====
        services.AddAuthorizationPresentation();

        // ===== Rule-Based Authorization (DB-driven, tenant-configurable policies) =====
        services.AddRuleBasedAuthorization();

        // ===== Core ASP.NET Authorization with Built-in Policies =====
        services.AddAuthorization(authzOptions =>
        {
            // Core role-based policies (fallback for common use cases)
            authzOptions.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
            authzOptions.AddPolicy("RequireTenantAccess", policy => policy.RequireClaim("TenantId"));
            
            // Authentication policies
            authzOptions.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Anonymous", policy => policy.RequireAssertion(_ => true)); // Always allow
            
            // Tenant policies (require authenticated user and tenant context)
            authzOptions.AddPolicy("TenantMember", policy => policy.RequireAuthenticatedUser().RequireClaim("TenantId"));
            authzOptions.AddPolicy("TenantAdmin", policy => policy.RequireRole("Admin").RequireClaim("TenantId"));
            
            // Admin policies
            authzOptions.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("SecureAdmin", policy => policy.RequireRole("Admin")); // TODO: Add MFA requirement
            
            // User management policies (require authenticated user)
            authzOptions.AddPolicy("Users.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.Create", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("Users.Update", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("Users.Delete", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("Users.Admin", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("Users.Purge", policy => policy.RequireRole("Admin"));
            authzOptions.AddPolicy("Users.ReadSelf", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.EditSelf", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Users.DeleteSelf", policy => policy.RequireAuthenticatedUser());
            
            // Project policies (require authenticated user)
            authzOptions.AddPolicy("Project.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Project.Edit", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Project.Delete", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Project.Owner", policy => policy.RequireAuthenticatedUser());
            
            // Content policies
            authzOptions.AddPolicy("Content.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Content.Edit", policy => policy.RequireAuthenticatedUser());
            
            // Course policies
            authzOptions.AddPolicy("Course.Read", policy => policy.RequireAuthenticatedUser());
            authzOptions.AddPolicy("Course.Manage", policy => policy.RequireAuthenticatedUser());
            
            // Document policies
            authzOptions.AddPolicy("Document.Edit", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }

    public static IServiceCollection SetupRateLimiting(this IServiceCollection services, IConfiguration configuration,
        RateLimitingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "RateLimiting",
            RateLimitingOptions.CreateDefault);
        options.Validate();

        services.AddRateLimiter(rateLimiterOptions =>
            {
                // Global rejection handler for rate limit exceeded
                rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/problem+json";

                    var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                        ? retryAfterValue.TotalSeconds
                        : 60;

                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = $"Rate limit exceeded. Please retry after {retryAfter:F0} seconds.",
                        Instance = context.HttpContext.Request.Path
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                };

                // ============ FIXED WINDOW POLICIES ============

                // Authentication policy: Partitioned by IP (anonymous) or User ID (authenticated)
                // 10 requests per minute to prevent brute-force attacks
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Authentication, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetAuthenticationPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.AuthenticationRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Authorization policy: Partitioned by User ID + Tenant ID
                // 100 requests per minute to prevent DoS on permission evaluation
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Authorization, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetUserTenantPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.AuthorizationRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Internal policy: Relaxed limits for admin/internal endpoints
                // Partitioned by User ID
                // 200 requests per minute
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Internal, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetUserPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 200,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit * 2
                        }));

                // ============ SLIDING WINDOW POLICIES ============

                // API policy: General API endpoints with sliding window for smoother distribution
                // Partitioned by User ID (authenticated) or IP (anonymous)
                // 60 requests per minute for general API calls
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Api, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetUserOrIpPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.ApiRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4, // 4 segments = 15-second buckets
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Per-Tenant policy: Sliding window partitioned by Tenant ID
                // 1000 requests per minute per tenant
                rateLimiterOptions.AddPolicy(RateLimitPolicies.PerTenant, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetTenantPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.TenantRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Per-User policy: Sliding window partitioned by User ID
                // 300 requests per minute per authenticated user
                rateLimiterOptions.AddPolicy(RateLimitPolicies.PerUser, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetUserPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.UserRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Per-IP policy: Sliding window partitioned by IP address for anonymous protection
                // 30 requests per minute per IP
                rateLimiterOptions.AddPolicy(RateLimitPolicies.PerIp, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetIpPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0 // No queuing for per-IP to prevent resource exhaustion
                        }));

                // ============ TOKEN BUCKET POLICIES ============

                // Bursty policy: Token bucket for bursty traffic patterns
                // Partitioned by User ID or IP
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Bursty, httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: GetUserOrIpPartitionKey(httpContext),
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = options.TokenBucketLimit,
                            ReplenishmentPeriod = options.TokenReplenishmentPeriod,
                            TokensPerPeriod = options.TokensPerPeriod,
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // API Key policy: Token bucket partitioned by API key with tiered limits
                rateLimiterOptions.AddPolicy(RateLimitPolicies.ApiKey, httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: GetApiKeyPartitionKey(httpContext),
                        factory: partition => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = partition.StartsWith("premium:")
                                ? options.PremiumApiKeyRequestsPerMinute
                                : options.StandardApiKeyRequestsPerMinute,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            TokensPerPeriod = partition.StartsWith("premium:")
                                ? options.PremiumApiKeyRequestsPerMinute
                                : options.StandardApiKeyRequestsPerMinute,
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // ============ CONCURRENCY POLICIES ============

                // Expensive Operations policy: Concurrency limiter for reports, exports, etc.
                // Partitioned by User ID to limit concurrent expensive operations per user
                rateLimiterOptions.AddPolicy(RateLimitPolicies.ExpensiveOperations, httpContext =>
                    RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: GetUserPartitionKey(httpContext),
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = options.MaxConcurrentRequests,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));
            }
        );

        return services;
    }

    #region Rate Limiting Partition Key Helpers

    /// <summary>
    /// Gets partition key based on IP for anonymous or User ID for authenticated.
    /// Used for authentication endpoints.
    /// </summary>
    private static string GetAuthenticationPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on User ID only.
    /// Falls back to IP for anonymous users.
    /// </summary>
    private static string GetUserPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"anonymous:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on User ID + Tenant ID.
    /// Used for authorization/permission check endpoints.
    /// </summary>
    private static string GetUserTenantPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";

        return $"user:{userId}:tenant:{tenantId}";
    }

    /// <summary>
    /// Gets partition key based on User ID (authenticated) or IP (anonymous).
    /// Used for general API rate limiting.
    /// </summary>
    private static string GetUserOrIpPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on Tenant ID.
    /// Used for per-tenant rate limiting.
    /// </summary>
    private static string GetTenantPartitionKey(HttpContext httpContext)
    {
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
        {
            return $"tenant:{tenantId}";
        }

        // Fall back to IP for requests without tenant context
        return $"no-tenant:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on IP address only.
    /// Used for per-IP rate limiting (anonymous protection).
    /// </summary>
    private static string GetIpPartitionKey(HttpContext httpContext)
    {
        return $"ip:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on API key from header.
    /// Returns tier prefix (premium: or standard:) for tiered limits.
    /// </summary>
    private static string GetApiKeyPartitionKey(HttpContext httpContext)
    {
        var apiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            // No API key - fall back to per-IP limiting
            return $"no-key:{GetClientIpAddress(httpContext)}";
        }

        // Check if this is a premium key (simple prefix check for now)
        // In production, this would check against a key registry
        if (apiKey.StartsWith("pk_", StringComparison.OrdinalIgnoreCase))
        {
            return $"premium:{apiKey}";
        }

        return $"standard:{apiKey}";
    }

    /// <summary>
    /// Gets the client IP address, handling proxies and load balancers.
    /// </summary>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Check X-Forwarded-For header for proxied requests
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first (original client)
            var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstIp))
            {
                return firstIp;
            }
        }

        // Check X-Real-IP header (common with nginx)
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to direct connection IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    #endregion

    public static IServiceCollection SetupModelValidation(this IServiceCollection services,
        IConfiguration configuration, ModelValidationOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "ModelValidation",
            ModelValidationOptions.CreateDefault);
        options.Validate();

        services.Configure<ApiBehaviorOptions>(behaviorOptions =>
        {
            behaviorOptions.SuppressModelStateInvalidFilter = options.SuppressModelStateInvalidFilter;
        });

        return services;
    }

    public static IServiceCollection SetupHealthChecks(this IServiceCollection services, IConfiguration configuration,
        HealthChecksOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "HealthChecks",
            HealthChecksOptions.CreateDefault);
        options.Validate();

        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection SetupSignalR(this IServiceCollection services, IConfiguration configuration,
        SignalROptions? options)
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

    public static IServiceCollection SetupGraphQl(this IServiceCollection services, IConfiguration configuration,
        GraphQlOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "GraphQL", GraphQlOptions.CreateDefault);
        options.Validate();

        // GraphQL services can be configured by the application layer if enabled.

        return services;
    }

    public static IServiceCollection SetupControllers(this IServiceCollection services, IConfiguration configuration,
        ControllersOptions? options)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");
        
        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Setting up controllers...");
        
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Controllers",
            ControllersOptions.CreateDefault);
        options.Validate();

        var controllerStopwatch = Stopwatch.StartNew();
        services.AddControllers(mvcOptions =>
            {
                if (options.UseKebabCaseRoutes)
                {
                    mvcOptions.Conventions.Add(
                        new Microsoft.AspNetCore.Mvc.ApplicationModels.RouteTokenTransformerConvention(
                            new KebabCaseParameterTransformer()));
                }

                // Add permission authorization filter globally to all controllers
                // This provides defense-in-depth by requiring explicit [AllowAnonymous] to opt-out
                // Re-enabled 2026-01-15 per ASSETS_RESOURCES_DEEP_REVIEW.md security audit
                if (options.EnablePermissionAuthorizationFilter)
                    mvcOptions.Filters.Add<ResourcePermissionAuthorizationFilter>();
            }
        )
        .ConfigureApplicationPartManager(manager =>
        {
            // Remove all GameGuild module assemblies from auto-discovery
            var partsToRemove = manager.ApplicationParts
                .Where(part => part.Name.StartsWith("GameGuild.", StringComparison.OrdinalIgnoreCase)
                               && part.Name != "GameGuild.API")
                .ToList();

            foreach (var part in partsToRemove)
            {
                manager.ApplicationParts.Remove(part);
            }
            logger.LogInformation("Removed {Count} auto-discovered module assemblies", partsToRemove.Count);
        })
        .AddApplicationPart(typeof(DependencyInjection).Assembly); // Main API assembly only
        
        // Log individual controllers from GameGuild.API
        LogControllersFromAssembly(typeof(DependencyInjection).Assembly, logger, controllerStopwatch);

        // ===== MODULE CONTROLLERS =====
        // .AddApplicationPart(typeof(GameGuild.Audit.Controllers.MfaController).Assembly) // Authentication module
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(Identity.Users.UsersController).Assembly); // Users module
        
        // Log individual controllers from GameGuild.Users
        LogControllersFromAssembly(typeof(Identity.Users.UsersController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(Identity.Tenants.TenantsController).Assembly); // Tenants module
        
        // Log individual controllers from GameGuild.Tenants
        LogControllersFromAssembly(typeof(Identity.Tenants.TenantsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(Resources.ResourcesController).Assembly); // Resources module
        
        // Log individual controllers from GameGuild.Resources
        LogControllersFromAssembly(typeof(Resources.ResourcesController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(PaymentsController).Assembly); // Payments module
        
        // Log individual controllers from GameGuild.Payments
        LogControllersFromAssembly(typeof(PaymentsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(SubscriptionsController).Assembly); // Subscriptions module
        
        // Log individual controllers from GameGuild.Subscriptions
        LogControllersFromAssembly(typeof(SubscriptionsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(BillingWebhooksController).Assembly); // Billing module
        
        // Log individual controllers from GameGuild.Billing
        LogControllersFromAssembly(typeof(BillingWebhooksController).Assembly, logger, controllerStopwatch);

        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(ProductsController).Assembly); // Products module

        // Log individual controllers from GameGuild.Products
        LogControllersFromAssembly(typeof(ProductsController).Assembly, logger, controllerStopwatch);
        
        controllerStopwatch.Restart();
        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly); // Authentication module
        
        // Log individual controllers from GameGuild.Audit
        LogControllersFromAssembly(typeof(AuthController).Assembly, logger, controllerStopwatch);
        
        services.AddControllers()
            .AddJsonOptions(jsonOptions =>
            {
                jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = options.JsonPropertyNamingPolicy switch
                {
                    "CamelCase" => System.Text.Json.JsonNamingPolicy.CamelCase,
                    "SnakeCaseLower" => System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                    "SnakeCaseUpper" => System.Text.Json.JsonNamingPolicy.SnakeCaseUpper,
                    "KebabCaseLower" => System.Text.Json.JsonNamingPolicy.KebabCaseLower,
                    "KebabCaseUpper" => System.Text.Json.JsonNamingPolicy.KebabCaseUpper,
                    _ => System.Text.Json.JsonNamingPolicy.CamelCase
                };
                jsonOptions.JsonSerializerOptions.WriteIndented = options.WriteIndentedJson;
            });

        totalStopwatch.Stop();
        logger.LogInformation("Completed controller setup in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return services;
    }
    
    /// <summary>
    ///     Logs individual controller names from an assembly with human-readable formatting.
    /// </summary>
    private static void LogControllersFromAssembly(Assembly assembly, ILogger logger, Stopwatch stopwatch)
    {
        var controllerBaseType = typeof(ControllerBase);
        var controllers = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && controllerBaseType.IsAssignableFrom(t))
            .ToList();

        foreach (var controller in controllers)
        {
            var formattedName = FormatControllerName(controller.Name);
            logger.LogInformation("Registered {ControllerName} in {ElapsedMs}ms", formattedName, stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
        }
    }
    
    /// <summary>
    ///     Formats a controller name from PascalCase to human-readable format.
    ///     e.g., "UserProfileController" -> "User Profile Controller"
    /// </summary>
    private static string FormatControllerName(string controllerName)
    {
        // Insert space before each uppercase letter (except the first)
        var formatted = Regex.Replace(controllerName, "([a-z])([A-Z])", "$1 $2");
        // Also handle consecutive uppercase letters like "APIController" -> "API Controller"
        formatted = Regex.Replace(formatted, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return formatted;
    }

    public static IServiceCollection SetupEndpoints(this IServiceCollection services, IConfiguration configuration,
        EndpointsOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "Endpoints",
            EndpointsOptions.CreateDefault);
        options.Validate();

        if (options.RegisterFromMainAssembly)
        {
            // Register minimal API endpoints (IEndpoint implementations)
            services.AddEndpoints(typeof(DependencyInjection).Assembly);
        }

        return services;
    }

    /// <summary>
    ///     Registers custom middleware services.
    ///     Note: Most middlewares are registered implicitly when UseMiddleware&lt;T&gt; is called.
    ///     This method registers any additional services that middlewares depend on.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    ///     Middlewares in this application:
    ///     - CorrelationIdMiddleware: Manages X-Correlation-Id header for distributed tracing
    ///     - SecurityHeadersMiddleware: Adds security headers (CSP, X-Frame-Options, etc.)
    ///     - TenantMiddleware: Resolves current tenant from header/domain/query
    ///     
    ///     These middlewares use the "method injection" pattern where dependencies
    ///     are resolved via InvokeAsync parameters rather than constructor injection.
    ///     This is the recommended pattern for middlewares that need scoped services.
    /// </remarks>
    public static IServiceCollection SetupMiddlewares(this IServiceCollection services, IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("GameGuild.API");

        var totalStopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting middleware setup...");

        var stepStopwatch = Stopwatch.StartNew();

        // CorrelationIdMiddleware - No additional services needed (uses ILogger via DI)
        stepStopwatch.Restart();
        // Middleware is registered implicitly via UseMiddleware<T>()
        logger.LogInformation("Registered Correlation Id Middleware in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        // SecurityHeadersMiddleware - No additional services needed (uses ILogger via DI)
        stepStopwatch.Restart();
        // Middleware is registered implicitly via UseMiddleware<T>()
        logger.LogInformation("Registered Security Headers Middleware in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        // TenantMiddleware - Depends on IMediator (CQRS) and ITenantDomainsRepository
        // Uses queries: GetTenantByIdQuery, GetDefaultTenantQuery
        // These are registered by the Tenants module infrastructure layer
        stepStopwatch.Restart();
        // Middleware is registered implicitly via UseMiddleware<T>()
        logger.LogInformation("Registered Tenant Resolution Middleware in {ElapsedMs}ms", stepStopwatch.ElapsedMilliseconds);

        totalStopwatch.Stop();
        logger.LogInformation("Completed middleware setup in {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);

        return services;
    }
}