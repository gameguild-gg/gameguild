using GameGuild.Permissions.Domain.Enums;

namespace GameGuild.Permissions.Domain.Entities;

/// <summary>
///     Represents an invitation to share a resource with a user via email.
///     Tracks invitation status, expiration, and response.
/// </summary>
public class ResourceInvitation : EntityBase<Guid>
{
    /// <summary>
    ///     Gets or sets the tenant ID this invitation belongs to.
    /// </summary>
    public required TenantId TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the email address of the invitee.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    ///     Gets or sets the type of resource being shared.
    /// </summary>
    public required string ResourceType { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the resource being shared.
    /// </summary>
    public required string ResourceId { get; set; }

    /// <summary>
    ///     Gets or sets the array of permission strings being granted.
    ///     Example: ["read", "write", "admin"]
    /// </summary>
    public required string[ ] Permissions { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who sent the invitation.
    /// </summary>
    public required Guid InvitedByUserId { get; set; }

    /// <summary>
    ///     Gets or sets the name of the user who sent the invitation (for display).
    /// </summary>
    public string? InvitedByUserName { get; set; }

    /// <summary>
    ///     Gets or sets an optional message to include with the invitation.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the invitation was sent.
    /// </summary>
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the date and time the invitation expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the invitation.
    /// </summary>
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>
    ///     Gets or sets the date and time the invitation was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who accepted the invitation (may differ from email for existing users).
    /// </summary>
    public Guid? AcceptedByUserId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the invitation was declined.
    /// </summary>
    public DateTime? DeclinedAt { get; set; }

    /// <summary>
    ///     Gets or sets the reason for declining the invitation.
    /// </summary>
    public string? DeclineReason { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the invitation was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who revoked the invitation.
    /// </summary>
    public Guid? RevokedByUserId { get; set; }

    /// <summary>
    ///     Gets whether the invitation is still pending.
    /// </summary>
    public bool IsPending { get => Status == InvitationStatus.Pending && !IsExpired; }

    /// <summary>
    ///     Gets whether the invitation has expired.
    /// </summary>
    public bool IsExpired { get => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow; }

    /// <summary>
    ///     Gets whether the invitation can be accepted.
    /// </summary>
    public bool CanBeAccepted { get => Status == InvitationStatus.Pending && !IsExpired; }

    /// <summary>
    ///     Gets whether the invitation can be revoked.
    /// </summary>
    public bool CanBeRevoked { get => Status == InvitationStatus.Pending; }

    /// <summary>
    ///     Accepts the invitation for the specified user.
    /// </summary>
    /// <param name="acceptingUserId">The ID of the user accepting the invitation.</param>
    /// <returns>True if accepted successfully, false if already processed or expired.</returns>
    public bool Accept(Guid acceptingUserId)
    {
        if (!CanBeAccepted) { return false; }

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        AcceptedByUserId = acceptingUserId;

        return true;
    }

    /// <summary>
    ///     Declines the invitation with an optional reason.
    /// </summary>
    /// <param name="reason">The reason for declining.</param>
    /// <returns>True if declined successfully, false if already processed or expired.</returns>
    public bool Decline(string? reason = null)
    {
        if (Status != InvitationStatus.Pending) { return false; }

        Status = InvitationStatus.Declined;
        DeclinedAt = DateTime.UtcNow;
        DeclineReason = reason;

        return true;
    }

    /// <summary>
    ///     Revokes the invitation before it's accepted or declined.
    /// </summary>
    /// <param name="revokedByUserId">The ID of the user revoking the invitation.</param>
    /// <returns>True if revoked successfully, false if already accepted/declined.</returns>
    public bool Revoke(Guid revokedByUserId)
    {
        if (!CanBeRevoked) { return false; }

        Status = InvitationStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;

        return true;
    }

    /// <summary>
    ///     Checks if the invitation has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the permission is included in the invitation.</returns>
    public bool HasPermission(string permission) { return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase); }
}
