using System.Text.Json;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service collection extensions for Authentication Presentation layer
///     Provides comprehensive registration of all authentication and authorization services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Add Authentication Presentation layer services
    /// </summary>
    public static IServiceCollection AddAuthenticationPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure authentication options
        services.Configure<AuthenticationModuleOptions>(configuration.GetSection(AuthenticationModuleOptions.SectionName));
        services.Configure<SmsMfaOptions>(configuration.GetSection(SmsMfaOptions.SectionName));

        // Register presentation services
        RegisterPresentationServices(services);

        // Register authorization services
        RegisterAuthorizationServices(services);

        // Register policy evaluation services
        RegisterPolicyEvaluationServices(services);

        // Register access review services
        RegisterAccessReviewServices(services);

        // Register caching services
        RegisterCachingServices(services);

        // Register audit services
        RegisterAuditServices(services);

        return services;
    }

    /// <summary>
    ///     Register core presentation services
    /// </summary>
    private static void RegisterPresentationServices(IServiceCollection services)
    {
        // Register API controllers
        services.AddControllers()
            .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = true;
                }
            );

        services.TryAddScoped<IModelValidationService, ModelValidationService>();
        services.TryAddScoped<IResponseFormattingService, ResponseFormattingService>();
        services.TryAddScoped<IErrorHandlingService, ErrorHandlingService>();
        services.TryAddScoped<ISmsService, LoggingSmsService>();
    }

    /// <summary>
    ///     Register authorization services
    /// </summary>
    private static void RegisterAuthorizationServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionService, PermissionService>();

        services.TryAddScoped<GameGuild.Identity.Authorization.IPermissionAnalyticsService, GameGuild.Identity.Authorization.PermissionAnalyticsService>();
        services.TryAddScoped<GameGuild.Identity.Authorization.IPermissionAuditService, GameGuild.Identity.Authorization.PermissionAuditService>();
    }

    /// <summary>
    ///     Register policy evaluation services
    /// </summary>
    private static void RegisterPolicyEvaluationServices(IServiceCollection services)
    {
        services.TryAddScoped<GameGuild.Identity.Authorization.IAbacPolicyEvaluator, GameGuild.Identity.Authorization.AbacPolicyEvaluator>();
        services.TryAddScoped<GameGuild.Identity.Authorization.IConditionalPolicyEvaluator, GameGuild.Identity.Authorization.ConditionalPolicyEvaluator>();
    }

    /// <summary>
    ///     Register access review services
    /// </summary>
    private static void RegisterAccessReviewServices(IServiceCollection services)
    {
        services.TryAddScoped<GameGuild.Identity.Authorization.IAccessReviewService, GameGuild.Identity.Authorization.AccessReviewService>();
    }

    /// <summary>
    ///     Register caching services
    /// </summary>
    private static void RegisterCachingServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddOptions<AuthorizationCacheOptions>();
        services.TryAddSingleton<GameGuild.Identity.Authorization.IPolicyCache, GameGuild.Identity.Authorization.MemoryPolicyCache>();
    }

    /// <summary>
    ///     Register audit services
    /// </summary>
    private static void RegisterAuditServices(IServiceCollection services)
    {
        services.TryAddScoped<GameGuild.Identity.Authorization.IPolicyEvaluationLogger, GameGuild.Identity.Authorization.PolicyEvaluationLogger>();
    }

    /// <summary>
    ///     Configure health checks for authentication services
    /// </summary>
    public static IServiceCollection AddAuthenticationHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("authentication-presentation", () => HealthCheckResult.Healthy("Authentication presentation services registered."))
            .AddCheck("permission-service", () => HealthCheckResult.Healthy("Permission services registered."))
            .AddCheck("policy-evaluation", () => HealthCheckResult.Healthy("Policy evaluation services registered."))
            .AddCheck("access-review", () => HealthCheckResult.Healthy("Access review services registered."))
            .AddCheck("permission-cache", () => HealthCheckResult.Healthy("Permission cache services registered."));

        return services;
    }

    /// <summary>
    ///     Configure metrics collection for authentication services
    /// </summary>
    public static IServiceCollection AddAuthenticationMetrics(this IServiceCollection services)
    {
        services.AddMetrics();
        services.TryAddSingleton<IAuthenticationMetricsRecorder, AuthenticationMetricsRecorder>();

        return services;
    }
}
