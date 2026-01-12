using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Entity Framework implementation of the tenant security version repository.
/// </summary>
public class TenantSecurityVersionRepository(IApplicationDbContext context) : ITenantSecurityVersionRepository
{
    public async Task<TenantSecurityVersion?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantSecurityVersion>()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantSecurityVersion> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        
        if (existing != null)
            return existing;

        var newVersion = new TenantSecurityVersion
        {
            TenantId = tenantId,
            SecurityVersion = 1,
            LastUpdatedAt = DateTime.UtcNow,
            LastChangeReason = "Initial creation"
        };

        await AddAsync(newVersion, cancellationToken).ConfigureAwait(false);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return newVersion;
    }

    public async Task<long> IncrementVersionAsync(Guid tenantId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var version = await GetOrCreateAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var newVersion = version.IncrementVersion(reason);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return newVersion;
    }

    public async Task AddAsync(TenantSecurityVersion version, CancellationToken cancellationToken = default)
    {
        await context.Set<TenantSecurityVersion>().AddAsync(version, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(TenantSecurityVersion version, CancellationToken cancellationToken = default)
    {
        version.Touch();
        context.Set<TenantSecurityVersion>().Update(version);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
