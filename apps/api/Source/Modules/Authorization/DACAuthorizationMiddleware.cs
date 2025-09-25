using System.Security.Claims;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Logging;


namespace GameGuild.Authorization;

/// <summary> HotChocolate middleware for 3-layer DAC permission system Supports Tenant, Content-Type, and Resource level permissions </summary>
public class DACAuthorizationMiddleware(FieldDelegate next) {
  public async ValueTask InvokeAsync(IMiddlewareContext context) {
    var logger = context.Services.GetRequiredService<ILogger<DACAuthorizationMiddleware>>();
    var fieldName = context.Selection.Field.Name;
    var operation = context.Operation.Type.ToString();

    // Skip authorization for introspection queries
    if (IsIntrospectionQuery(context)) {
      logger.LogDebug("🔍 [GRAPHQL-INTROSPECTION] Operation: {Operation} | Field: {FieldName} | Skipping authorization", operation, fieldName);
      await next(context);
      return;
    }

    var permissionService = context.Services.GetRequiredService<IPermissionService>();

    // Extract user context from GraphQL context
    var userContext = await GetUserContextAsync(context);

    if (userContext == null) {
      logger.LogWarning("🚫 [GRAPHQL-AUTH] Operation: {Operation} | Field: {FieldName} | User not authenticated", operation, fieldName);
      throw new UnauthorizedAccessException("User not authenticated");
    }

    // Log GraphQL operation context
    logger.LogInformation("🎯 [GRAPHQL] Operation: {Operation} | Field: {FieldName} | User: {UserId} | Tenant: {TenantId}",
      operation, fieldName, userContext.UserId, userContext.TenantId);

    // Check for DAC authorization attributes on the resolver
    var dacAttribute = GetDACAttribute(context);

    if (dacAttribute != null) {
      var attributeType = dacAttribute.GetType().Name;
      logger.LogInformation("🔐 [GRAPHQL-PERMISSION] Operation: {Operation} | Field: {FieldName} | User: {UserId} | Checking: {AttributeType}",
        operation, fieldName, userContext.UserId, attributeType);

      var hasPermission = await CheckPermissionAsync(permissionService, userContext, dacAttribute, context);

      if (!hasPermission) {
        logger.LogWarning("🚫 [GRAPHQL-DENIED] Operation: {Operation} | Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | Missing: {AttributeType}",
          operation, fieldName, userContext.UserId, userContext.TenantId, attributeType);
        throw new UnauthorizedAccessException($"Insufficient permissions for {attributeType}");
      }
      else {
        logger.LogInformation("✅ [GRAPHQL-ALLOWED] Operation: {Operation} | Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | Permission: {AttributeType}",
          operation, fieldName, userContext.UserId, userContext.TenantId, attributeType);
      }
    }
    else {
      logger.LogInformation("ℹ️ [GRAPHQL-OPEN] Operation: {Operation} | Field: {FieldName} | User: {UserId} | No permission check required",
        operation, fieldName, userContext.UserId);
    }

    await next(context);
  }

  private static ValueTask<UsersContext?> GetUserContextAsync(IMiddlewareContext context) {
    var httpContext = context.Services.GetService<IHttpContextAccessor>()?.HttpContext;

    if (httpContext?.User?.Identity?.IsAuthenticated != true) return ValueTask.FromResult<UsersContext?>(null);

    var claims = httpContext.User.Claims;
    var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    var tenantIdClaim = claims.FirstOrDefault(c => c.Type == "tenantId")?.Value;

    if (userIdClaim == null || tenantIdClaim == null || !Guid.TryParse(userIdClaim, out var userId) || !Guid.TryParse(tenantIdClaim, out var tenantId)) return ValueTask.FromResult<UsersContext?>(null);

    return ValueTask.FromResult<UsersContext?>(new UsersContext { UserId = userId, TenantId = tenantId });
  }

  private static DACAuthorizationAttribute? GetDACAttribute(IMiddlewareContext context) {
    // Check the resolver method for DAC attributes
    var selection = context.Selection;
    var field = selection.Field;

    // Get the resolver method attributes
    var resolverMember = field.ResolverMember;

    if (resolverMember != null) return resolverMember.GetCustomAttributes(typeof(DACAuthorizationAttribute), true).FirstOrDefault() as DACAuthorizationAttribute;

    return null;
  }

  private static bool IsIntrospectionQuery(IMiddlewareContext context) {
    // Check if this is an introspection query (__schema, __type fields)
    var selection = context.Selection;
    var fieldName = selection.Field.Name;

    return fieldName.StartsWith("__");
  }

  private async ValueTask<bool> CheckPermissionAsync(IPermissionService permissionService, UsersContext userContext, DACAuthorizationAttribute dacAttribute, IMiddlewareContext context) {
    var logger = context.Services.GetRequiredService<ILogger<DACAuthorizationMiddleware>>();
    var fieldName = context.Selection.Field.Name;

    return dacAttribute switch {
      RequireTenantPermissionAttribute tenantAttr => await CheckTenantPermissionAsync(permissionService, userContext, tenantAttr, logger, fieldName),
      _ when IsContentTypePermissionAttribute(dacAttribute) => await CheckContentTypePermissionDynamicAsync(permissionService, userContext, dacAttribute, logger, fieldName),
      _ when IsResourcePermissionAttribute(dacAttribute) => await CheckResourcePermissionDynamicAsync(permissionService, userContext, dacAttribute, context, logger, fieldName),
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

  private static async ValueTask<bool> CheckTenantPermissionAsync(IPermissionService permissionService, UsersContext userContext, RequireTenantPermissionAttribute attribute, ILogger logger, string fieldName) {
    logger.LogInformation("🏢 [TENANT-PERMISSION] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | Required: {RequiredPermission}",
      fieldName, userContext.UserId, userContext.TenantId, attribute.RequiredPermission);

    var hasPermission = await permissionService.HasTenantPermissionAsync(userContext.UserId, userContext.TenantId, attribute.RequiredPermission);

    if (hasPermission) {
      logger.LogInformation("✅ [TENANT-ALLOWED] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | Permission: {RequiredPermission}",
        fieldName, userContext.UserId, userContext.TenantId, attribute.RequiredPermission);
    }
    else {
      logger.LogWarning("🚫 [TENANT-DENIED] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | Missing: {RequiredPermission}",
        fieldName, userContext.UserId, userContext.TenantId, attribute.RequiredPermission);
    }

    return hasPermission;
  }

  private static async ValueTask<bool> CheckContentTypePermissionDynamicAsync(IPermissionService permissionService, UsersContext userContext, DACAuthorizationAttribute attribute, ILogger logger, string fieldName) {
    var entityType = attribute.GetType().GetGenericArguments()[0];
    var requiredPermissionProperty = attribute.GetType().GetProperty("RequiredPermission");
    var requiredPermission = (PermissionType)requiredPermissionProperty!.GetValue(attribute)!;

    logger.LogInformation("📝 [CONTENT-TYPE-PERMISSION] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | EntityType: {EntityType} | Required: {RequiredPermission}",
      fieldName, userContext.UserId, userContext.TenantId, entityType.Name, requiredPermission);

    var hasPermission = await permissionService.HasContentTypePermissionAsync(userContext.UserId, userContext.TenantId, entityType.Name, requiredPermission);

    if (hasPermission) {
      logger.LogInformation("✅ [CONTENT-TYPE-ALLOWED] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | EntityType: {EntityType} | Permission: {RequiredPermission}",
        fieldName, userContext.UserId, userContext.TenantId, entityType.Name, requiredPermission);
    }
    else {
      logger.LogWarning("🚫 [CONTENT-TYPE-DENIED] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | EntityType: {EntityType} | Missing: {RequiredPermission}",
        fieldName, userContext.UserId, userContext.TenantId, entityType.Name, requiredPermission);
    }

    return hasPermission;
  }

  private async ValueTask<bool> CheckResourcePermissionDynamicAsync(IPermissionService permissionService, UsersContext userContext, DACAuthorizationAttribute attribute, IMiddlewareContext context, ILogger logger, string fieldName) {
    var resourceIdParameterProperty = attribute.GetType().GetProperty("ResourceIdParameterName");
    var resourceIdParameter = resourceIdParameterProperty?.GetValue(attribute) as string ?? "id";

    // Get the resource ID from the context parameters
    var resourceId = GetResourceIdFromContext(context, resourceIdParameter);

    if (resourceId == null) {
      logger.LogWarning("🚫 [RESOURCE-PERMISSION] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | ResourceId parameter '{ResourceIdParameter}' not found",
        fieldName, userContext.UserId, userContext.TenantId, resourceIdParameter);
      return false;
    }

    var genericArguments = attribute.GetType().GetGenericArguments();
    var entityType = genericArguments.Length > 1 ? genericArguments[1] : genericArguments[0];

    var requiredPermissionProperty = attribute.GetType().GetProperty("RequiredPermission");
    var requiredPermission = (PermissionType)requiredPermissionProperty!.GetValue(attribute)!;

    logger.LogInformation("📋 [RESOURCE-PERMISSION] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | ResourceId: {ResourceId} | EntityType: {EntityType} | Required: {RequiredPermission}",
      fieldName, userContext.UserId, userContext.TenantId, resourceId, entityType.Name, requiredPermission);

    // For now, fall back to content-type level permission since we can't easily call the generic method dynamically
    var hasPermission = await permissionService.HasContentTypePermissionAsync(userContext.UserId, userContext.TenantId, entityType.Name, requiredPermission);

    if (hasPermission) {
      logger.LogInformation("✅ [RESOURCE-ALLOWED] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | ResourceId: {ResourceId} | EntityType: {EntityType} | Permission: {RequiredPermission}",
        fieldName, userContext.UserId, userContext.TenantId, resourceId, entityType.Name, requiredPermission);
    }
    else {
      logger.LogWarning("🚫 [RESOURCE-DENIED] Field: {FieldName} | User: {UserId} | Tenant: {TenantId} | ResourceId: {ResourceId} | EntityType: {EntityType} | Missing: {RequiredPermission}",
        fieldName, userContext.UserId, userContext.TenantId, resourceId, entityType.Name, requiredPermission);
    }

    return hasPermission;
  }

  private static Guid? GetResourceIdFromContext(IMiddlewareContext context, string parameterName) {
    var argumentValue = context.ArgumentValue<object>(parameterName);

    if (argumentValue is Guid guidValue) return guidValue;

    if (argumentValue is string stringValue && Guid.TryParse(stringValue, out var parsedGuid)) return parsedGuid;

    return null;
  }
}
