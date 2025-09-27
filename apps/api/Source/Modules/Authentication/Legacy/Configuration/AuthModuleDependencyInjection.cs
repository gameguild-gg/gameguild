using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication.Services;

namespace GameGuild.Modules.Authentication;

/// <summary> Authentication module dependency injection configuration following CQRS, GraphQL, and REST best practices. Implements Clean Architecture with clear separation of concerns. </summary>
public static class AuthModuleDependencyInjection {
  /// <summary> Registers all Authentication module services following Clean Architecture layers. </summary>
  /// <param name="services"> The service collection </param>
  /// <param name="configuration"> Application configuration </param>
  /// <returns> The configured service collection </returns>
  public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration) {
    return services.AddAuthServices().AddAuthHandlers().AddAuthValidators().AddAuthAuthentication(configuration).AddAuthGraphQl().AddAuthControllers();
  }

  /// <summary> Registers core authentication services. </summary>
  private static IServiceCollection AddAuthServices(this IServiceCollection services) {
    // Core authentication services
    services.AddScoped<IAuthService, EnhancedAuthService>(); // Use enhanced version with security features
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IOAuthService, OAuthService>();
    services.AddScoped<IWeb3Service, Web3Service>();
    services.AddScoped<IEmailVerificationService, EmailVerificationService>();
    services.AddScoped<ITenantAuthService, TenantAuthService>();

    // MFA and session management services
    services.AddScoped<IMfaService, MfaService>();
    services.AddScoped<ISessionManagementService, SessionManagementService>();
    services.AddScoped<IEncryptionService, EncryptionService>();

    // Security services
    services.AddScoped<IAuthenticationAnomalyDetectionService, AuthenticationAnomalyDetectionService>();
    services.AddScoped<IUserEnumerationProtectionService, UserEnumerationProtectionService>();

    // Audit logging service
    services.AddScoped<IAuditService, AuditService>();

    // HTTP context accessor for IP tracking
    services.AddHttpContextAccessor();

    // HTTP clients for external services
    services.AddHttpClient<IOAuthService, OAuthService>();

    return services;
  }

  /// <summary> Registers CQRS command and query handlers. </summary>
  private static IServiceCollection AddAuthHandlers(this IServiceCollection services) {
    // Get the authentication module assembly
    var authAssembly = typeof(AuthModuleDependencyInjection).Assembly;

    // Register all GameGuild.CQRS handlers from the authentication module
    services.AddCqrs(authAssembly);

    return services;
  }

  /// <summary> Registers FluentValidation validators for commands and queries. </summary>
  private static IServiceCollection AddAuthValidators(this IServiceCollection services) {
    // Register validators for CQRS commands and queries
    services.AddScoped<IValidator<LocalSignUpCommand>, LocalSignUpCommandValidator>();
    services.AddScoped<IValidator<LocalSignInCommand>, LocalSignInCommandValidator>();
    services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
    services.AddScoped<IValidator<RevokeTokenCommand>, RevokeTokenCommandValidator>();
    services.AddScoped<IValidator<GetUserProfileQuery>, GetUserProfileQueryValidator>();

    return services;
  }

  /// <summary> Configures JWT authentication and authorization. </summary>
  private static IServiceCollection AddAuthAuthentication(this IServiceCollection services, IConfiguration configuration) {
    // Configure JWT authentication
    services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

    // Configure session management options
    services.Configure<Services.SessionOptions>(configuration.GetSection("SessionManagement"));

    // Add JWT authentication configuration
    services.AddAuthJwtConfiguration(configuration);

    // Add authorization policies
    services.AddAuthorizationPolicies();

    return services;
  }

  /// <summary> Registers GraphQL types and extensions. </summary>
  private static IServiceCollection AddAuthGraphQl(this IServiceCollection services) {
    // GraphQL types are registered automatically by HotChocolate type discovery
    // This method exists for explicit registration if needed
    return services;
  }

  /// <summary> Configures REST API controllers. </summary>
  private static IServiceCollection AddAuthControllers(this IServiceCollection services) {
    // Controllers are automatically registered by ASP.NET Core
    // This method exists for any controller-specific configuration
    return services;
  }

  /// <summary> Configures the authentication middleware pipeline. </summary>
  /// <param name="app"> The application builder </param>
  /// <returns> The configured application builder </returns>
  public static IApplicationBuilder UseAuthModule(this IApplicationBuilder app) { return app.UseAuthentication().UseAuthorization().UseMiddleware<JwtAuthenticationMiddleware>(); }
}