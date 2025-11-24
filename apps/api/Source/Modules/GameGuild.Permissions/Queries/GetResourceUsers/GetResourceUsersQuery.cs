using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Application.Queries.GetResourceUsers;

/// <summary>
///     Query to get all users who have access to a specific resource.
/// </summary>
public sealed record GetResourceUsersQuery : IQuery<GetResourceUsersResponse>
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
    public required string ResourceId { get; init; }

    /// <summary>
    ///     Gets whether to include inherited permissions from groups/roles.
    /// </summary>
    public bool IncludeInherited { get; init; } = true;

    /// <summary>
    ///     Gets whether to include expired permissions in the results.
    /// </summary>
    public bool IncludeExpired { get; init; } = false;
}

/// <summary>
///     Response containing all users with access to a resource.
/// </summary>
public sealed record GetResourceUsersResponse
{
    /// <summary>
    ///     Gets the resource type.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     Gets the resource ID.
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    ///     Gets the list of users with access.
    /// </summary>
    public required List<ResourceUser> Users { get; init; }

    /// <summary>
    ///     Gets the total count of users.
    /// </summary>
    public int TotalCount { get => Users.Count; }

    /// <summary>
    ///     Gets the count of users with owner access.
    /// </summary>
    public int OwnerCount { get => Users.Count(u => u.IsOwner); }
}
