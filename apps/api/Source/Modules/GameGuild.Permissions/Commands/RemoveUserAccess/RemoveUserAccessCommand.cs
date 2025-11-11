using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Application.Commands.RemoveUserAccess;

/// <summary>
///     Command to remove a user's access to a resource by revoking all their permissions.
///     Matches Game Guild's RemoveUserAccess mutation functionality.
/// </summary>
public sealed record RemoveUserAccessCommand : ICommand<PermissionUpdateResult>
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
    ///     Gets the ID of the user whose access is being removed.
    /// </summary>
    public required Guid TargetUserId { get; init; }

    /// <summary>
    ///     Gets the ID of the user removing the access.
    /// </summary>
    public required Guid RemovedByUserId { get; init; }

    /// <summary>
    ///     Gets the optional reason for removing access.
    /// </summary>
    public string? Reason { get; init; }
}
