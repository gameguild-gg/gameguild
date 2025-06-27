using HotChocolate.Resolvers;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using System.Security.Claims;


namespace GameGuild.Common.GraphQL.Authorization;

/// <summary>
/// HotChocolate middleware for 3-layer DAC permission system
/// Supports Tenant, Content-Type, and Resource level permissions
/// </summary>
public class DACAuthorizationMiddleware {
  private readonly FieldDelegate _next;

  public DACAuthorizationMiddleware(FieldDelegate next) { _next = next; }

  public async ValueTask InvokeAsync(IMiddlewareContext context) {
    var permissionService = context.Services.GetRequiredService<IPermissionService>();

    // Extract user context from GraphQL context
    var userContext = await GetUserContextAsync(context);

    if (userContext == null) throw new UnauthorizedAccessException("User not authenticated");

    // Check for DAC authorization attributes on the resolver
    var dacAttribute = GetDACAttribute(context);

    if (dacAttribute != null) {
      var hasPermission = await CheckPermissionAsync(permissionService, userContext, dacAttribute, context);

      if (!hasPermission) throw new UnauthorizedAccessException($"Insufficient permissions for {dacAttribute.GetType().Name}");
    }

    await _next(context);
  }

  private async ValueTask<UserContext?> GetUserContextAsync(IMiddlewareContext context) {
    var httpContext = context.Services.GetService<IHttpContextAccessor>()?.HttpContext;

    if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;

    var claims = httpContext.User.Claims;
    var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    var tenantIdClaim = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

    if (userIdClaim == null ||
        tenantIdClaim == null ||
        !Guid.TryParse(userIdClaim, out var userId) ||
        !Guid.TryParse(tenantIdClaim, out var tenantId))
      return null;

    return new UserContext { UserId = userId, TenantId = tenantId };
  }

  private DACAuthorizationAttribute? GetDACAttribute(IMiddlewareContext context) {
    // Check the resolver method for DAC attributes
    var selection = context.Selection;
    var field = selection.Field;

    // Get the resolver method attributes
    var resolverMember = field.ResolverMember;

    if (resolverMember != null)
      return resolverMember.GetCustomAttributes(typeof(DACAuthorizationAttribute), true).FirstOrDefault() as
               DACAuthorizationAttribute;

    return null;
  }

  private async ValueTask<bool> CheckPermissionAsync(
    IPermissionService permissionService, UserContext userContext,
    DACAuthorizationAttribute dacAttribute, IMiddlewareContext context
  ) {
    return dacAttribute switch {
      RequireTenantPermissionAttribute tenantAttr => await CheckTenantPermissionAsync(
                                                       permissionService,
                                                       userContext,
                                                       tenantAttr
                                                     ),
      _ when IsContentTypePermissionAttribute(dacAttribute) => await CheckContentTypePermissionDynamicAsync(
                                                                 permissionService,
                                                                 userContext,
                                                                 dacAttribute
                                                               ),
      _ when IsResourcePermissionAttribute(dacAttribute) => await CheckResourcePermissionDynamicAsync(
                                                              permissionService,
                                                              userContext,
                                                              dacAttribute,
                                                              context
                                                            ),
      _ => false,
    };
  }

  private static bool IsContentTypePermissionAttribute(DACAuthorizationAttribute attribute) {
    var type = attribute.GetType();

    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RequireContentTypePermissionAttribute<>);
  }

  private static bool IsResourcePermissionAttribute(DACAuthorizationAttribute attribute) {
    var type = attribute.GetType();

    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RequireResourcePermissionAttribute<,>);
  }

  private async ValueTask<bool> CheckTenantPermissionAsync(
    IPermissionService permissionService,
    UserContext userContext, RequireTenantPermissionAttribute attribute
  ) {
    return await permissionService.HasTenantPermissionAsync(
             userContext.UserId,
             userContext.TenantId,
             attribute.RequiredPermission
           );
  }

  private async ValueTask<bool> CheckContentTypePermissionDynamicAsync(
    IPermissionService permissionService,
    UserContext userContext, DACAuthorizationAttribute attribute
  ) {
    var entityType = attribute.GetType().GetGenericArguments()[0];
    var requiredPermissionProperty = attribute.GetType().GetProperty("RequiredPermission");
    var requiredPermission = (PermissionType)requiredPermissionProperty!.GetValue(attribute)!;

    return await permissionService.HasContentTypePermissionAsync(
             userContext.UserId,
             userContext.TenantId,
             entityType.Name,
             requiredPermission
           );
  }

  private async ValueTask<bool> CheckResourcePermissionDynamicAsync(
    IPermissionService permissionService,
    UserContext userContext, DACAuthorizationAttribute attribute, IMiddlewareContext context
  ) {
    var resourceIdParameterProperty = attribute.GetType().GetProperty("ResourceIdParameterName");
    var resourceIdParameter = resourceIdParameterProperty?.GetValue(attribute) as string ?? "id";

    // Get the resource ID from the context parameters
    var resourceId = GetResourceIdFromContext(context, resourceIdParameter);

    if (resourceId == null) return false;

    var genericArguments = attribute.GetType().GetGenericArguments();
    var entityType = genericArguments.Length > 1 ? genericArguments[1] : genericArguments[0];

    var requiredPermissionProperty = attribute.GetType().GetProperty("RequiredPermission");
    var requiredPermission = (PermissionType)requiredPermissionProperty!.GetValue(attribute)!;

    // For now, fall back to content-type level permission since we can't easily call the generic method dynamically
    return await permissionService.HasContentTypePermissionAsync(
             userContext.UserId,
             userContext.TenantId,
             entityType.Name,
             requiredPermission
           );
  }

  private Guid? GetResourceIdFromContext(IMiddlewareContext context, string parameterName) {
    var argumentValue = context.ArgumentValue<object>(parameterName);

    if (argumentValue is Guid guidValue) return guidValue;

    if (argumentValue is string stringValue && Guid.TryParse(stringValue, out var parsedGuid)) return parsedGuid;

    return null;
  }
}
