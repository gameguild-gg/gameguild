using GameGuild.CQRS;

namespace GameGuild.Permissions.Application.Queries.GetEffectivePermissions;

/// <summary>
///     Query to get all effective permissions for a user on a specific resource.
///     Matches Game Guild's GetEffectivePermissions GraphQL query.
/// </summary>
public sealed record GetEffectivePermissionsQuery : IQuery<EffectivePermissionsResponse>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the type of resource.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     Gets the ID of the resource.
    /// </summary>
    public required Guid ResourceId { get; init; }

    /// <summary>
    ///     Gets the user ID to check permissions for (defaults to current user if not specified).
    /// </summary>
    public Guid? UserId { get; init; }
}

/// <summary>
///     Response containing all effective permissions for a user on a resource.
/// </summary>
public sealed record EffectivePermissionsResponse
{
    /// <summary>
    ///     Gets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the resource type.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     Gets the resource ID.
    /// </summary>
    public required Guid ResourceId { get; init; }

    /// <summary>
    ///     Gets the list of effective permissions.
    /// </summary>
    public required List<EffectivePermissionDto> Permissions { get; init; }

    /// <summary>
    ///     Gets whether the user is the owner of the resource.
    /// </summary>
    public bool IsOwner { get; init; }

    /// <summary>
    ///     Gets whether the user has full access to the resource.
    /// </summary>
    public bool HasFullAccess { get; init; }
}

/// <summary>
///     Represents a single effective permission with its source.
/// </summary>
public sealed record EffectivePermissionDto
{
    /// <summary>
    ///     Gets the permission name.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    ///     Gets the source of the permission (e.g., "Direct", "Role", "Group", "Tenant").
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    ///     Gets the permission layer (Tenant, ContentType, Resource).
    /// </summary>
    public required string Layer { get; init; }

    /// <summary>
    ///     Gets whether the permission is granted or denied.
    /// </summary>
    public bool IsGranted { get; init; } = true;

    /// <summary>
    ///     Gets the expiration date of the permission if any.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
