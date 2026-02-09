using System.Text.Json;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for GetRolesQuery
/// </summary>
public sealed class GetRolesQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetRolesQuery, List<RoleDto>>
{
    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetAllAsync(request.TenantId, request.IncludeInactive, cancellationToken).ConfigureAwait(false);

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Permissions = JsonSerializer.Deserialize<List<string>>(r.Permissions) ?? new List<string>(),
            IsActive = r.IsActive,
            TenantId = r.TenantId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }
}

/// <summary>
///     Handler for GetRoleByIdQuery
/// </summary>
public sealed class GetRoleByIdQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetRoleByIdQuery, RoleDto?>
{
    public async Task<RoleDto?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken).ConfigureAwait(false);

        if (role == null)
        {
            return null;
        }

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = JsonSerializer.Deserialize<List<string>>(role.Permissions) ?? new List<string>(),
            IsActive = role.IsActive,
            TenantId = role.TenantId,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}

/// <summary>
///     Handler for GetUserRolesQuery
/// </summary>
public sealed class GetUserRolesQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetUserRolesQuery, List<RoleDto>>
{
    public async Task<List<RoleDto>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetUserRolesAsync(request.UserId, request.IncludeExpired, cancellationToken).ConfigureAwait(false);

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Permissions = JsonSerializer.Deserialize<List<string>>(r.Permissions) ?? new List<string>(),
            IsActive = r.IsActive,
            TenantId = r.TenantId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }
}
