using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Features.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Model;

namespace GameGuild.Modules.Features.Infrastructure;

/// <summary>
/// Custom OpenFeature provider that reads feature flags from GameGuild database
/// </summary>
public class DatabaseFeatureFlagProvider : FeatureProvider {
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<DatabaseFeatureFlagProvider> _logger;
  private readonly Dictionary<string, FeatureFlag> _cache = new Dictionary<string, FeatureFlag>();
  private DateTime _lastCacheUpdate = DateTime.UtcNow.AddMinutes(-10);

  public override Metadata GetMetadata() => new Metadata("GameGuild Database Provider");

  public DatabaseFeatureFlagProvider(IServiceProvider serviceProvider, ILogger<DatabaseFeatureFlagProvider> logger) {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default) {
    try {
      var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

      if (!result.IsEnabled) {
        return new ResolutionDetails<bool>(flagKey, defaultValue, reason: result.Reason);
      }

      var value = result.GetValue<bool>();
      var finalValue = value is bool b ? b : defaultValue;
      return new ResolutionDetails<bool>(flagKey, finalValue, reason: result.Reason);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error resolving boolean flag '{FlagKey}'", flagKey);
      return new ResolutionDetails<bool>(flagKey, defaultValue, reason: ex.Message);
    }
  }

  public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default) {
    try {
      var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

      if (!result.IsEnabled) {
        return new ResolutionDetails<string>(flagKey, defaultValue, reason: result.Reason);
      }

      var value = result.GetValue<string>() ?? defaultValue;
      return new ResolutionDetails<string>(flagKey, value, reason: result.Reason);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error resolving string flag '{FlagKey}'", flagKey);
      return new ResolutionDetails<string>(flagKey, defaultValue, reason: ex.Message);
    }
  }

  public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default) {
    try {
      var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

      if (!result.IsEnabled) {
        return new ResolutionDetails<int>(flagKey, defaultValue, reason: result.Reason);
      }

      var value = result.GetValue<int>();
      var finalValue = value is int i ? i : defaultValue;
      return new ResolutionDetails<int>(flagKey, finalValue, reason: result.Reason);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error resolving integer flag '{FlagKey}'", flagKey);
      return new ResolutionDetails<int>(flagKey, defaultValue, reason: ex.Message);
    }
  }

  public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default) {
    try {
      var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

      if (!result.IsEnabled) {
        return new ResolutionDetails<double>(flagKey, defaultValue, reason: result.Reason);
      }

      var value = result.GetValue<double>();
      var finalValue = value is double d ? d : defaultValue;
      return new ResolutionDetails<double>(flagKey, finalValue, reason: result.Reason);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error resolving double flag '{FlagKey}'", flagKey);
      return new ResolutionDetails<double>(flagKey, defaultValue, reason: ex.Message);
    }
  }

  public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default) {
    // For now, return the default value as we don't support complex structures
    return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue, reason: "Structure values not supported"));
  }

  private async Task<FeatureEvaluationResult> EvaluateFeatureFlagAsync(string featureKey, EvaluationContext? openFeatureContext, CancellationToken cancellationToken) {
    // Convert OpenFeature context to our FeatureContext
    var context = ConvertFromOpenFeatureContext(openFeatureContext);

    // Get feature flag from database (with caching)
    var featureFlag = await GetFeatureFlagByKeyAsync(featureKey, cancellationToken);

    if (featureFlag == null) {
      _logger.LogWarning("Feature flag '{FeatureKey}' not found", featureKey);
      return new FeatureEvaluationResult {
        FeatureKey = featureKey,
        IsEnabled = false,
        Reason = "Feature flag not found"
      };
    }

    // Evaluate the flag using our business logic
    return await EvaluateFeatureFlagLogic(featureFlag, context, cancellationToken);
  }

  private async Task<FeatureFlag?> GetFeatureFlagByKeyAsync(string key, CancellationToken cancellationToken) {
    // Try cache first
    await RefreshCacheIfNeeded(cancellationToken);

    if (_cache.TryGetValue(key, out var cached)) {
      return cached;
    }

    // Fallback to database
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var featureFlag = await context.FeatureFlags
      .Include(f => f.Targets)
      .FirstOrDefaultAsync(f => f.Key == key, cancellationToken);

    if (featureFlag != null) {
      _cache[key] = featureFlag;
    }

    return featureFlag;
  }

  private async Task RefreshCacheIfNeeded(CancellationToken cancellationToken) {
    if (DateTime.UtcNow - _lastCacheUpdate > TimeSpan.FromMinutes(5)) {
      await RefreshCache(cancellationToken);
    }
  }

  private async Task RefreshCache(CancellationToken cancellationToken) {
    try {
      using var scope = _serviceProvider.CreateScope();
      var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var flags = await context.FeatureFlags
        .Include(f => f.Targets)
        .ToListAsync(cancellationToken);

      _cache.Clear();
      foreach (var flag in flags) {
        _cache[flag.Key] = flag;
      }

      _lastCacheUpdate = DateTime.UtcNow;
      _logger.LogDebug("Refreshed feature flag cache with {Count} flags", flags.Count);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error refreshing feature flag cache");
    }
  }

  private static FeatureContext ConvertFromOpenFeatureContext(EvaluationContext? openFeatureContext) {
    var context = new FeatureContext();

    if (openFeatureContext == null) return context;

    // Extract values from OpenFeature context
    if (openFeatureContext.TryGetValue("userId", out var userId) && userId.IsString) {
      if (Guid.TryParse(userId.AsString, out var userGuid)) {
        context.UserId = userGuid;
      }
    }

    if (openFeatureContext.TryGetValue("tenantId", out var tenantId) && tenantId.IsString) {
      if (Guid.TryParse(tenantId.AsString, out var tenantGuid)) {
        context.TenantId = tenantGuid;
      }
    }

    if (openFeatureContext.TryGetValue("environment", out var environment) && environment.IsString) {
      context.Environment = environment.AsString ?? "production";
    }

    if (openFeatureContext.TryGetValue("ipAddress", out var ipAddress) && ipAddress.IsString) {
      context.IpAddress = ipAddress.AsString;
    }

    if (openFeatureContext.TryGetValue("userAgent", out var userAgent) && userAgent.IsString) {
      context.UserAgent = userAgent.AsString;
    }

    if (openFeatureContext.TryGetValue("userRoles", out var userRoles) && userRoles.IsString) {
      var roles = userRoles.AsString?.Split(',') ?? Array.Empty<string>();
      context.UserRoles = roles.ToList();
    }

    // Add any custom attributes
    foreach (var kvp in openFeatureContext) {
      if (!new[] { "userId", "tenantId", "environment", "ipAddress", "userAgent", "userRoles" }.Contains(kvp.Key)) {
        context.CustomAttributes[kvp.Key] = kvp.Value.AsObject;
      }
    }

    return context;
  }

  private async Task<FeatureEvaluationResult> EvaluateFeatureFlagLogic(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken) {
    // Check environment
    if (!string.IsNullOrEmpty(featureFlag.Environment) && featureFlag.Environment != context.Environment) {
      return new FeatureEvaluationResult {
        FeatureKey = featureFlag.Key,
        IsEnabled = false,
        Reason = $"Environment mismatch: expected {featureFlag.Environment}, got {context.Environment}"
      };
    }

    // Check tenant-specific flags
    if (!featureFlag.IsGlobal && featureFlag.TenantId.HasValue && featureFlag.TenantId != context.TenantId) {
      return new FeatureEvaluationResult {
        FeatureKey = featureFlag.Key,
        IsEnabled = false,
        Reason = "Tenant mismatch"
      };
    }

    // Evaluate targeting rules
    var targetingResult = await EvaluateTargetingRulesAsync(featureFlag, context, cancellationToken);
    if (targetingResult != null) {
      await RecordUsageAsync(featureFlag, context, targetingResult.IsEnabled, targetingResult.Value, targetingResult.Reason, cancellationToken);
      return targetingResult;
    }

    // Check percentage rollout
    if (featureFlag.RolloutPercentage < 100) {
      var userHash = GetUserHash(context.UserId?.ToString() ?? context.IpAddress ?? "anonymous", featureFlag.Key);
      var isInRollout = userHash % 100 < featureFlag.RolloutPercentage;

      if (!isInRollout) {
        var result = new FeatureEvaluationResult {
          FeatureKey = featureFlag.Key,
          IsEnabled = false,
          Value = GetTypedValue(featureFlag.DefaultValue, featureFlag.Type),
          Reason = $"User not in rollout percentage ({featureFlag.RolloutPercentage}%)"
        };

        await RecordUsageAsync(featureFlag, context, result.IsEnabled, result.Value, result.Reason, cancellationToken);
        return result;
      }
    }

    // Flag is enabled
    var enabledResult = new FeatureEvaluationResult {
      FeatureKey = featureFlag.Key,
      IsEnabled = featureFlag.IsEnabled,
      Value = GetTypedValue(featureFlag.EnabledValue ?? featureFlag.DefaultValue, featureFlag.Type),
      Reason = featureFlag.IsEnabled ? "Flag enabled" : "Flag disabled"
    };

    await RecordUsageAsync(featureFlag, context, enabledResult.IsEnabled, enabledResult.Value, enabledResult.Reason, cancellationToken);
    return enabledResult;
  }

  private async Task<FeatureEvaluationResult?> EvaluateTargetingRulesAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken) {
    if (!featureFlag.Targets.Any()) return null;

    foreach (var target in featureFlag.Targets.OrderBy(t => t.CreatedAt)) {
      var matches = target.TargetType.ToLowerInvariant() switch {
        "user" => context.UserId?.ToString() == target.TargetIdentifier,
        "tenant" => context.TenantId?.ToString() == target.TargetIdentifier,
        "role" => context.UserRoles.Contains(target.TargetIdentifier),
        "ip" => context.IpAddress == target.TargetIdentifier,
        _ => false
      };

      if (matches) {
        return new FeatureEvaluationResult {
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

  private async Task RecordUsageAsync(FeatureFlag featureFlag, FeatureContext context, bool isEnabled, object? value, string reason, CancellationToken cancellationToken) {
    try {
      using var scope = _serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      var usage = new FeatureFlagUsage {
        FeatureFlagId = featureFlag.Id,
        UserId = context.UserId,
        TenantId = context.TenantId,
        WasEnabled = isEnabled,
        ReturnedValue = value?.ToString(),
        Environment = context.Environment,
        Reason = reason,
        ContextData = JsonSerializer.Serialize(new {
          context.UserRoles,
          context.CustomAttributes,
          context.IpAddress,
          context.UserAgent
        })
      };

      dbContext.FeatureFlagUsage.Add(usage);
      await dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error recording feature flag usage for '{FeatureKey}'", featureFlag.Key);
    }
  }

  private static object? GetTypedValue(string? value, FeatureFlagType type) {
    if (string.IsNullOrEmpty(value)) return null;

    try {
      return type switch {
        FeatureFlagType.Toggle => bool.Parse(value),
        FeatureFlagType.Numeric => double.Parse(value),
        FeatureFlagType.String => value,
        FeatureFlagType.Percentage => double.Parse(value),
        FeatureFlagType.UserSegment => value,
        _ => value,
      };
    }
    catch {
      return value;
    }
  }

  private static int GetUserHash(string identifier, string featureKey) {
    var combined = $"{identifier}:{featureKey}";
    return Math.Abs(combined.GetHashCode());
  }
}
