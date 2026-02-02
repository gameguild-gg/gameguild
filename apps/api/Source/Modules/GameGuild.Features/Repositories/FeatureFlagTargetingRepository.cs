using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Features;

/// <summary>
///     Repository implementation for feature flag targeting operations
/// </summary>
public class FeatureFlagTargetingRepository : IFeatureFlagTargetingRepository
{
    private readonly IApplicationDbContext _context;

    public FeatureFlagTargetingRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateTargetAsync(FeatureFlagTarget target, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlagTarget>().Add(target);
        await _context.SaveChangesAsync(cancellationToken);
        return target.Id;
    }

    public async Task UpdateTargetAsync(FeatureFlagTarget target, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlagTarget>().Update(target);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTargetAsync(Guid targetId, CancellationToken cancellationToken = default)
    {
        var target = await GetTargetByIdAsync(targetId, cancellationToken);
        if (target != null)
        {
            target.DeletedAt = DateTimeOffset.UtcNow.DateTime;
            await UpdateTargetAsync(target, cancellationToken);
        }
    }

    public async Task<IEnumerable<FeatureFlagTarget>> GetTargetsAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagTarget>()
            .Where(t => t.FeatureFlagId == featureFlagId && t.DeletedAt == null)
            .OrderBy(t => t.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeatureFlagTarget?> GetTargetByIdAsync(Guid targetId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagTarget>()
            .FirstOrDefaultAsync(t => t.Id == targetId && t.DeletedAt == null, cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlagTarget>> GetTargetsByTypeAsync(Guid featureFlagId, string targetType, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagTarget>()
            .Where(t => t.FeatureFlagId == featureFlagId && t.TargetType == targetType && t.DeletedAt == null)
            .OrderBy(t => t.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlagTarget>> GetTargetsByIdentifierAsync(Guid featureFlagId, string targetIdentifier, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagTarget>()
            .Where(t => t.FeatureFlagId == featureFlagId && t.TargetIdentifier == targetIdentifier && t.DeletedAt == null)
            .OrderBy(t => t.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Guid>> CreateTargetsAsync(IEnumerable<FeatureFlagTarget> targets, CancellationToken cancellationToken = default)
    {
        var targetsList = targets.ToList();
        _context.Set<FeatureFlagTarget>().AddRange(targetsList);
        await _context.SaveChangesAsync(cancellationToken);
        return targetsList.Select(t => t.Id);
    }

    public async Task DeleteTargetsByFeatureFlagAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        var targets = await GetTargetsAsync(featureFlagId, cancellationToken);
        foreach (var target in targets)
        {
            target.DeletedAt = DateTimeOffset.UtcNow.DateTime;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
