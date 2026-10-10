using GameGuild.Identity.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Provisioning;

/// <summary>
///     Repository implementation for SCIM group mappings (tenant roles) and their
///     read-side projections.
/// </summary>
public class ScimGroupMappingRepository(IApplicationDbContext context) : IScimGroupMappingRepository
{
    private DbSet<ScimGroupMapping> Mappings => context.Set<ScimGroupMapping>();
    private DbSet<Role> Roles => context.Set<Role>();

    public async Task<ScimGroupMapping?> GetByExternalIdAsync(Guid tenantId, string externalId, CancellationToken cancellationToken = default)
    {
        return await Mappings
            .FirstOrDefaultAsync(mapping => mapping.TenantId == tenantId
                                            && mapping.ExternalId == externalId
                                            && mapping.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScimGroupMapping?> GetByRoleIdAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await Mappings
            .FirstOrDefaultAsync(mapping => mapping.TenantId == tenantId
                                            && mapping.RoleId == roleId
                                            && mapping.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScimGroupView?> GetViewAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await QueryTenantGroups(tenantId)
            .FirstOrDefaultAsync(view => view.RoleId == roleId, cancellationToken).ConfigureAwait(false);
    }

    public IQueryable<ScimGroupView> QueryTenantGroups(Guid tenantId)
    {
        return Mappings
            .AsNoTracking()
            .Where(mapping => mapping.TenantId == tenantId && mapping.DeletedAt == null)
            .Join(Roles.AsNoTracking(),
                mapping => mapping.RoleId,
                role => role.Id,
                (mapping, role) => new ScimGroupView(
                    role.Id,
                    mapping.ExternalId,
                    role.Name,
                    role.Description,
                    role.IsActive && role.DeletedAt == null,
                    role.CreatedAt,
                    role.UpdatedAt));
    }

    public void Add(ScimGroupMapping mapping) => Mappings.Add(mapping);

    public void Remove(ScimGroupMapping mapping) => Mappings.Remove(mapping);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
