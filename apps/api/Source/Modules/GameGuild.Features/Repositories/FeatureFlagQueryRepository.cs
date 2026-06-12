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
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Include(f => f.Targets)
            .FirstOrDefaultAsync(f => f.Key == key && f.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlag>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => f.IsEnabled && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlag>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => f.Environment == environment && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => (f.TenantId == tenantId || f.IsGlobal) && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlag>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => keys.Contains(f.Key) && f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlagTargetDto>> GetTargetingRulesAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagTarget>()
            .AsNoTracking()
            .Where(t => t.FeatureFlagId == featureFlagId && t.DeletedAt == null)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => ToTargetDto(t))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<FeatureFlagTargetDto?> GetTargetingRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlagTarget>()
            .AsNoTracking()
            .Where(t => t.Id == ruleId && t.DeletedAt == null)
            .Select(t => ToTargetDto(t))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlagUsageSummary>> GetUsageSummaryAsync(string featureKey, DateTime? startDate, DateTime? endDate, string? groupBy, CancellationToken cancellationToken = default)
    {
        var flag = await GetByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false);
        if (flag == null)
            return Enumerable.Empty<FeatureFlagUsageSummary>();

        var query = _context.Set<FeatureFlagUsage>()
            .Where(u => u.FeatureFlagId == flag.Id);

        if (startDate.HasValue) query = query.Where(u => u.LastAccessAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(u => u.LastAccessAt <= endDate.Value);

        var usages = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return new[]
        {
            new FeatureFlagUsageSummary
            {
                FeatureFlagId = flag.Id,
                FeatureFlagKey = flag.Key,
                Name = flag.Name,
                IsEnabled = flag.IsEnabled,
                TotalEvaluations = (int)usages.Sum(u => u.AccessCount),
                UniqueUsers = usages.Where(u => u.UserId.HasValue).Select(u => u.UserId!.Value).Distinct().Count(),
                LastEvaluatedAt = usages.Any() ? usages.Max(u => u.LastAccessAt) : SystemClock.UtcNow,
                CreatedAt = flag.CreatedAt
            }
        };
    }

    public async Task<FeatureFlagStatistics> GetStatisticsAsync(string environment, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var flags = await _context.Set<FeatureFlag>()
            .Where(f => f.Environment == environment && f.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalEnabled = flags.Count(f => f.IsEnabled);
        var totalDisabled = flags.Count(f => !f.IsEnabled);
        var total = flags.Count;

        return new FeatureFlagStatistics
        {
            FeatureFlagId = Guid.Empty,
            FeatureFlagKey = $"aggregate:{environment}",
            TotalEvaluations = total,
            EnabledEvaluations = totalEnabled,
            DisabledEvaluations = totalDisabled,
            EnabledPercentage = total > 0 ? (double)totalEnabled / total * 100 : 0,
            UniqueUsers = 0,
            FirstEvaluationAt = startDate ?? SystemClock.UtcNow,
            LastEvaluationAt = endDate ?? SystemClock.UtcNow,
            PeriodStart = startDate ?? SystemClock.UtcNow.AddMonths(-1),
            PeriodEnd = endDate ?? SystemClock.UtcNow
        };
    }

    public async Task<PagedResult<FeatureFlagEvaluationHistory>> GetEvaluationHistoryAsync(
        string featureKey,
        DateTime? startDate,
        DateTime? endDate,
        Guid? tenantId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var flag = await GetByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false);
        if (flag == null)
            return PagedResult<FeatureFlagEvaluationHistory>.FromPage(
                Enumerable.Empty<FeatureFlagEvaluationHistory>(), 0, page, pageSize);

        var query = _context.Set<FeatureFlagUsage>()
            .Where(u => u.FeatureFlagId == flag.Id);

        if (startDate.HasValue) query = query.Where(u => u.LastAccessAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(u => u.LastAccessAt <= endDate.Value);
        if (tenantId.HasValue) query = query.Where(u => u.TenantId == tenantId.Value);
        if (userId.HasValue) query = query.Where(u => u.UserId == userId.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(u => u.LastAccessAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new FeatureFlagEvaluationHistory
            {
                Id = u.Id,
                FeatureFlagId = flag.Id,
                FeatureFlagKey = featureKey,
                UserId = u.UserId.HasValue ? u.UserId.Value.ToString() : string.Empty,
                EvaluatedValue = u.ReturnedValue ?? (object)u.WasEnabled,
                WasEnabled = u.WasEnabled,
                Environment = u.Environment,
                TenantId = u.TenantId,
                Context = u.ContextData,
                EvaluatedAt = u.LastAccessAt
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return PagedResult<FeatureFlagEvaluationHistory>.FromPage(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<FeatureFlagDependency>> GetDependenciesAsync(Guid featureFlagId, bool includeInverse, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlagDependencyLink>()
            .AsNoTracking()
            .Include(d => d.FeatureFlag)
            .Include(d => d.DependsOnFeatureFlag)
            .Where(d => d.DeletedAt == null && d.FeatureFlag.DeletedAt == null && d.DependsOnFeatureFlag.DeletedAt == null);

        query = includeInverse
            ? query.Where(d => d.FeatureFlagId == featureFlagId || d.DependsOnFeatureFlagId == featureFlagId)
            : query.Where(d => d.FeatureFlagId == featureFlagId);

        return await query
            .OrderBy(d => d.FeatureFlag.Key)
            .ThenBy(d => d.DependsOnFeatureFlag.Key)
            .Select(d => new FeatureFlagDependency
            {
                Id = d.Id,
                FeatureFlagId = d.FeatureFlagId,
                DependsOnFeatureFlagId = d.DependsOnFeatureFlagId,
                DependencyType = d.DependencyType,
                FeatureFlagKey = d.FeatureFlag.Key,
                DependsOnFeatureFlagKey = d.DependsOnFeatureFlag.Key,
                CreatedAt = d.CreatedAt,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
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

    public async Task<FeatureFlagAnalytics> GetAnalyticsAsync(string featureKey, DateTime? startDate, DateTime? endDate, string? environment, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var flag = await GetByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false);
        if (flag == null)
            return new FeatureFlagAnalytics { FeatureKey = featureKey };

        var query = _context.Set<FeatureFlagUsage>()
            .Where(u => u.FeatureFlagId == flag.Id);

        if (startDate.HasValue) query = query.Where(u => u.LastAccessAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(u => u.LastAccessAt <= endDate.Value);
        if (tenantId.HasValue) query = query.Where(u => u.TenantId == tenantId.Value);

        var usages = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return new FeatureFlagAnalytics
        {
            FeatureKey = featureKey,
            TotalAccesses = usages.Sum(u => u.AccessCount),
            EnabledAccesses = usages.Where(u => u.WasEnabled).Sum(u => u.AccessCount),
            DisabledAccesses = usages.Where(u => !u.WasEnabled).Sum(u => u.AccessCount),
            EnabledPercentage = usages.Any() ? (double)usages.Count(u => u.WasEnabled) / usages.Count * 100 : 0,
            UniqueUsers = usages.Where(u => u.UserId.HasValue).Select(u => u.UserId!.Value).Distinct().Count(),
            UniqueTenants = usages.Where(u => u.TenantId.HasValue).Select(u => u.TenantId!.Value).Distinct().Count(),
            FirstAccess = usages.Any() ? usages.Min(u => u.FirstAccessAt) : SystemClock.UtcNow,
            LastAccess = usages.Any() ? usages.Max(u => u.LastAccessAt) : SystemClock.UtcNow,
            PeriodStart = startDate ?? SystemClock.UtcNow.AddMonths(-1),
            PeriodEnd = endDate ?? SystemClock.UtcNow
        };
    }

    public async Task<FeatureFlag> AddAsync(FeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<FeatureFlag> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeatureFlag> UpdateAsync(FeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateRangeAsync(IEnumerable<FeatureFlag> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(FeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().Remove(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity != null)
        {
            _context.Set<FeatureFlag>().Remove(entity);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RemoveRangeAsync(IEnumerable<FeatureFlag> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<FeatureFlag>().RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity != null)
        {
            entity.SoftDelete();
            await UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<FeatureFlag>()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken).ConfigureAwait(false);
        if (entity != null)
        {
            entity.Restore();
            await UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IPage<FeatureFlag>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlag>()
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (IPage<FeatureFlag>)PagedResult<FeatureFlag>.FromPage(items, totalCount, page, pageSize);
    }

    public async Task<IEnumerable<FeatureFlag>> FindAsync(Expression<Func<FeatureFlag, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(predicate)
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FeatureFlag?> FirstOrDefaultAsync(Expression<Func<FeatureFlag, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(predicate)
            .Where(f => f.DeletedAt == null)
            .Include(f => f.Targets)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AnyAsync(Expression<Func<FeatureFlag, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .Where(predicate)
            .Where(f => f.DeletedAt == null)
            .AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(Expression<Func<FeatureFlag, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FeatureFlag>()
            .Where(f => f.DeletedAt == null);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FeatureFlag>()
            .AnyAsync(f => f.Id == id && f.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    private static FeatureFlagTargetDto ToTargetDto(FeatureFlagTarget target) => new()
    {
        Id = target.Id,
        FeatureFlagId = target.FeatureFlagId,
        TargetType = target.TargetType,
        TargetIdentifier = target.TargetIdentifier,
        IsEnabled = target.IsEnabled,
        RolloutPercentage = target.RolloutPercentage,
        CustomValue = target.CustomValue,
        Metadata = target.Metadata,
        Priority = target.Priority,
        CreatedAt = target.CreatedAt,
        UpdatedAt = target.UpdatedAt,
        DeletedAt = target.DeletedAt
    };
}
