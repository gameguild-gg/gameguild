using System.Reflection;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Pipeline behavior that performs authorization checks on CQRS commands and queries
///     using the new <see cref="ActorContext"/> model.
/// </summary>
/// <remarks>
///     <para>
///         This behavior uses <see cref="IActorContextAccessor"/> to get the pre-evaluated
///         security context for the current request. For resource-level permissions that
///         require database lookups, it delegates to <see cref="IPermissionService"/>.
///     </para>
///     <para>
///         Use <see cref="AuthorizeRequestAttribute"/> on command/query classes to define
///         authorization requirements.
///     </para>
/// </remarks>
/// <typeparam name="TRequest">The request type (command or query)</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class ActorAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequestBase
{
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IPermissionService _permissionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorAuthorizationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    public ActorAuthorizationBehavior(
        IActorContextAccessor actorContextAccessor,
        IPermissionService permissionService)
    {
        _actorContextAccessor = actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    }

    private ActorContext Actor => _actorContextAccessor.ActorContext;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
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

        // Check if actor is authenticated
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
            await EvaluateAuthorizationAttributeAsync(request, rawAttr, cancellationToken)
                .ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }

    private async Task EvaluateAuthorizationAttributeAsync(
        TRequest request,
        object rawAttr,
        CancellationToken cancellationToken)
    {
        var attrType = rawAttr.GetType();

        // Check for explicit RequireSystemAdmin flag
        var requireSystemAdminProp = attrType.GetProperty("RequireSystemAdmin");
        if (requireSystemAdminProp != null)
        {
            var requireSys = requireSystemAdminProp.GetValue(rawAttr) as bool? ?? false;
            if (requireSys)
            {
                if (!Actor.IsSystemAdmin)
                    throw new UnauthorizedAccessException("System admin required");
                return;
            }
        }

        // Check for explicit RequireTenantAdmin flag
        var requireTenantAdminProp = attrType.GetProperty("RequireTenantAdmin");
        if (requireTenantAdminProp != null)
        {
            var requireTenant = requireTenantAdminProp.GetValue(rawAttr) as bool? ?? false;
            if (requireTenant)
            {
                if (!Actor.IsTenantAdmin)
                    throw new UnauthorizedAccessException("Tenant admin required");
                return;
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
            return;
        }

        bool hasPerm;

        if (!string.IsNullOrEmpty(resourceType))
        {
            // Resource-level check: requires database lookup
            hasPerm = await CheckResourcePermissionAsync(
                request, resourceType, resourceIdProperty, permission, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            // Tenant-level permission: use pre-evaluated permissions from ActorContext
            hasPerm = Actor.HasPermission(permission);
        }

        if (!hasPerm)
        {
            throw new UnauthorizedAccessException(
                $"User does not have required permission: {permission}");
        }
    }

    private async Task<bool> CheckResourcePermissionAsync(
        TRequest request,
        string resourceType,
        string resourceIdProperty,
        string permission,
        CancellationToken cancellationToken)
    {
        var resourceId = GetResourceIdFromRequest(request, resourceIdProperty);

        if (resourceId == Guid.Empty)
            return false;

        var userId = Actor.SubjectIdAsGuid;
        var tenantId = Actor.TenantId;

        if (!userId.HasValue || !tenantId.HasValue)
            return false;

        // Check resource-specific permission via permission service
        var resourcePermission = $"{resourceType}.{resourceId}.{permission}";

        return await _permissionService.HasTenantPermissionAsync(
            userId.Value,
            tenantId.Value,
            resourcePermission,
            cancellationToken).ConfigureAwait(false);
    }

    private static Guid GetResourceIdFromRequest(object request, string propertyName)
    {
        var property = request.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null) return Guid.Empty;

        var value = property.GetValue(request);

        return value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => Guid.Empty
        };
    }
}
