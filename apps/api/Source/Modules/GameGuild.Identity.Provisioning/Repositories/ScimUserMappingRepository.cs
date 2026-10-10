using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Provisioning;

/// <summary>
///     Repository implementation for SCIM user mappings and their read-side projections.
/// </summary>
public class ScimUserMappingRepository(IApplicationDbContext context) : IScimUserMappingRepository
{
    private DbSet<ScimUserMapping> Mappings => context.Set<ScimUserMapping>();
    private DbSet<User> Users => context.Set<User>();

    public async Task<ScimUserMapping?> GetByExternalIdAsync(Guid tenantId, string externalId, CancellationToken cancellationToken = default)
    {
        return await Mappings
            .FirstOrDefaultAsync(mapping => mapping.TenantId == tenantId
                                            && mapping.ExternalId == externalId
                                            && mapping.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScimUserMapping?> GetByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await Mappings
            .FirstOrDefaultAsync(mapping => mapping.TenantId == tenantId
                                            && mapping.UserId == userId
                                            && mapping.DeletedAt == null,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<ScimUserView?> GetViewAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await QueryTenantUsers(tenantId)
            .FirstOrDefaultAsync(view => view.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public IQueryable<ScimUserView> QueryTenantUsers(Guid tenantId)
    {
        return Mappings
            .AsNoTracking()
            .Where(mapping => mapping.TenantId == tenantId && mapping.DeletedAt == null)
            .Join(Users.AsNoTracking(),
                mapping => mapping.UserId,
                user => user.Id,
                (mapping, user) => new ScimUserView(
                    user.Id,
                    mapping.ExternalId,
                    user.Username,
                    user.Email,
                    user.Name,
                    user.PhoneNumber,
                    user.IsActive && !user.IsSuspended && user.DeletedAt == null,
                    user.CreatedAt,
                    user.UpdatedAt));
    }

    public void Add(ScimUserMapping mapping) => Mappings.Add(mapping);

    public void Remove(ScimUserMapping mapping) => Mappings.Remove(mapping);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
