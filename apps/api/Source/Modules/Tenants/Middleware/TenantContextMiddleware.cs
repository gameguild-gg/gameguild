namespace GameGuild.Modules.Tenants;

/// <summary>
/// Middleware to handle tenant context in requests
/// </summary>
public class TenantContextMiddleware(RequestDelegate next) {
  private const string TenantHeader = "X-Tenant-ID";

  public async Task InvokeAsync(HttpContext context, ITenantContextService tenantContextService) {
    // Extract tenant ID from header
    string? tenantHeader = context.Request.Headers[TenantHeader];

    // Get authenticated user (if any)
    var user = context.User.Identity?.IsAuthenticated == true ? context.User : null;

    // Get current tenant
    var tenant = await tenantContextService.GetCurrentTenantAsync(user, tenantHeader);

    // Check tenant access if tenant was specified
    if (tenant != null && user != null) {
      // Get tenant ID
      var tenantId = tenant.Id;

      // Check if user can access this tenant
      var canAccess = await tenantContextService.CanAccessTenantAsync(user, tenantId);

      if (!canAccess) {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "User does not have access to this tenant" });

        return;
      }

      // If access is allowed, set tenant information in HttpContext.Items for use in the request pipeline
      context.Items["CurrentTenant"] = tenant;
      context.Items["TenantId"] = tenantId;
    }

    await next(context);
  }
}
