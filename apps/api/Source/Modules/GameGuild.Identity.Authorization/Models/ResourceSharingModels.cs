namespace GameGuild.Identity.Authorization;

/// <summary>
///     DTO for resource access details.
/// </summary>
/// <param name="UserId">The user's ID.</param>
/// <param name="UserName">The user's display name.</param>
/// <param name="Email">The user's email.</param>
/// <param name="Permissions">The permissions granted.</param>
/// <param name="GrantedAt">When the permissions were granted.</param>
/// <param name="ExpiresAt">When the permissions expire.</param>
/// <param name="IsOwner">Whether the user is the resource owner.</param>
public sealed record ResourceAccessDto(
    Guid UserId,
    string UserName,
    string Email,
    string[] Permissions,
    DateTime GrantedAt,
    DateTime? ExpiresAt = null,
    bool IsOwner = false);

/// <summary>
///     Request to share a resource with a user.
/// </summary>
/// <param name="Email">The email address of the user to share with.</param>
/// <param name="Permissions">The permissions to grant.</param>
/// <param name="ExpiresAt">Optional expiration date for the share.</param>
/// <param name="Message">Optional message to include with the invitation.</param>
public sealed record ShareResourceRequest(
    string Email,
    string[] Permissions,
    DateTime? ExpiresAt = null,
    string? Message = null);

/// <summary>
///     Result of sharing a resource.
/// </summary>
public class ShareResult
{
    public bool Success { get; init; }
    public bool IsNewUser { get; init; }
    public Guid? UserId { get; init; }
    public Guid? InvitationId { get; init; }
    public string? Email { get; init; }
    public string? ErrorMessage { get; init; }
    public string? InvitationLink { get; init; }

    public static ShareResult SuccessWithUser(Guid userId, string email)
    {
        return new ShareResult { Success = true, IsNewUser = false, UserId = userId, Email = email };
    }

    public static ShareResult SuccessWithInvitation(Guid invitationId, string email, string invitationLink)
    {
        return new ShareResult
        {
            Success = true,
            IsNewUser = true,
            InvitationId = invitationId,
            Email = email,
            InvitationLink = invitationLink
        };
    }

    public static ShareResult Failure(string errorMessage)
    {
        return new ShareResult { Success = false, ErrorMessage = errorMessage };
    }
}

/// <summary>
///     Request to update permissions for a user.
/// </summary>
/// <param name="UserId">The user's ID.</param>
/// <param name="Permissions">The new set of permissions.</param>
/// <param name="ExpiresAt">Optional new expiration date.</param>
public sealed record UpdatePermissionsRequest(
    Guid UserId,
    string[] Permissions,
    DateTime? ExpiresAt = null);

/// <summary>
///     Result of updating permissions.
/// </summary>
public class PermissionUpdateResult
{
    public bool Success { get; init; }
    public Guid UserId { get; init; }
    public string[] UpdatedPermissions { get; init; } = [];
    public string? ErrorMessage { get; init; }

    public static PermissionUpdateResult SuccessResult(Guid userId, string[] permissions)
    {
        return new PermissionUpdateResult { Success = true, UserId = userId, UpdatedPermissions = permissions };
    }

    public static PermissionUpdateResult Failure(string errorMessage)
    {
        return new PermissionUpdateResult { Success = false, ErrorMessage = errorMessage };
    }
}

/// <summary>
///     Request to remove a user's access to a resource.
/// </summary>
/// <param name="UserId">The user's ID.</param>
/// <param name="Reason">Optional reason for removal.</param>
public sealed record RemoveAccessRequest(
    Guid UserId,
    string? Reason = null);

/// <summary>
///     Result of a bulk share operation.
/// </summary>
public class BulkShareResult
{
    public bool Success { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<ShareResult> Results { get; init; } = [];
    public string? ErrorMessage { get; init; }
}

/// <summary>
///     Response containing resource users with their permissions.
/// </summary>
public class ResourceUsersResponse
{
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public List<ResourceAccessDto> Users { get; init; } = [];
    public List<PendingInvitationDto> PendingInvitations { get; init; } = [];
    public int TotalCount { get; init; }
}

/// <summary>
///     DTO for pending invitation.
/// </summary>
/// <param name="InvitationId">The invitation ID.</param>
/// <param name="Email">The invitee's email.</param>
/// <param name="Permissions">The permissions to be granted.</param>
/// <param name="InvitedAt">When the invitation was sent.</param>
/// <param name="ExpiresAt">When the invitation expires.</param>
/// <param name="Status">The current invitation status.</param>
public sealed record PendingInvitationDto(
    Guid InvitationId,
    string Email,
    string[] Permissions,
    DateTime InvitedAt,
    DateTime? ExpiresAt,
    string Status);

/// <summary>
///     Request to apply a permission template.
/// </summary>
public sealed record ApplyTemplateRequest(
    string TemplateId,
    Guid[] UserIds);

/// <summary>
///     Resource sharing configuration.
/// </summary>
public class ResourceSharingConfig
{
    public bool AllowPublicSharing { get; init; }
    public bool AllowAnonymousAccess { get; init; }
    public int MaxSharesPerResource { get; init; } = 100;
    public TimeSpan DefaultInvitationExpiry { get; init; } = TimeSpan.FromDays(7);
    public string[] AllowedPermissions { get; init; } = ["read", "write", "admin", "delete"];
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
    public required string[] Permissions { get; init; }

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
    public bool IsOwner => Permissions.Contains("Owner", StringComparer.OrdinalIgnoreCase);
}
