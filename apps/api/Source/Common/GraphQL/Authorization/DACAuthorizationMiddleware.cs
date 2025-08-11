using System.Security.Claims;
using GameGuild.Modules.Permissions;
using HotChocolate.Resolvers;


namespace GameGuild.Common.Authorization;

/// <summary>
/// HotChocolate middleware for 3-layer DAC permission system
/// Supports Tenant, Content-Type, and Resource level permissions
/// </summary>
public class DACAuthorizationMiddleware(FieldDelegate next) {
  public async ValueTask InvokeAsync(IMiddlewareContext context) {
                                                                                                                                                                                                                                                                            // Skip authorization for introspection queries
    if (IsIntrospectionQuery(context)) {
      await next(context);
      return;
    }

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

    await next(context);
  }

  private static ValueTask<UsersContext?> GetUserContextAsync(IMiddlewareContext context) {
    var httpContext = context.Services.GetService<IHttpContextAccessor>()?.HttpContext;

    if (httpContext?.User?.Identity?.IsAuthenticated != true) return ValueTask.FromResult<UsersContext?>(null);

    var claims = httpContext.User.Claims;
    var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    var tenantIdClaim = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

    if (userIdClaim == null ||
        tenantIdClaim == null ||
        !Guid.TryParse(userIdClaim, out var userId) ||
        !Guid.TryParse(tenantIdClaim, out var tenantId))
      return ValueTask.FromResult<UsersContext?>(null);

    return ValueTask.FromResult<UsersContext?>(new UsersContext { UserId = userId, TenantId = tenantId });
  }

  private static DACAuthorizationAttribute? GetDACAttribute(IMiddlewareContext context) {
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

  private static bool IsIntrospectionQuery(IMiddlewareContext context) {
    // Check if this is an introspection query (__schema, __type fields)
    var selection = context.Selection;
    var fieldName = selection.Field.Name;
    
    return fieldName.StartsWith("__");
  }

  private async ValueTask<bool> CheckPermissionAsync(
    IPermissionService permissionService, UsersContext userContext,
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

  private static async ValueTask<bool> CheckTenantPermissionAsync(
    IPermissionService permissionService,
    UsersContext userContext, RequireTenantPermissionAttribute attribute
  ) {
    return await permissionService.HasTenantPermissionAsync(
             userContext.UserId,
             userContext.TenantId,
             attribute.RequiredPermission
           );
  }

  private static async ValueTask<bool> CheckContentTypePermissionDynamicAsync(
    IPermissionService permissionService,
    UsersContext userContext, DACAuthorizationAttribute attribute
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
    UsersContext userContext, DACAuthorizationAttribute attribute, IMiddlewareContext context
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

  private static Guid? GetResourceIdFromContext(IMiddlewareContext context, string parameterName) {
    var argumentValue = context.ArgumentValue<object>(parameterName);

    if (argumentValue is Guid guidValue) return guidValue;

    if (argumentValue is string stringValue && Guid.TryParse(stringValue, out var parsedGuid)) return parsedGuid;

    return null;
  }
}
