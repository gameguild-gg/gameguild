using GameGuild.CQRS.Models;
using GameGuild.Entities;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Represents a user's direct permissions on a specific resource.
///     Tracks who granted the permissions, when they were granted, and when they expire.
/// </summary>
public class ResourceUserPermission : EntityBase<Guid>
{
    /// <summary>
    ///     Gets or sets the tenant ID this permission belongs to.
    /// </summary>
    public new required TenantId TenantId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who has these permissions.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    ///     Gets or sets the type of resource.
    ///     Example: "Project", "Post", "Document", "Dataset"
    /// </summary>
    public required string ResourceType { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the resource.
    /// </summary>
    public required string ResourceId { get; set; }

    /// <summary>
    ///     Gets or sets the array of permission strings granted to the user.
    ///     Example: ["read", "write", "delete", "admin"]
    /// </summary>
    public required string[] Permissions { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the permissions were granted.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the ID of the user who granted these permissions.
    /// </summary>
    public required Guid GrantedByUserId { get; set; }

    /// <summary>
    ///     Gets or sets the name of the user who granted these permissions (for display).
    /// </summary>
    public string? GrantedByUserName { get; set; }

    /// <summary>
    ///     Gets or sets the optional expiration date for these permissions.
    ///     If null, permissions don't expire.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the permissions were revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who revoked these permissions.
    /// </summary>
    public Guid? RevokedByUserId { get; set; }

    /// <summary>
    ///     Gets or sets the name of the user who revoked these permissions (for display).
    /// </summary>
    public string? RevokedByUserName { get; set; }

    /// <summary>
    ///     Gets or sets the reason for revoking these permissions.
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the user last accessed this resource.
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    ///     Gets whether these permissions are currently active.
    /// </summary>
    public bool IsActive => RevokedAt == null && !IsExpired;

    /// <summary>
    ///     Gets whether these permissions have expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    ///     Gets whether the user can access the resource.
    /// </summary>
    public bool CanAccess => IsActive;

    /// <summary>
    ///     Revokes the permissions for the user.
    /// </summary>
    /// <param name="revokedByUserId">The ID of the user revoking the permissions.</param>
    /// <param name="reason">Optional reason for revocation.</param>
    /// <returns>True if revoked successfully, false if already revoked.</returns>
    public bool Revoke(Guid revokedByUserId, string? reason = null)
    {
        if (RevokedAt.HasValue) { return false; }

        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason;

        return true;
    }

    /// <summary>
    ///     Updates the permissions granted to the user.
    /// </summary>
    /// <param name="newPermissions">The new set of permissions.</param>
    /// <param name="updatedByUserId">The ID of the user making the update.</param>
    /// <returns>True if updated successfully.</returns>
    public bool UpdatePermissions(string[] newPermissions, Guid updatedByUserId)
    {
        if (!IsActive) { return false; }

        Permissions = newPermissions;

        return true;
    }

    /// <summary>
    ///     Records that the user accessed the resource.
    /// </summary>
    public void RecordAccess() { LastAccessedAt = DateTime.UtcNow; }

    /// <summary>
    ///     Checks if the user has a specific permission on this resource.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the user has the permission and it's active.</returns>
    public bool HasPermission(string permission)
    {
        return IsActive && Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Checks if the user has any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has at least one of the permissions and it's active.</returns>
    public bool HasAnyPermission(string[] permissions)
    {
        return IsActive && Permissions.Any(p => permissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Checks if the user has all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has all of the permissions and it's active.</returns>
    public bool HasAllPermissions(string[] permissions)
    {
        return IsActive && permissions.All(p => Permissions.Contains(p, StringComparer.OrdinalIgnoreCase));
    }
}

/// <summary>
///     Represents the status of a resource invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    ///     Invitation is pending a response.
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     Invitation has been accepted by the invitee.
    /// </summary>
    Accepted = 1,

    /// <summary>
    ///     Invitation has been declined by the invitee.
    /// </summary>
    Declined = 2,

    /// <summary>
    ///     Invitation has been revoked by the inviter or administrator.
    /// </summary>
    Revoked = 3,

    /// <summary>
    ///     Invitation has expired before being accepted or declined.
    /// </summary>
    Expired = 4
}

/// <summary>
///     Represents an invitation to share a resource with a user via email.
///     Tracks invitation status, expiration, and response.
/// </summary>
public class ResourceInvitation : EntityBase<Guid>
{
    /// <summary>
    ///     Gets or sets the tenant ID this invitation belongs to.
    /// </summary>
    public new required TenantId TenantId { get; set; }

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
    public required string[] Permissions { get; set; }

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
    ///     Gets or sets the ID of the user who accepted the invitation.
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
    public bool IsPending => Status == InvitationStatus.Pending && !IsExpired;

    /// <summary>
    ///     Gets whether the invitation has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    ///     Gets whether the invitation can be accepted.
    /// </summary>
    public bool CanBeAccepted => Status == InvitationStatus.Pending && !IsExpired;

    /// <summary>
    ///     Gets whether the invitation can be revoked.
    /// </summary>
    public bool CanBeRevoked => Status == InvitationStatus.Pending;

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
    ///     Revokes the invitation.
    /// </summary>
    /// <param name="revokedByUserId">The ID of the user revoking the invitation.</param>
    /// <returns>True if revoked successfully, false if already processed.</returns>
    public bool Revoke(Guid revokedByUserId)
    {
        if (!CanBeRevoked) { return false; }

        Status = InvitationStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;

        return true;
    }
}
