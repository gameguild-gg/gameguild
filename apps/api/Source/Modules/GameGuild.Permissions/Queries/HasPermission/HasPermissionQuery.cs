using GameGuild.CQRS;

namespace GameGuild.Permissions.Application.Queries.HasPermission;

/// <summary>
///     Query to check if a user has a specific permission on a resource.
///     Matches Game Guild's HasPermission GraphQL query.
/// </summary>
public sealed record HasPermissionQuery : IQuery<HasPermissionResponse>
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
    ///     Gets the permission to check.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    ///     Gets the user ID to check (defaults to current user if not specified).
    /// </summary>
    public Guid? UserId { get; init; }
}

/// <summary>
///     Response indicating whether the user has the requested permission.
/// </summary>
public sealed record HasPermissionResponse
{
    /// <summary>
    ///     Gets whether the permission is granted.
    /// </summary>
    public required bool HasPermission { get; init; }

    /// <summary>
    ///     Gets the user ID that was checked.
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
    ///     Gets the permission that was checked.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    ///     Gets the reason if permission was denied.
    /// </summary>
    public string? DenialReason { get; init; }
}
