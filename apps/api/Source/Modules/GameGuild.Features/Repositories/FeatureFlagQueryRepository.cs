using GameGuild.Abstractions;
using GameGuild.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GameGuild.Features;

/// <summary>
///     Repository implementation for feature flag CRUD and query operations
/// </summary>
public class FeatureFlagQueryRepository : IFeatureFlagQueryRepository
{
    private readonly IApplicationDbContext _context;

    public FeatureFlagQueryRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Include(f => f.Targets)
            .Include(f => f.UsageAnalytics)
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken);
    }

    public async Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Include(f => f.Targets)
            .FirstOrDefaultAsync(f => f.Key == key && f.DeletedAt == null, cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlag>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => f.IsEnabled && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlag>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => f.Environment == environment && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => (f.TenantId == tenantId || f.IsGlobal) && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlag>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => keys.Contains(f.Key) && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken);
    }

    public Task<IEnumerable<FeatureFlagTargetDto>> GetTargetingRulesAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetTargetingRulesAsync will be implemented in follow-up work");
    }

    public Task<FeatureFlagTargetDto?> GetTargetingRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetTargetingRuleByIdAsync will be implemented in follow-up work");
    }

    public Task<IEnumerable<FeatureFlagUsageSummary>> GetUsageSummaryAsync(string featureKey, DateTime? startDate, DateTime? endDate, string? groupBy, CancellationToken cancellationToken = default)
    {
        // TODO: Implement usage summary aggregation
        return Task.FromResult(Enumerable.Empty<FeatureFlagUsageSummary>());
    }

    public Task<FeatureFlagStatistics> GetStatisticsAsync(string environment, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        // TODO: Implement statistics gathering - return stub for now
        throw new NotImplementedException("Statistics gathering not yet implemented");
    }

    public Task<PagedResult<FeatureFlagEvaluationHistory>> GetEvaluationHistoryAsync(
        string featureKey,
        DateTime? startDate,
        DateTime? endDate,
        Guid? tenantId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement evaluation history tracking
        return Task.FromResult(new PagedResult<FeatureFlagEvaluationHistory>(
            Enumerable.Empty<FeatureFlagEvaluationHistory>(),
            0,
            page,
            pageSize));
    }

    public Task<IEnumerable<FeatureFlagDependency>> GetDependenciesAsync(Guid featureFlagId, bool includeInverse, CancellationToken cancellationToken = default)
    {
        // TODO: Implement dependency tracking
        return Task.FromResult(Enumerable.Empty<FeatureFlagDependency>());
    }

    public async Task<IEnumerable<FeatureFlagConfig>> GetConfigsAsync(string environment, string? tenantId, IEnumerable<string>? featureKeys, DateTime? modifiedSince, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlag>()
            .Where(f => f.Environment == environment && f.DeletedAt == null);

        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var parsedTenantId))
        {
            query = query.Where(f => f.TenantId == parsedTenantId || f.IsGlobal);
        }

        if (featureKeys != null && featureKeys.Any())
        {
            query = query.Where(f => featureKeys.Contains(f.Key));
        }

        if (modifiedSince.HasValue)
        {
            query = query.Where(f => f.UpdatedAt >= modifiedSince.Value);
        }

        var flags = await query.Include(f => f.Targets).ToListAsync(cancellationToken);

        return flags.Select(f => new FeatureFlagConfig
        {
            Key = f.Key,
            Name = f.Name,
            Description = f.Description,
            IsEnabled = f.IsEnabled,
            Type = f.Type,
            DefaultValue = f.DefaultValue,
            EnabledValue = f.EnabledValue,
            Environment = f.Environment,
            RolloutPercentage = f.RolloutPercentage
        });
    }

    public Task<FeatureFlagAnalytics> GetAnalyticsAsync(string featureKey, DateTime? startDate, DateTime? endDate, string? environment, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement comprehensive analytics
        throw new NotImplementedException("Analytics not yet fully implemented");
    }

    public async Task<FeatureFlag> AddAsync(FeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<FeatureFlag> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeatureFlag> UpdateAsync(FeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateRangeAsync(IEnumerable<FeatureFlag> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(FeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _context.Set<FeatureFlag>().Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveRangeAsync(IEnumerable<FeatureFlag> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            entity.DeletedAt = DateTimeOffset.UtcNow.DateTime;
            await UpdateAsync(entity, cancellationToken);
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<FeatureFlag>()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity != null)
        {
            entity.DeletedAt = null;
            await UpdateAsync(entity, cancellationToken);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken);
    }

    public async Task<IPage<FeatureFlag>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlag>()
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (IPage<FeatureFlag>)new PagedResult<FeatureFlag>(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<FeatureFlag>> FindAsync(Expression<Func<FeatureFlag, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(predicate)
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeatureFlag?> FirstOrDefaultAsync(Expression<Func<FeatureFlag, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(predicate)
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<FeatureFlag, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(predicate)
            .Where(f => f.DeletedAt == null)
            .AnyAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<FeatureFlag, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlag>()
            .Where(f => f.DeletedAt == null);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .AnyAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken);
    }
}
