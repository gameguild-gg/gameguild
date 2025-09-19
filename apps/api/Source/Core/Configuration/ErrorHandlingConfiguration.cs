using GameGuild;

namespace GameGuild.Core.Configuration;

/// <summary>
/// Configuration for global error handling and RFC 7807 ProblemDetails
/// </summary>
public static class ErrorHandlingConfiguration {
    /// <summary>
    /// Configures global error handling with RFC 7807 ProblemDetails
    /// </summary>
    public static IServiceCollection SetupErrorHandling(this IServiceCollection services, IConfiguration configuration, ErrorHandlingOptions? options = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        options ??= new ErrorHandlingOptions();
        options.Validate();

        // Global exception handling is configured via middleware in the application pipeline
        // The existing GlobalExceptionHandler is already registered and provides:
        // - RFC 7807 ProblemDetails compliance
        // - Structured logging with correlation IDs
        // - Consistent error response format
        // - Security-aware error details (no sensitive info in production)

        // No additional service registration needed as GlobalExceptionHandler
        // is already configured in the existing infrastructure

        return services;
    }
}
