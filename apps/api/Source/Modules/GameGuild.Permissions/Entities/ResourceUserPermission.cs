namespace GameGuild.Permissions.Domain.Entities;

/// <summary>
///     Represents a user's direct permissions on a specific resource.
///     Tracks who granted the permissions, when they were granted, and when they expire.
/// </summary>
public class ResourceUserPermission : EntityBase<Guid>
{
    /// <summary>
    ///     Gets or sets the tenant ID this permission belongs to.
    /// </summary>
    public required TenantId TenantId { get; set; }

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
    public required string[ ] Permissions { get; set; }

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
    public bool IsActive { get => RevokedAt == null && !IsExpired; }

    /// <summary>
    ///     Gets whether these permissions have expired.
    /// </summary>
    public bool IsExpired { get => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow; }

    /// <summary>
    ///     Gets whether the user can access the resource.
    /// </summary>
    public bool CanAccess { get => IsActive; }

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
    public bool UpdatePermissions(string[ ] newPermissions, Guid updatedByUserId)
    {
        if (!IsActive) { return false; }

        Permissions = newPermissions;
        // Note: We don't track permission updates in this entity. 
        // Use audit logs for detailed change tracking.

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
    public bool HasPermission(string permission) { return IsActive && Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase); }

    /// <summary>
    ///     Checks if the user has any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has at least one of the permissions and it's active.</returns>
    public bool HasAnyPermission(params string[ ] permissions) { return IsActive && permissions.Any(p => HasPermission(p)); }

    /// <summary>
    ///     Checks if the user has all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has all of the permissions and it's active.</returns>
    public bool HasAllPermissions(params string[ ] permissions) { return IsActive && permissions.All(p => HasPermission(p)); }

    /// <summary>
    ///     Sets an expiration date for these permissions.
    /// </summary>
    /// <param name="expiresAt">The expiration date.</param>
    public void SetExpiration(DateTime expiresAt) { ExpiresAt = expiresAt; }

    /// <summary>
    ///     Removes the expiration date (makes permissions permanent).
    /// </summary>
    public void RemoveExpiration() { ExpiresAt = null; }
}
