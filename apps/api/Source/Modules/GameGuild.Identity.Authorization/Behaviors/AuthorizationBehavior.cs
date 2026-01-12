using System.Reflection;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Pipeline behavior that performs authorization checks on CQRS commands and queries
///     Uses custom attributes on request classes to determine authorization requirements
/// </summary>
/// <typeparam name="TRequest">The request type (command or query)</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class AuthorizationBehavior<TRequest, TResponse>(
    IActorContextAccessor actorContextAccessor,
    IPermissionService permissionService
) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Get attributes named AuthorizeRequestAttribute from request type
        var allAttrs = request.GetType()
            .GetCustomAttributes(true)
            .Where(a => string.Equals(
                a.GetType().Name,
                "AuthorizeRequestAttribute",
                StringComparison.Ordinal))
            .ToList();

        if (!allAttrs.Any())
        {
            return await next().ConfigureAwait(false);
        }

        // Check if user is authenticated
        if (!Actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // System admin bypass
        if (Actor.IsSystemAdmin)
        {
            return await next().ConfigureAwait(false);
        }

        // Evaluate each attribute
        foreach (var rawAttr in allAttrs)
        {
            var attrType = rawAttr.GetType();

            // Check for explicit RequireSystemAdmin/RequireTenantAdmin flags
            var requireSystemAdminProp = attrType.GetProperty("RequireSystemAdmin");

            if (requireSystemAdminProp != null)
            {
                var requireSys = requireSystemAdminProp.GetValue(rawAttr) as bool? ?? false;

                if (requireSys)
                {
                    if (!Actor.IsSystemAdmin)
                        throw new UnauthorizedAccessException("System admin required");

                    continue;
                }
            }

            var requireTenantAdminProp = attrType.GetProperty("RequireTenantAdmin");

            if (requireTenantAdminProp != null)
            {
                var requireTenant = requireTenantAdminProp.GetValue(rawAttr) as bool? ?? false;

                if (requireTenant)
                {
                    if (!Actor.IsTenantAdmin)
                        throw new UnauthorizedAccessException("Tenant admin required");

                    continue;
                }
            }

            // Extract permission and resource info
            var permissionProp = attrType.GetProperty("Permission");
            var resourceTypeProp = attrType.GetProperty("ResourceType");
            var resourceIdPropertyProp = attrType.GetProperty("ResourceIdProperty");

            var permission = permissionProp?.GetValue(rawAttr) as string;
            var resourceType = resourceTypeProp?.GetValue(rawAttr) as string;
            var resourceIdProperty = resourceIdPropertyProp?.GetValue(rawAttr) as string ?? "ResourceId";

            if (string.IsNullOrEmpty(permission))
            {
                continue;
            }

            bool hasPerm;

            if (!string.IsNullOrEmpty(resourceType))
            {
                // Resource-level check: extract resource id from request
                var resourceId = GetResourceIdFromRequest(request, resourceIdProperty);

                if (resourceId == Guid.Empty)
                {
                    hasPerm = false;
                }
                else
                {
                    var userId = Actor.SubjectIdAsGuid ?? throw new UnauthorizedAccessException("User not authenticated");
                    var tenantId = Actor.TenantId ?? throw new UnauthorizedAccessException("Tenant context required");
                    // Compose resource-specific permission name (same pattern as PermissionsContext)
                    var resourcePermission = $"{resourceType}.{resourceId}.{permission}";
                    hasPerm = await permissionService
                        .HasTenantPermissionAsync(userId, tenantId, resourcePermission, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                // Tenant-level permission - use ActorContext.HasPermission for simple check
                hasPerm = Actor.HasPermission(permission);
            }

            if (!hasPerm)
            {
                throw new UnauthorizedAccessException(
                    $"User does not have required permission: {permission}");
            }
        }

        return await next().ConfigureAwait(false);
    }

    private static Guid GetResourceIdFromRequest(object request, string propertyName)
    {
        var property = request.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null) return Guid.Empty;

        var value = property.GetValue(request);

        if (value is Guid g) return g;
        if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;

        return Guid.Empty;
    }
}

/// <summary>
///     Attribute to mark CQRS requests (commands/queries) with authorization requirements
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AuthorizeRequestAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;

    public PermissionLayer Layer { get; set; } = PermissionLayer.Tenant;

    public string? ResourceType { get; set; }

    public string? ResourceIdProperty { get; set; }

    public bool RequireSystemAdmin { get; set; }

    public bool RequireTenantAdmin { get; set; }
}
