using GameGuild.CQRS;

namespace GameGuild.Permissions.Application.Queries.GetTenantPermissions;

/// <summary>
///     Query to get all tenant-level permissions for a user.
/// </summary>
public sealed record GetTenantPermissionsQuery : IQuery<GetTenantPermissionsResponse>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to check (defaults to current user if not specified).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Gets whether to include effective permissions (from roles, groups, etc).
    /// </summary>
    public bool IncludeEffective { get; init; } = true;
}

/// <summary>
///     Response containing tenant permissions for a user.
/// </summary>
public sealed record GetTenantPermissionsResponse
{
    /// <summary>
    ///     Gets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    ///     Gets the list of permissions.
    /// </summary>
    public required List<string> Permissions { get; init; }

    /// <summary>
    ///     Gets whether the user is a tenant admin.
    /// </summary>
    public bool IsTenantAdmin { get; init; }

    /// <summary>
    ///     Gets whether the user is a system admin.
    /// </summary>
    public bool IsSystemAdmin { get; init; }
}
