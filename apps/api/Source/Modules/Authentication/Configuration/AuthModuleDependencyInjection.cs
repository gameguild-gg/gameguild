using System.Reflection;
using FluentValidation;
using GameGuild.Common.GraphQL;
using MediatR;


namespace GameGuild.Modules.Auth;

/// <summary>
/// Authentication module dependency injection configuration following CQRS, GraphQL, and REST best practices.
/// Implements Clean Architecture with clear separation of concerns.
/// </summary>
public static class AuthModuleDependencyInjection
{
    /// <summary>
    /// Registers all Authentication module services following Clean Architecture layers.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddAuthServices()
            .AddAuthHandlers()
            .AddAuthValidators()
            .AddAuthAuthentication(configuration)
            .AddAuthGraphQL()
            .AddAuthControllers();
    }

    /// <summary>
    /// Registers core authentication services.
    /// </summary>
    private static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        // Core authentication services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IWeb3Service, Web3Service>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<ITenantAuthService, TenantAuthService>();

        // HTTP clients for external services
        services.AddHttpClient<IOAuthService, OAuthService>();

        return services;
    }

    /// <summary>
    /// Registers CQRS command and query handlers.
    /// </summary>
    private static IServiceCollection AddAuthHandlers(this IServiceCollection services)
    {
        // Get the authentication module assembly
        var authAssembly = typeof(AuthModuleDependencyInjection).Assembly;

        // Register all MediatR handlers from the authentication module
        services.AddMediatR(authAssembly);

        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators for commands and queries.
    /// </summary>
    private static IServiceCollection AddAuthValidators(this IServiceCollection services)
    {
        // Register validators for CQRS commands and queries
        services.AddScoped<IValidator<LocalSignUpCommand>, LocalSignUpCommandValidator>();
        services.AddScoped<IValidator<LocalSignInCommand>, LocalSignInCommandValidator>();
        services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddScoped<IValidator<RevokeTokenCommand>, RevokeTokenCommandValidator>();
        services.AddScoped<IValidator<GetUserProfileQuery>, GetUserProfileQueryValidator>();

        return services;
    }

    /// <summary>
    /// Configures JWT authentication and authorization.
    /// </summary>
    private static IServiceCollection AddAuthAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure JWT authentication
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        
        // Add JWT authentication configuration
        services.AddAuthJwtConfiguration(configuration);
        
        // Add authorization policies
        services.AddAuthorizationPolicies();

        return services;
    }

    /// <summary>
    /// Registers GraphQL types and extensions.
    /// </summary>
    private static IServiceCollection AddAuthGraphQL(this IServiceCollection services)
    {
        // GraphQL types are registered automatically by HotChocolate type discovery
        // This method exists for explicit registration if needed
        return services;
    }

    /// <summary>
    /// Configures REST API controllers.
    /// </summary>
    private static IServiceCollection AddAuthControllers(this IServiceCollection services)
    {
        // Controllers are automatically registered by ASP.NET Core
        // This method exists for any controller-specific configuration
        return services;
    }

    /// <summary>
    /// Configures the authentication middleware pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The configured application builder</returns>
    public static IApplicationBuilder UseAuthModule(this IApplicationBuilder app)
    {
        return app
            .UseAuthentication()
            .UseAuthorization()
            .UseMiddleware<JwtAuthenticationMiddleware>();
    }
}

/// <summary>
/// JWT configuration options.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(SecretKey))
            throw new InvalidOperationException("JWT SecretKey is required");
        if (string.IsNullOrEmpty(Issuer))
            throw new InvalidOperationException("JWT Issuer is required");
        if (string.IsNullOrEmpty(Audience))
            throw new InvalidOperationException("JWT Audience is required");
    }
}
