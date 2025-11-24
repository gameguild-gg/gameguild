namespace GameGuild.Authentication.Entities;

/// <summary>
///     Junction entity representing the many-to-many relationship between users and roles
/// </summary>
public class UserRole : EntityBase<Guid>
{
    /// <summary>
    ///     Default parameterless constructor (required by Entity Framework)
    /// </summary>
    public UserRole() { }

    /// <summary>
    ///     Constructor for creating a user-role assignment
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleId">Role ID</param>
    /// <param name="assignedBy">ID of the user who assigned this role</param>
    public UserRole(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedBy = assignedBy;
        AssignedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Role ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    ///     ID of the user who assigned this role
    /// </summary>
    public Guid? AssignedBy { get; set; }

    /// <summary>
    ///     When this role was assigned to the user
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    ///     Optional expiration date for temporary role assignments
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     Navigation property to Role
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    ///     Check if this role assignment has expired
    /// </summary>
    /// <returns>True if the role assignment has expired</returns>
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    ///     Check if this is a permanent role assignment
    /// </summary>
    /// <returns>True if there is no expiration date</returns>
    public bool IsPermanent() => !ExpiresAt.HasValue;
}
