using FluentValidation;

namespace GameGuild.Modules.Authentication;

/// <summary> Extension methods for registering Authentication module services </summary>
public static class AuthenticationModule
{
    /// <summary> Registers all Authentication module services </summary>
    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication repositories
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthenticationAttemptRepository, AuthenticationAttemptRepository>();

        // Register core authentication services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IWeb3Service, Web3Service>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<ITenantAuthService, TenantAuthService>();

        // Register security services
        services.AddScoped<IAuthenticationAnomalyDetectionService, AuthenticationAnomalyDetectionService>();
        services.AddScoped<IUserEnumerationProtectionService, UserEnumerationProtectionService>();

        // Register validators for CQRS commands
        services.AddScoped<IValidator<LocalSignUpCommand>, LocalSignUpCommandValidator>();
        services.AddScoped<IValidator<LocalSignInCommand>, LocalSignInCommandValidator>();
        services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddScoped<IValidator<RevokeTokenCommand>, RevokeTokenCommandValidator>();

        // HTTP context accessor for IP tracking
        services.AddHttpContextAccessor();

        // HTTP clients for external services
        services.AddHttpClient<IOAuthService, OAuthService>();

        // Configure JWT authentication
        services.AddAuthJwtConfiguration(configuration);

        // CQRS handlers are automatically registered by assembly scanning

        return services;
    }

    /// <summary> Adds authentication middleware to the application pipeline </summary>
    public static IApplicationBuilder UseAuthenticationModule(this IApplicationBuilder app) { return app.UseAuthentication().UseAuthorization(); }
}
