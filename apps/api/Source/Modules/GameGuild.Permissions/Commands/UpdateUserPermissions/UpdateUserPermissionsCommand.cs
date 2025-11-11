using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Application.Commands.UpdateUserPermissions;

/// <summary>
///     Command to update a specific user's permissions on a resource.
///     Matches Game Guild's UpdateUserPermissions mutation functionality.
/// </summary>
public sealed record UpdateUserPermissionsCommand : ICommand<PermissionUpdateResult>
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
    ///     Gets the ID of the user whose permissions are being updated.
    /// </summary>
    public required Guid TargetUserId { get; init; }

    /// <summary>
    ///     Gets the new set of permissions to grant to the user.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user making the update.
    /// </summary>
    public required Guid UpdatedByUserId { get; init; }

    /// <summary>
    ///     Gets the optional expiration date for the permissions.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}
