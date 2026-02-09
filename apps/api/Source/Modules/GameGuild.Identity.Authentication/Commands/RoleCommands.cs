using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to create a new role
/// </summary>
[RequiresQuota(ResourceUsageType.Roles, Source = "CreateRole")]
public sealed record CreateRoleCommand : ICommand<RoleDto>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public Guid? TenantId { get; init; }
}

/// <summary>
///     Command to update an existing role
/// </summary>
public sealed record UpdateRoleCommand : ICommand<RoleDto>
{
    public Guid RoleId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public List<string>? Permissions { get; init; }
    public bool? IsActive { get; init; }
}

/// <summary>
///     Command to delete a role
/// </summary>
public sealed record DeleteRoleCommand : ICommand<bool>
{
    public Guid RoleId { get; init; }
}

/// <summary>
///     Command to assign a role to a user
/// </summary>
public sealed record AssignRoleToUserCommand : ICommand<UserRoleDto>
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public Guid? AssignedBy { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
///     Command to remove a role from a user
/// </summary>
public sealed record RemoveRoleFromUserCommand : ICommand<bool>
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}
