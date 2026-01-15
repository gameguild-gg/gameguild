using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets;

/// <summary>
/// Repository implementation for TransformedAsset entities.
/// </summary>
public class TransformedAssetRepository : ITransformedAssetRepository
{
    private readonly IApplicationDbContext _context;

    public TransformedAssetRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TransformedAsset?> GetAsync(
        Guid sourceContentId,
        string transformationSpec,
        CancellationToken ct = default)
    {
        return await _context.Set<TransformedAsset>()
            .FirstOrDefaultAsync(x => 
                x.SourceContentId == sourceContentId && 
                x.TransformationSpec == transformationSpec, ct);
    }

    public async Task<TransformedAsset> AddAsync(TransformedAsset asset, CancellationToken ct = default)
    {
        await _context.Set<TransformedAsset>().AddAsync(asset, ct);
        await _context.SaveChangesAsync(ct);
        return asset;
    }

    public async Task UpdateAsync(TransformedAsset asset, CancellationToken ct = default)
    {
        _context.Set<TransformedAsset>().Update(asset);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TransformedAsset>> GetStaleAssetsAsync(
        TimeSpan maxAge,
        int limit = 100,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        
        return await _context.Set<TransformedAsset>()
            .Where(x => x.LastAccessedAt < cutoff)
            .OrderBy(x => x.LastAccessedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _context.Set<TransformedAsset>()
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteBySourceAsync(Guid sourceContentId, CancellationToken ct = default)
    {
        await _context.Set<TransformedAsset>()
            .Where(x => x.SourceContentId == sourceContentId)
            .ExecuteDeleteAsync(ct);
    }
}
