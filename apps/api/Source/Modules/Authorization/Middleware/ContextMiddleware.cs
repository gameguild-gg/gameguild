using GameGuild.Core.Domain.Identity;

namespace GameGuild.Authorization.Middleware;

/// <summary> Middleware to set up user and tenant context for requests </summary>
public class ContextMiddleware {
  private readonly ILogger<ContextMiddleware> _logger;

  private readonly RequestDelegate _next;

  public ContextMiddleware(RequestDelegate next, ILogger<ContextMiddleware> logger) {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context, IUserContext userContext, ITenantContext tenantContext, IPermissionsContext permissionsContext, IResourceContext resourceContext, ILocalizationContext localizationContext) {
    try {
      // Extract and log token information for debugging
      var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

      if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) {
        var token = authHeader["Bearer ".Length..].Trim();
        _logger.LogDebug("Processing request with JWT token (length: {TokenLength})", token.Length);
      }

      // Log comprehensive context information for debugging - using Information level to ensure visibility
      var requestPath = context.Request.Path.Value ?? "unknown";
      var requestMethod = context.Request.Method;

      if (userContext.IsAuthenticated) {
        // Log main context information at Information level for better visibility
        _logger.LogInformation("🔍 [CONTEXT] {Method} {Path} | User: {UserId} ({Email}) | Tenant: {TenantId} | Auth: {IsAuthenticated}",
          requestMethod, requestPath, userContext.UserId, userContext.Email, tenantContext.TenantId, userContext.IsAuthenticated);

        // Log permissions context with role information
        _logger.LogInformation("🔐 [PERMISSIONS] User: {UserId} | SystemAdmin: {IsSystemAdmin} | TenantAdmin: {IsTenantAdmin} | Tenant: {TenantId}",
          userContext.UserId, permissionsContext.IsSystemAdmin, permissionsContext.IsTenantAdmin, tenantContext.TenantId);

        // Log resource context if available
        if (resourceContext.ResourceId.HasValue) {
          _logger.LogInformation("📋 [RESOURCE] User: {UserId} | ResourceId: {ResourceId} | ResourceType: {ResourceType} | Identifier: {ResourceIdentifier}",
            userContext.UserId, resourceContext.ResourceId, resourceContext.ResourceType, resourceContext.GetResourceIdentifier());
        }
        else {
          _logger.LogInformation("📋 [RESOURCE] User: {UserId} | No specific resource context", userContext.UserId);
        }

        // Log tenant information if available
        if (tenantContext.TenantId.HasValue) {
          _logger.LogInformation("🏢 [TENANT] User: {UserId} | TenantId: {TenantId} | TenantName: {TenantName}",
            userContext.UserId, tenantContext.TenantId, tenantContext.TenantName ?? "Unknown");
        }

        // Log user claims at debug level for detailed troubleshooting
        if (_logger.IsEnabled(LogLevel.Debug)) {
          var claims = string.Join(", ", userContext.Claims.Select(kvp => $"{kvp.Key}={kvp.Value}"));
          _logger.LogDebug("👤 [CLAIMS] User: {UserId} | Claims: {Claims}", userContext.UserId, claims);
        }
      }
      else {
        _logger.LogInformation("🔍 [CONTEXT] {Method} {Path} | User: UNAUTHENTICATED | No user context", requestMethod, requestPath);
      }

      // Add context information to HttpContext items for easy access
      context.Items["UserContext"] = userContext;
      context.Items["TenantContext"] = tenantContext;
      context.Items["PermissionsContext"] = permissionsContext;
      context.Items["ResourceContext"] = resourceContext;
      context.Items["LocalizationContext"] = localizationContext;
      context.Items["UserId"] = userContext.UserId;
      context.Items["TenantId"] = tenantContext.TenantId;

      // Validate tenant if user is authenticated
      if (userContext.IsAuthenticated && tenantContext.TenantId == null) { _logger.LogWarning("Authenticated user {UserId} has no tenant context", userContext.UserId); }

      // Validate context consistency
      await ValidateContextsAsync(userContext, tenantContext, permissionsContext, resourceContext, localizationContext);

      await _next(context);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error in context middleware");

      throw;
    }
  }

  /// <summary> Validates the consistency and state of all context services </summary>
  private async Task ValidateContextsAsync(IUserContext userContext, ITenantContext tenantContext, IPermissionsContext permissionsContext, IResourceContext resourceContext, ILocalizationContext localizationContext) {
    try {
      // Validate user context consistency
      if (userContext.IsAuthenticated) {
        if (userContext.UserId == null) { _logger.LogWarning("User is authenticated but UserId is null"); }

        if (string.IsNullOrEmpty(userContext.Email)) { _logger.LogWarning("Authenticated user {UserId} has no email", userContext.UserId); }
      }

      // Validate tenant context consistency
      if (tenantContext.TenantId.HasValue && string.IsNullOrEmpty(tenantContext.TenantName)) { _logger.LogWarning("Tenant {TenantId} has no name configured", tenantContext.TenantId); }

      // Validate permissions context consistency
      if (userContext.IsAuthenticated) {
        if (permissionsContext.UserId != userContext.UserId) { _logger.LogWarning("Permission context UserId {PermUserId} doesn't match UserContext UserId {UserUserId}", permissionsContext.UserId, userContext.UserId); }

        if (permissionsContext.TenantId != tenantContext.TenantId) {
          _logger.LogWarning("Permission context TenantId {PermTenantId} doesn't match TenantContext TenantId {TenantTenantId}", permissionsContext.TenantId, tenantContext.TenantId);
        }
      }

      // Validate localization context
      if (localizationContext.CurrentCulture == null) { _logger.LogWarning("Localization context has null CurrentCulture"); }

      if (localizationContext.CurrentTimeZone == null) { _logger.LogWarning("Localization context has null CurrentTimeZone"); }

      // Test basic functionality of each context (non-destructive operations only)
      _ = userContext.Claims.Count; // Ensure claims can be accessed
      _ = tenantContext.Settings.Count; // Ensure settings can be accessed
      _ = permissionsContext.IsAuthenticated; // Test permissions context
      _ = resourceContext.GetResourceIdentifier(); // Test resource context
      _ = localizationContext.GetCurrentLocalTime(); // Test localization context

      _logger.LogDebug("All context validations passed successfully");

      await Task.CompletedTask; // Make method async for future extensibility
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Context validation failed");
      // Don't throw here - allow request to continue with potentially degraded context
    }
  }
}

/// <summary> Extension methods for adding context middleware </summary>
public static class ContextMiddlewareExtensions {
  /// <summary> Adds context middleware to the application pipeline </summary>
  public static IApplicationBuilder UseContextMiddleware(this IApplicationBuilder builder) { return builder.UseMiddleware<ContextMiddleware>(); }
}
