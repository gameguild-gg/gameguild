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
      // Log context information for debugging
      if (userContext.IsAuthenticated) {
        _logger.LogDebug(
          "User context: UserId={UserId}, Email={Email}, TenantId={TenantId}",
          userContext.UserId,
          userContext.Email,
          tenantContext.TenantId
        );
      }

      // Add context information to HttpContext items for easy access
      context.Items["UserContext"] = userContext;
      context.Items["TenantContext"] = tenantContext;
      context.Items["UserId"] = userContext.UserId;
      context.Items["TenantId"] = tenantContext.TenantId;

      // Validate tenant if user is authenticated
      if (userContext.IsAuthenticated && tenantContext.TenantId == null) { _logger.LogWarning("Authenticated user {UserId} has no tenant context", userContext.UserId); }

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
    services.AddScoped<IUserContext, UserContext>();
    services.AddScoped<ITenantContext, TenantContext>();

    return services;
  }
}
