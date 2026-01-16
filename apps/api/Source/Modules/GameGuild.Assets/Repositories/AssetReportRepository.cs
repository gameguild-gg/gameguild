using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets;

/// <summary>
/// Repository implementation for AssetReport entities.
/// </summary>
public class AssetReportRepository : IAssetReportRepository
{
    private readonly IApplicationDbContext _context;

    public AssetReportRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssetReport?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<AssetReport>()
            .Include(x => x.Reference)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<AssetReport>> GetByAssetReferenceAsync(
        Guid assetReferenceId,
        CancellationToken ct = default)
    {
        return await _context.Set<AssetReport>()
            .Where(x => x.AssetReferenceId == assetReferenceId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AssetReport>> GetPendingReportsAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        return await _context.Set<AssetReport>()
            .Include(x => x.Reference)
            .ThenInclude(x => x.Content)
            .Where(x => x.Status == ReportStatus.Pending || x.Status == ReportStatus.UnderReview)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<AssetReport> AddAsync(AssetReport report, CancellationToken ct = default)
    {
        await _context.Set<AssetReport>().AddAsync(report, ct);
        await _context.SaveChangesAsync(ct);
        return report;
    }

    public async Task UpdateAsync(AssetReport report, CancellationToken ct = default)
    {
        _context.Set<AssetReport>().Update(report);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> HasUserReportedAsync(
        Guid assetReferenceId,
        Guid userId,
        CancellationToken ct = default)
    {
        return await _context.Set<AssetReport>()
            .AnyAsync(x => x.AssetReferenceId == assetReferenceId && x.ReportedByUserId == userId, ct);
    }
}
