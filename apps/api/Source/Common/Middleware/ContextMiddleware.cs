namespace GameGuild.Common;

/// <summary>
/// Middleware to set up user and tenant context for requests
/// </summary>
public class ContextMiddleware {
  private readonly RequestDelegate _next;
  private readonly ILogger<ContextMiddleware> _logger;

  public ContextMiddleware(RequestDelegate next, ILogger<ContextMiddleware> logger) {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context, IUserContext userContext, ITenantContext tenantContext) {
    try {
      // Extract and log token information for debugging
      var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
      if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) {
        var token = authHeader["Bearer ".Length..].Trim();
        _logger.LogDebug("Processing request with JWT token (length: {TokenLength})", token.Length);
      }

      // Log context information for debugging
      if (userContext.IsAuthenticated) {
        _logger.LogDebug(
          "User context: UserId={UserId}, Email={Email}, TenantId={TenantId}, IsAuthenticated={IsAuthenticated}",
          userContext.UserId,
          userContext.Email,
          tenantContext.TenantId,
          userContext.IsAuthenticated
        );

        // Log all user claims for debugging (in debug builds only)
        if (_logger.IsEnabled(LogLevel.Debug)) {
          var claims = string.Join(", ", userContext.Claims.Select(kvp => $"{kvp.Key}={kvp.Value}"));
          _logger.LogDebug("User claims: {Claims}", claims);
        }
      }
      else {
        _logger.LogDebug("Request with no authenticated user context");
      }

      // Add context information to HttpContext items for easy access
      context.Items["UserContext"] = userContext;
      context.Items["TenantContext"] = tenantContext;
      context.Items["UserId"] = userContext.UserId;
      context.Items["TenantId"] = tenantContext.TenantId;

      // Validate tenant if user is authenticated
      if (userContext.IsAuthenticated && tenantContext.TenantId == null) {
        _logger.LogWarning("Authenticated user {UserId} has no tenant context", userContext.UserId);
      }

      await _next(context);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error in context middleware");
      throw;
    }
  }
}

/// <summary>
/// Extension methods for adding context middleware
/// </summary>
public static class ContextMiddlewareExtensions {
  /// <summary>
  /// Adds context middleware to the application pipeline
  /// </summary>
  public static IApplicationBuilder UseContextMiddleware(this IApplicationBuilder builder) { return builder.UseMiddleware<ContextMiddleware>(); }

  /// <summary>
  /// Adds context services to the service collection
  /// </summary>
  public static IServiceCollection AddContextServices(this IServiceCollection services) {
    services.AddHttpContextAccessor();
    services.AddScoped<IUserContext, UsersContext>();
    services.AddScoped<ITenantContext, TenantContext>();

    return services;
  }
}
