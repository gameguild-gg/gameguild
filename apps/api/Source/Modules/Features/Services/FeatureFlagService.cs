using GameGuild.Modules.Features.Models;
using OpenFeature;
using OpenFeature.Model;

namespace GameGuild.Modules.Features.Services;

/// <summary>
/// Service for managing and evaluating feature flags using OpenFeature
/// </summary>
public class FeatureFlagService(FeatureClient featureClient) : IFeatureFlagService {
  /// <summary>
  /// Evaluates a feature flag and returns its result
  /// </summary>
  public async Task<FeatureEvaluationResult> EvaluateFeatureAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default) {
    var openFeatureContext = ConvertToOpenFeatureContext(context);

    try {
      // Use OpenFeature to evaluate the flag - it will use our DatabaseFeatureFlagProvider
      var boolResult = await featureClient.GetBooleanValueAsync(featureKey, false, openFeatureContext);

      return new FeatureEvaluationResult {
        FeatureKey = featureKey,
        IsEnabled = boolResult,
        Value = boolResult,
        Reason = "Evaluated via OpenFeature"
      };
    }
    catch (Exception ex) {
      return new FeatureEvaluationResult {
        FeatureKey = featureKey,
        IsEnabled = false,
        Reason = $"Error: {ex.Message}"
      };
    }
  }

  /// <summary>
  /// Gets a boolean feature flag value
  /// </summary>
  public async Task<bool> GetBooleanAsync(string featureKey, bool defaultValue, FeatureContext? context = null, CancellationToken cancellationToken = default) {
    var openFeatureContext = ConvertToOpenFeatureContext(context ?? new FeatureContext());
    return await featureClient.GetBooleanValueAsync(featureKey, defaultValue, openFeatureContext);
  }

  /// <summary>
  /// Gets a string feature flag value
  /// </summary>
  public async Task<string> GetStringAsync(string featureKey, string defaultValue, FeatureContext? context = null, CancellationToken cancellationToken = default) {
    var openFeatureContext = ConvertToOpenFeatureContext(context ?? new FeatureContext());
    return await featureClient.GetStringValueAsync(featureKey, defaultValue, openFeatureContext);
  }

  /// <summary>
  /// Gets an integer feature flag value
  /// </summary>
  public async Task<int> GetIntAsync(string featureKey, int defaultValue, FeatureContext? context = null, CancellationToken cancellationToken = default) {
    var openFeatureContext = ConvertToOpenFeatureContext(context ?? new FeatureContext());
    return await featureClient.GetIntegerValueAsync(featureKey, defaultValue, openFeatureContext);
  }

  /// <summary>
  /// Gets a double feature flag value
  /// </summary>
  public async Task<double> GetDoubleAsync(string featureKey, double defaultValue, FeatureContext? context = null, CancellationToken cancellationToken = default) {
    var openFeatureContext = ConvertToOpenFeatureContext(context ?? new FeatureContext());
    return await featureClient.GetDoubleValueAsync(featureKey, defaultValue, openFeatureContext);
  }

  /// <summary>
  /// Converts our FeatureContext to OpenFeature EvaluationContext
  /// </summary>
  private static EvaluationContext ConvertToOpenFeatureContext(FeatureContext context) {
    var builder = EvaluationContext.Builder();

    if (context.UserId.HasValue) {
      builder.Set("userId", context.UserId.Value.ToString());
    }

    if (context.TenantId.HasValue) {
      builder.Set("tenantId", context.TenantId.Value.ToString());
    }

    if (!string.IsNullOrEmpty(context.Environment)) {
      builder.Set("environment", context.Environment);
    }

    if (!string.IsNullOrEmpty(context.IpAddress)) {
      builder.Set("ipAddress", context.IpAddress);
    }

    if (!string.IsNullOrEmpty(context.UserAgent)) {
      builder.Set("userAgent", context.UserAgent);
    }

    if (context.UserRoles.Any()) {
      builder.Set("userRoles", string.Join(",", context.UserRoles));
    }

    // Add custom attributes as strings for now
    foreach (var kvp in context.CustomAttributes) {
      builder.Set(kvp.Key, kvp.Value?.ToString() ?? "");
    }

    return builder.Build();
  }

  // TODO: Implement these methods for CRUD operations on feature flags
  public Task<FeatureFlag> CreateFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("CRUD operations will be implemented in a separate service");
  }

  public Task<FeatureFlag?> UpdateFeatureFlagAsync(Guid id, FeatureFlag featureFlag, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("CRUD operations will be implemented in a separate service");
  }

  public Task<bool> DeleteFeatureFlagAsync(Guid id, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("CRUD operations will be implemented in a separate service");
  }

  public Task<FeatureFlag?> GetFeatureFlagByIdAsync(Guid id, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("CRUD operations will be implemented in a separate service");
  }

  public Task<FeatureFlag?> GetFeatureFlagByKeyAsync(string key, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("CRUD operations will be implemented in a separate service");
  }

  public Task<IEnumerable<FeatureFlag>> GetFeatureFlagsAsync(Guid? tenantId = null, string? environment = null, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("CRUD operations will be implemented in a separate service");
  }

  public Task<IEnumerable<FeatureFlagUsage>> GetUsageAnalyticsAsync(Guid featureFlagId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) {
    throw new NotImplementedException("Analytics will be implemented in a separate service");
  }
}
