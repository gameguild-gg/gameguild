using GameGuild.Abstractions;
using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tenants.Repositories;

/// <summary>
///     Repository interface for TenantMetadata
/// </summary>
public interface ITenantMetadataRepository
{
    Task<TenantMetadata?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<TenantMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<TenantMetadata>> GetByIndustryAsync(string industry, CancellationToken cancellationToken = default);

    Task<List<TenantMetadata>> GetBySizeAsync(TenantSize size, CancellationToken cancellationToken = default);

    Task<List<TenantMetadata>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);

    Task<List<TenantMetadata>> GetByTagsAsync(List<string> tags, CancellationToken cancellationToken = default);

    Task AddAsync(TenantMetadata metadata, CancellationToken cancellationToken = default);

    Task UpdateAsync(TenantMetadata metadata, CancellationToken cancellationToken = default);

    Task DeleteAsync(TenantMetadata metadata, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     EntityFramework implementation of TenantMetadata repository
/// </summary>
public class TenantMetadataRepository(IApplicationDbContext context) : ITenantMetadataRepository
{
    public async Task<TenantMetadata?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMetadata>().FirstOrDefaultAsync(tm => tm.TenantId == tenantId && !tm.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TenantMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMetadata>().FirstOrDefaultAsync(tm => tm.Id == id && !tm.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TenantMetadata>> GetByIndustryAsync(string industry, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMetadata>().Where(tm => tm.Industry == industry && !tm.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TenantMetadata>> GetBySizeAsync(TenantSize size, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMetadata>().Where(tm => tm.Size == size && !tm.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TenantMetadata>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await context.Set<TenantMetadata>().Where(tm => tm.Type == type && !tm.IsDeleted).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TenantMetadata>> GetByTagsAsync(List<string> tags, CancellationToken cancellationToken = default)
    {
        // For PostgreSQL JSONB array contains query
        return await context.Set<TenantMetadata>()
            .Where(tm => !tm.IsDeleted)
            .ToListAsync(cancellationToken) // Load all and filter in memory for now
            .ContinueWith(
                task =>
                {
                    return task.Result.Where(tm =>
                            {
                                var tenantTags = tm.GetTags();

                                return tags.Any(tag => tenantTags.Contains(tag));
                            }
                        )
                        .ToList();
                },
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task AddAsync(TenantMetadata metadata, CancellationToken cancellationToken = default) { await context.Set<TenantMetadata>().AddAsync(metadata, cancellationToken).ConfigureAwait(false); }

    public Task UpdateAsync(TenantMetadata metadata, CancellationToken cancellationToken = default)
    {
        context.Set<TenantMetadata>().Update(metadata);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        metadata.SoftDelete();
        context.Set<TenantMetadata>().Update(metadata);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
