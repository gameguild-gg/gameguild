using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Query to get all roles
/// </summary>
public sealed record GetRolesQuery : IQuery<List<RoleDto>>
{
    public Guid? TenantId { get; init; }
    public bool IncludeInactive { get; init; }
}

/// <summary>
///     Query to get a role by ID
/// </summary>
public sealed record GetRoleByIdQuery : IQuery<RoleDto?>
{
    public Guid RoleId { get; init; }
}

/// <summary>
///     Query to get all roles assigned to a user
/// </summary>
public sealed record GetUserRolesQuery : IQuery<List<RoleDto>>
{
    public Guid UserId { get; init; }
    public bool IncludeExpired { get; init; }
}
