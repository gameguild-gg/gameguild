using GameGuild.Authentication.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Authentication.Presentation;

/// <summary>
///     Authentication module configuration for comprehensive permission management system
///     Integrates Domain, Application, Infrastructure, and Presentation layers
/// </summary>
public static class AuthenticationModule
{
    /// <summary>
    ///     Register all authentication services including advanced authorization features
    /// </summary>
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Application layer services
        services.AddAuthenticationApplication();

        // Register Data layer services  
        services.AddAuthenticationData(configuration);

        // Register Presentation layer services
        services.AddAuthenticationPresentation(configuration);

        return services;
    }

    /// <summary>
    ///     Configure the authentication module in the application pipeline
    /// </summary>
    public static IApplicationBuilder UseAuthenticationModule(this IApplicationBuilder app)
    {
        // Configure authentication middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Configure permission caching middleware
        app.UseMiddleware<PermissionCachingMiddleware>();

        // Configure ABAC policy evaluation middleware
        app.UseMiddleware<AbacPolicyMiddleware>();

        // Configure access review middleware for compliance
        app.UseMiddleware<AccessReviewMiddleware>();

        return app;
    }

    /// <summary>
    ///     Register presentation layer services
    /// </summary>
    private static IServiceCollection AddAuthenticationPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // TEMPORARY: Disabled CQRS controllers to use Minimal API endpoints
        // The AuthController has incomplete implementation (mocked user creation)
        // TODO: Re-enable once User module is implemented and AuthService.LocalSignUpAsync is complete
        // services.AddControllers()
        //     .AddApplicationPart(typeof(AuthenticationModule).Assembly);

        // Note: Swagger is configured centrally by the API host
        // Authentication endpoints will be automatically included in the main Swagger document

        // TODO: Register these services when permission module is implemented
        // services.AddScoped<IPermissionAuthorizationService, PermissionAuthorizationService>();
        // services.AddScoped<IAbacPolicyEvaluationService, AbacPolicyEvaluationService>();
        // services.AddScoped<IConditionalPolicyEvaluationService, ConditionalPolicyEvaluationService>();
        // services.AddScoped<IAccessReviewOrchestrationService, AccessReviewOrchestrationService>();

        // NOTE: Middleware should NOT be registered as services
        // Middleware is added via app.UseMiddleware<T>() in the pipeline
        // services.AddScoped<PermissionCachingMiddleware>();
        // services.AddScoped<AbacPolicyMiddleware>();
        // services.AddScoped<AccessReviewMiddleware>();

        // Configure CORS for API access
        services.AddCors(options => { options.AddPolicy("AuthenticationPolicy", builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); }); });

        // TODO: Configure API versioning when package is added
        // services.AddApiVersioning(options =>
        // {
        //     options.DefaultApiVersion = new(1, 0);
        //     options.AssumeDefaultVersionWhenUnspecified = true;
        //     options.ApiVersionReader = Microsoft.AspNetCore.Mvc.ApiVersionReader.Combine(
        //         new Microsoft.AspNetCore.Mvc.QueryStringApiVersionReader("version"),
        //         new Microsoft.AspNetCore.Mvc.HeaderApiVersionReader("X-Version")
        //     );
        // });

        // Configure response compression
        services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            }
        );

        return services;
    }
}
