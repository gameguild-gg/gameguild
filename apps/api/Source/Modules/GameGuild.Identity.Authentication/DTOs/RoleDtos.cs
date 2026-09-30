namespace GameGuild.Identity.Authentication;

/// <summary>
///     DTO representing a role
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     Request DTO for creating a new role
/// </summary>
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public Guid? TenantId { get; set; }
}

/// <summary>
///     Request DTO for updating an existing role
/// </summary>
public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
///     Request DTO for assigning a role to a user
/// </summary>
public class AssignRoleToUserRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
///     Request DTO for removing a role from a user
/// </summary>
public class RemoveRoleFromUserRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

/// <summary>
///     Response DTO for user role assignment
/// </summary>
public class UserRoleDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public RoleDto? Role { get; set; }
    public Guid? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>Outcome for a user in a bulk role assignment.</summary>
public sealed record BulkRoleAssignmentItemResult(
    Guid UserId,
    Guid AssignmentId,
    BulkRoleAssignmentStatus Status,
    DateTime AssignedAt,
    DateTime? ExpiresAt);

/// <summary>Result status for one requested user.</summary>
public enum BulkRoleAssignmentStatus
{
    Assigned,
    Reactivated,
    AlreadyAssigned
}

/// <summary>Summary and per-user outcomes for a bulk role assignment.</summary>
public sealed class BulkRoleAssignmentResult
{
    public Guid RoleId { get; init; }
    public int TotalRequested { get; init; }
    public int DuplicateUserIds { get; init; }
    public int Assigned { get; init; }
    public int Reactivated { get; init; }
    public int AlreadyAssigned { get; init; }
    public IReadOnlyList<BulkRoleAssignmentItemResult> Users { get; init; } = Array.Empty<BulkRoleAssignmentItemResult>();
}
