using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Queries;

/// <summary>
///     Query to get all roles
/// </summary>
public record GetRolesQuery : IQuery<List<RoleDto>>
{
    public Guid? TenantId { get; init; }
    public bool IncludeInactive { get; init; }
}

/// <summary>
///     Query to get a role by ID
/// </summary>
public record GetRoleByIdQuery : IQuery<RoleDto?>
{
    public Guid RoleId { get; init; }
}

/// <summary>
///     Query to get all roles assigned to a user
/// </summary>
public record GetUserRolesQuery : IQuery<List<RoleDto>>
{
    public Guid UserId { get; init; }
    public bool IncludeExpired { get; init; }
}
