using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Application.Commands.ShareResource;

/// <summary>
///     Command to share a resource with one or more users by granting them permissions.
///     Matches Game Guild's ShareResource mutation functionality.
/// </summary>
public sealed record ShareResourceCommand : ICommand<ShareResult>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the type of resource being shared (e.g., "Project", "Document", "Task").
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     Gets the ID of the resource being shared.
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    ///     Gets the IDs of users to share the resource with.
    /// </summary>
    public required Guid[ ] UserIds { get; init; }

    /// <summary>
    ///     Gets the email addresses of users to share with (alternative to UserIds).
    /// </summary>
    public string[ ]? UserEmails { get; init; }

    /// <summary>
    ///     Gets the permissions to grant to the users.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user who is sharing the resource.
    /// </summary>
    public required Guid GrantedByUserId { get; init; }

    /// <summary>
    ///     Gets the optional expiration date for the granted permissions.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Gets the optional message to include with the share.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Gets whether the users must accept the share before gaining access.
    /// </summary>
    public bool RequireAcceptance { get; init; } = true;

    /// <summary>
    ///     Gets whether to notify users about the share via email/notification.
    /// </summary>
    public bool NotifyUsers { get; init; } = true;
}
