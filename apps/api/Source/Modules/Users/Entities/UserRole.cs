namespace GameGuild.Modules.Users.Entities;

/// <summary>
///     Junction entity representing user-role assignments
/// </summary>
public sealed class UserRole : EntityBase<Guid>
{
    /// <summary>
    ///     User identifier
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Role identifier
    /// </summary>
    public required Guid RoleId { get; init; }

    /// <summary>
    ///     When the role was assigned
    /// </summary>
    public DateTime AssignedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    ///     Who assigned the role (user ID)
    /// </summary>
    public Guid? AssignedBy { get; init; }

    /// <summary>
    ///     When the role assignment expires (null = never)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Navigation property to User
    /// </summary>
    public User User { get; init; } = null!;

    /// <summary>
    ///     Navigation property to Role
    /// </summary>
    public Role Role { get; init; } = null!;

    /// <summary>
    ///     Checks if the role assignment is still valid
    /// </summary>
    public bool IsValid()
    {
        return ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    }

    /// <summary>
    ///     Checks if the role assignment has expired
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }
}
