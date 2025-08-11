using FluentValidation;
using GameGuild.Modules.Authentication.Validators;
using MediatR;
using Microsoft.Extensions.Logging;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Authentication module dependency injection configuration following CQRS, GraphQL, and REST best practices.
/// Implements Clean Architecture with clear separation of concerns.
/// </summary>
public static class AuthModuleDependencyInjection {
  /// <summary>
  /// Registers all Authentication module services following Clean Architecture layers.
  /// </summary>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">Application configuration</param>
  /// <returns>The configured service collection</returns>
  public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration) {
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
  private static IServiceCollection AddAuthServices(this IServiceCollection services) {
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
  private static IServiceCollection AddAuthHandlers(this IServiceCollection services) {
    // Get the authentication module assembly
    var authAssembly = typeof(AuthModuleDependencyInjection).Assembly;

    // Register all MediatR handlers from the authentication module
    services.AddMediatR(authAssembly);

    return services;
  }

  /// <summary>
  /// Registers FluentValidation validators for commands and queries.
  /// </summary>
  private static IServiceCollection AddAuthValidators(this IServiceCollection services) {
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
  private static IServiceCollection AddAuthAuthentication(this IServiceCollection services, IConfiguration configuration) {
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
  private static IServiceCollection AddAuthGraphQL(this IServiceCollection services) {
    // GraphQL types are registered automatically by HotChocolate type discovery
    // This method exists for explicit registration if needed
    return services;
  }

  /// <summary>
  /// Configures REST API controllers.
  /// </summary>
  private static IServiceCollection AddAuthControllers(this IServiceCollection services) {
    // Controllers are automatically registered by ASP.NET Core
    // This method exists for any controller-specific configuration
    return services;
  }

  /// <summary>
  /// Configures the authentication middleware pipeline.
  /// </summary>
  /// <param name="app">The application builder</param>
  /// <returns>The configured application builder</returns>
  public static IApplicationBuilder UseAuthModule(this IApplicationBuilder app) {
    return app
           .UseAuthentication()
           .UseAuthorization()
           .UseMiddleware<JwtAuthenticationMiddleware>();
  }
}

/// <summary>
/// JWT configuration options.
/// </summary>
public class JwtOptions {
  public const string SectionName = "Jwt";

  public string SecretKey { get; set; } = string.Empty;

  public string Issuer { get; set; } = string.Empty;

  public string Audience { get; set; } = string.Empty;

  public int ExpirationMinutes { get; set; } = 60;

  public int RefreshTokenExpirationDays { get; set; } = 7;

  /// <summary>
  /// Applies fallback values with warnings when environment variables are not set.
  /// </summary>
  /// <param name="configuration">Application configuration</param>
  public void ApplyFallbacksWithWarnings(IConfiguration configuration) {
    var logger = GetLogger();

    if (string.IsNullOrEmpty(SecretKey)) {
      SecretKey = "game-guild-production-jwt-secret-key-must-be-at-least-32-characters-long-and-secure";
      logger?.LogWarning("JWT SecretKey not found in configuration. Using fallback value. Please set Jwt__SecretKey environment variable for production.");
    }

    if (string.IsNullOrEmpty(Issuer)) {
      Issuer = "GameGuild.API";
      logger?.LogWarning("JWT Issuer not found in configuration. Using fallback value 'GameGuild.API'. Please set Jwt__Issuer environment variable.");
    }

    if (string.IsNullOrEmpty(Audience)) {
      Audience = "GameGuild.Users";
      logger?.LogWarning("JWT Audience not found in configuration. Using fallback value 'GameGuild.Users'. Please set Jwt__Audience environment variable.");
    }

    if (ExpirationMinutes <= 0) {
      ExpirationMinutes = 15;
      logger?.LogWarning("JWT ExpirationMinutes not found or invalid in configuration. Using fallback value '15'. Please set Jwt__ExpirationMinutes environment variable.");
    }

    if (RefreshTokenExpirationDays <= 0) {
      RefreshTokenExpirationDays = 7;
      logger?.LogWarning("JWT RefreshTokenExpirationDays not found or invalid in configuration. Using fallback value '7'. Please set Jwt__RefreshTokenExpirationDays environment variable.");
    }
  }

  private ILogger? GetLogger() {
    try {
      var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
      return loggerFactory.CreateLogger<JwtOptions>();
    }
    catch {
      // If logger creation fails, return null and continue without logging
      return null;
    }
  }

  public void Validate() {
    if (string.IsNullOrEmpty(SecretKey)) throw new InvalidOperationException("JWT SecretKey is required");
    if (string.IsNullOrEmpty(Issuer)) throw new InvalidOperationException("JWT Issuer is required");
    if (string.IsNullOrEmpty(Audience)) throw new InvalidOperationException("JWT Audience is required");
  }
}
