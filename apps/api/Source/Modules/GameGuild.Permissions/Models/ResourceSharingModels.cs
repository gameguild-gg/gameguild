namespace GameGuild.Permissions.Domain.Models;

/// <summary>
///     Request to share a resource with one or more users.
/// </summary>
public record ShareResourceRequest
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the type of resource being shared.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     Gets the ID of the resource being shared.
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    ///     Gets the IDs of users to share with.
    /// </summary>
    public required Guid[ ] UserIds { get; init; }

    /// <summary>
    ///     Gets the permissions to grant.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user granting access.
    /// </summary>
    public required Guid GrantedByUserId { get; init; }

    /// <summary>
    ///     Gets the optional expiration date for the permissions.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
///     Result of a share resource operation.
/// </summary>
public record ShareResult
{
    /// <summary>
    ///     Gets whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the results for each user.
    /// </summary>
    public required UserShareResult[ ] UserResults { get; init; }

    /// <summary>
    ///     Gets the number of users successfully shared with.
    /// </summary>
    public int SuccessCount { get => UserResults.Count(r => r.Success); }

    /// <summary>
    ///     Gets the number of users that failed.
    /// </summary>
    public int FailureCount { get => UserResults.Count(r => !r.Success); }
}

/// <summary>
///     Result of sharing with a single user.
/// </summary>
public record UserShareResult
{
    /// <summary>
    ///     Gets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets whether the share succeeded for this user.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Gets the error message if it failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the ID of the created permission record.
    /// </summary>
    public Guid? PermissionId { get; init; }
}

/// <summary>
///     Request to invite a user to a resource via email.
/// </summary>
public record InviteUserRequest
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the email address of the invitee.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    ///     Gets the type of resource being shared.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    ///     Gets the ID of the resource being shared.
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    ///     Gets the permissions to grant.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user sending the invitation.
    /// </summary>
    public required Guid InvitedByUserId { get; init; }

    /// <summary>
    ///     Gets the optional message to include with the invitation.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Gets the optional expiration date for the invitation.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
///     Result of an invite user operation.
/// </summary>
public record InvitationResult
{
    /// <summary>
    ///     Gets whether the invitation was sent successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the ID of the created invitation.
    /// </summary>
    public Guid? InvitationId { get; init; }

    /// <summary>
    ///     Gets whether the user already exists in the system.
    /// </summary>
    public bool UserExists { get; init; }

    /// <summary>
    ///     Gets whether an email was sent.
    /// </summary>
    public bool EmailSent { get; init; }

    /// <summary>
    ///     Gets the user ID if the user already exists.
    /// </summary>
    public Guid? ExistingUserId { get; init; }
}

/// <summary>
///     Result of accepting an invitation.
/// </summary>
public record InvitationAcceptResult
{
    /// <summary>
    ///     Gets whether the invitation was accepted successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the ID of the created permission record.
    /// </summary>
    public Guid? PermissionId { get; init; }

    /// <summary>
    ///     Gets the invitation ID.
    /// </summary>
    public Guid? InvitationId { get; init; }
}

/// <summary>
///     Result of declining an invitation.
/// </summary>
public record InvitationDeclineResult
{
    /// <summary>
    ///     Gets whether the invitation was declined successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the invitation ID.
    /// </summary>
    public Guid? InvitationId { get; init; }
}

/// <summary>
///     Result of updating user permissions on a resource.
/// </summary>
public record PermissionUpdateResult
{
    /// <summary>
    ///     Gets whether the update succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the permission ID that was updated.
    /// </summary>
    public Guid? PermissionId { get; init; }

    /// <summary>
    ///     Gets the previous permissions.
    /// </summary>
    public string[ ]? PreviousPermissions { get; init; }

    /// <summary>
    ///     Gets the new permissions.
    /// </summary>
    public string[ ]? NewPermissions { get; init; }
}

/// <summary>
///     Represents a user who has access to a resource.
/// </summary>
public record ResourceUser
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
    public required string ResourceId { get; init; }

    /// <summary>
    ///     Gets the permissions the user has.
    /// </summary>
    public required string[ ] Permissions { get; init; }

    /// <summary>
    ///     Gets when the permissions were granted.
    /// </summary>
    public required DateTime GrantedAt { get; init; }

    /// <summary>
    ///     Gets who granted the permissions.
    /// </summary>
    public required Guid GrantedByUserId { get; init; }

    /// <summary>
    ///     Gets when the permissions expire (if applicable).
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Gets when the user last accessed the resource.
    /// </summary>
    public DateTime? LastAccessedAt { get; init; }

    /// <summary>
    ///     Gets whether the permissions are currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    ///     Gets whether the user is an owner of this resource.
    /// </summary>
    public bool IsOwner { get => Permissions.Contains("Owner", StringComparer.OrdinalIgnoreCase); }
}
