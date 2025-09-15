using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Features.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Features.Services;

/// <summary>
/// Service implementation for feature flag management and evaluation
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly Dictionary<string, FeatureFlag> _cache = new();
    private DateTime _lastCacheUpdate = DateTime.UtcNow.AddMinutes(-10); // Force initial cache load

    public FeatureFlagService(ApplicationDbContext context, ILogger<FeatureFlagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FeatureEvaluationResult> EvaluateFeatureAsync(
        string featureKey,
        FeatureContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await GetFeatureFlagByKeyAsync(featureKey, cancellationToken);

            if (featureFlag == null)
            {
                _logger.LogWarning("Feature flag '{FeatureKey}' not found", featureKey);
                return new FeatureEvaluationResult
                {
                    FeatureKey = featureKey,
                    IsEnabled = false,
                    Reason = "Feature flag not found"
                };
            }

            // Check environment
            if (!string.IsNullOrEmpty(featureFlag.Environment) &&
                featureFlag.Environment != context.Environment)
            {
                return new FeatureEvaluationResult
                {
                    FeatureKey = featureKey,
                    IsEnabled = false,
                    Reason = $"Environment mismatch: expected {featureFlag.Environment}, got {context.Environment}"
                };
            }

            // Check tenant-specific flags
            if (!featureFlag.IsGlobal && featureFlag.TenantId.HasValue &&
                featureFlag.TenantId != context.TenantId)
            {
                return new FeatureEvaluationResult
                {
                    FeatureKey = featureKey,
                    IsEnabled = false,
                    Reason = "Tenant mismatch"
                };
            }

            // Evaluate targeting rules
            var targetingResult = await EvaluateTargetingRulesAsync(featureFlag, context, cancellationToken);
            if (targetingResult != null)
            {
                await RecordUsageAsync(featureFlag, context, targetingResult.IsEnabled, targetingResult.Value, targetingResult.Reason, cancellationToken);
                return targetingResult;
            }

            // Check percentage rollout
            if (featureFlag.RolloutPercentage < 100)
            {
                var userHash = GetUserHash(context.UserId?.ToString() ?? context.IpAddress ?? "anonymous", featureFlag.Key);
                var isInRollout = userHash % 100 < featureFlag.RolloutPercentage;

                if (!isInRollout)
                {
                    var result = new FeatureEvaluationResult
                    {
                        FeatureKey = featureKey,
                        IsEnabled = false,
                        Value = GetTypedValue(featureFlag.DefaultValue, featureFlag.Type),
                        Reason = $"User not in rollout percentage ({featureFlag.RolloutPercentage}%)"
                    };

                    await RecordUsageAsync(featureFlag, context, result.IsEnabled, result.Value, result.Reason, cancellationToken);
                    return result;
                }
            }

            // Global feature flag evaluation
            var globalResult = new FeatureEvaluationResult
            {
                FeatureKey = featureKey,
                IsEnabled = featureFlag.IsEnabled,
                Value = featureFlag.IsEnabled
                    ? GetTypedValue(featureFlag.EnabledValue, featureFlag.Type)
                    : GetTypedValue(featureFlag.DefaultValue, featureFlag.Type),
                Reason = featureFlag.IsEnabled ? "Feature enabled" : "Feature disabled"
            };

            await RecordUsageAsync(featureFlag, context, globalResult.IsEnabled, globalResult.Value, globalResult.Reason, cancellationToken);
            return globalResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag '{FeatureKey}'", featureKey);
            return new FeatureEvaluationResult
            {
                FeatureKey = featureKey,
                IsEnabled = false,
                Reason = $"Evaluation error: {ex.Message}"
            };
        }
    }

    public async Task<bool> GetBooleanAsync(
        string featureKey,
        bool defaultValue = false,
        FeatureContext? context = null,
        CancellationToken cancellationToken = default)
    {
        context ??= new FeatureContext();
        var result = await EvaluateFeatureAsync(featureKey, context, cancellationToken);

        if (!result.IsEnabled) return defaultValue;

        return result.GetValue<bool>() ?? defaultValue;
    }

    public async Task<string> GetStringAsync(
        string featureKey,
        string defaultValue = "",
        FeatureContext? context = null,
        CancellationToken cancellationToken = default)
    {
        context ??= new FeatureContext();
        var result = await EvaluateFeatureAsync(featureKey, context, cancellationToken);

        if (!result.IsEnabled) return defaultValue;

        return result.GetValue<string>() ?? defaultValue;
    }

    public async Task<int> GetIntAsync(
        string featureKey,
        int defaultValue = 0,
        FeatureContext? context = null,
        CancellationToken cancellationToken = default)
    {
        context ??= new FeatureContext();
        var result = await EvaluateFeatureAsync(featureKey, context, cancellationToken);

        if (!result.IsEnabled) return defaultValue;

        return result.GetValue<int>() ?? defaultValue;
    }

    public async Task<double> GetDoubleAsync(
        string featureKey,
        double defaultValue = 0d,
        FeatureContext? context = null,
        CancellationToken cancellationToken = default)
    {
        context ??= new FeatureContext();
        var result = await EvaluateFeatureAsync(featureKey, context, cancellationToken);

        if (!result.IsEnabled) return defaultValue;

        return result.GetValue<double>() ?? defaultValue;
    }

    public async Task<FeatureFlag> CreateFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default)
    {
        _context.FeatureFlags.Add(featureFlag);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return featureFlag;
    }

    public async Task<FeatureFlag?> UpdateFeatureFlagAsync(Guid id, FeatureFlag featureFlag, CancellationToken cancellationToken = default)
    {
        var existing = await _context.FeatureFlags.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null) return null;

        existing.Name = featureFlag.Name;
        existing.Description = featureFlag.Description;
        existing.IsEnabled = featureFlag.IsEnabled;
        existing.Type = featureFlag.Type;
        existing.DefaultValue = featureFlag.DefaultValue;
        existing.EnabledValue = featureFlag.EnabledValue;
        existing.IsGlobal = featureFlag.IsGlobal;
        existing.RolloutPercentage = featureFlag.RolloutPercentage;
        existing.Environment = featureFlag.Environment;
        existing.TenantId = featureFlag.TenantId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return existing;
    }

    public async Task<bool> DeleteFeatureFlagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var featureFlag = await _context.FeatureFlags.FindAsync(new object[] { id }, cancellationToken);
        if (featureFlag == null) return false;

        _context.FeatureFlags.Remove(featureFlag);
        await _context.SaveChangesAsync(cancellationToken);

        InvalidateCache();
        return true;
    }

    public async Task<FeatureFlag?> GetFeatureFlagByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FeatureFlags
            .Include(f => f.Targets)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FeatureFlag?> GetFeatureFlagByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        // Try cache first
        await RefreshCacheIfNeeded(cancellationToken);

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Fallback to database
        var featureFlag = await _context.FeatureFlags
            .Include(f => f.Targets)
            .FirstOrDefaultAsync(f => f.Key == key, cancellationToken);

        if (featureFlag != null)
        {
            _cache[key] = featureFlag;
        }

        return featureFlag;
    }

    public async Task<IEnumerable<FeatureFlag>> GetFeatureFlagsAsync(
        Guid? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FeatureFlags.Include(f => f.Targets).AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(f => f.IsGlobal || f.TenantId == tenantId.Value);
        }

        if (!string.IsNullOrEmpty(environment))
        {
            query = query.Where(f => f.Environment == environment);
        }

        return await query.OrderBy(f => f.Key).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FeatureFlagUsage>> GetUsageAnalyticsAsync(
        Guid featureFlagId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FeatureFlagUsage.Where(u => u.FeatureFlagId == featureFlagId);

        if (fromDate.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= toDate.Value);
        }

        return await query.OrderByDescending(u => u.CreatedAt).ToListAsync(cancellationToken);
    }

    private async Task<FeatureEvaluationResult?> EvaluateTargetingRulesAsync(
        FeatureFlag featureFlag,
        FeatureContext context,
        CancellationToken cancellationToken)
    {
        if (!featureFlag.Targets.Any()) return null;

        foreach (var target in featureFlag.Targets.OrderBy(t => t.CreatedAt))
        {
            bool matches = target.TargetType.ToLowerInvariant() switch
            {
                "user" => context.UserId?.ToString() == target.TargetIdentifier,
                "tenant" => context.TenantId?.ToString() == target.TargetIdentifier,
                "role" => context.UserRoles.Contains(target.TargetIdentifier),
                "ip" => context.IpAddress == target.TargetIdentifier,
                _ => false
            };

            if (matches)
            {
                return new FeatureEvaluationResult
                {
                    FeatureKey = featureFlag.Key,
                    IsEnabled = target.IsIncluded,
                    Value = target.IsIncluded
                        ? GetTypedValue(target.Value ?? featureFlag.EnabledValue, featureFlag.Type)
                        : GetTypedValue(featureFlag.DefaultValue, featureFlag.Type),
                    Reason = $"Matched {target.TargetType} target: {target.TargetIdentifier}"
                };
            }
        }

        return null;
    }

    private async Task RecordUsageAsync(
        FeatureFlag featureFlag,
        FeatureContext context,
        bool wasEnabled,
        object? value,
        string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var usage = new FeatureFlagUsage
            {
                FeatureFlagId = featureFlag.Id,
                UserId = context.UserId,
                TenantId = context.TenantId,
                WasEnabled = wasEnabled,
                ReturnedValue = value?.ToString(),
                Environment = context.Environment,
                Reason = reason,
                ContextData = JsonSerializer.Serialize(new
                {
                    UserRoles = context.UserRoles,
                    CustomAttributes = context.CustomAttributes,
                    IpAddress = context.IpAddress,
                    UserAgent = context.UserAgent
                })
            };

            _context.FeatureFlagUsage.Add(usage);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording feature flag usage for '{FeatureKey}'", featureFlag.Key);
        }
    }

    private object? GetTypedValue(string? value, FeatureFlagType type)
    {
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            return type switch
            {
                FeatureFlagType.Toggle => bool.Parse(value),
                FeatureFlagType.Numeric => double.Parse(value),
                FeatureFlagType.String => value,
                FeatureFlagType.Percentage => double.Parse(value),
                FeatureFlagType.UserSegment => value,
                _ => value
            };
        }
        catch
        {
            return value;
        }
    }

    private int GetUserHash(string identifier, string featureKey)
    {
        var combined = $"{identifier}:{featureKey}";
        return Math.Abs(combined.GetHashCode());
    }

    private async Task RefreshCacheIfNeeded(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastCacheUpdate > TimeSpan.FromMinutes(5))
        {
            await RefreshCache(cancellationToken);
        }
    }

    private async Task RefreshCache(CancellationToken cancellationToken)
    {
        try
        {
            var flags = await _context.FeatureFlags
                .Include(f => f.Targets)
                .ToListAsync(cancellationToken);

            _cache.Clear();
            foreach (var flag in flags)
            {
                _cache[flag.Key] = flag;
            }

            _lastCacheUpdate = DateTime.UtcNow;
            _logger.LogDebug("Refreshed feature flag cache with {Count} flags", flags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing feature flag cache");
        }
    }

    private void InvalidateCache()
    {
        _cache.Clear();
        _lastCacheUpdate = DateTime.UtcNow.AddMinutes(-10); // Force refresh on next request
    }
}
