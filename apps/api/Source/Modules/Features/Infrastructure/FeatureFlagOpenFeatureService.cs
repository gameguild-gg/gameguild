using GameGuild.Modules.Features.Models;
using OpenFeature;
using OpenFeature.Model;


namespace GameGuild.Modules.Features.Infrastructure;

/// <summary> Infrastructure implementation of feature flag service using OpenFeature. Provides integration between Game Guild's feature flag domain and OpenFeature SDK. </summary>
internal sealed class FeatureFlagOpenFeatureService(FeatureClient featureClient) : IOpenFeatureFlagService {
  /// <summary> Gets a boolean feature flag value with optional evaluation context. </summary>
  /// <param name="key"> The feature flag key </param>
  /// <param name="defaultValue"> Default value if flag is not found </param>
  /// <param name="context"> Evaluation context with user/tenant information </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> The evaluated boolean value </returns>
  public async Task<bool> GetBooleanAsync(string key, bool defaultValue = false, FeatureContext? context = null, CancellationToken ct = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var openFeatureContext = ConvertToOpenFeatureContext(context);

    return openFeatureContext != null
             ? await featureClient.GetBooleanValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
             : await featureClient.GetBooleanValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
  }

  /// <summary> Gets a string feature flag value with optional evaluation context. </summary>
  /// <param name="key"> The feature flag key </param>
  /// <param name="defaultValue"> Default value if flag is not found </param>
  /// <param name="context"> Evaluation context with user/tenant information </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> The evaluated string value </returns>
  public async Task<string> GetStringAsync(string key, string defaultValue = "", FeatureContext? context = null, CancellationToken ct = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var openFeatureContext = ConvertToOpenFeatureContext(context);

    return openFeatureContext != null
             ? await featureClient.GetStringValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
             : await featureClient.GetStringValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
  }

  /// <summary> Gets an integer feature flag value with optional evaluation context. </summary>
  /// <param name="key"> The feature flag key </param>
  /// <param name="defaultValue"> Default value if flag is not found </param>
  /// <param name="context"> Evaluation context with user/tenant information </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> The evaluated integer value </returns>
  public async Task<int> GetIntAsync(string key, int defaultValue = 0, FeatureContext? context = null, CancellationToken ct = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var openFeatureContext = ConvertToOpenFeatureContext(context);

    return openFeatureContext != null
             ? await featureClient.GetIntegerValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
             : await featureClient.GetIntegerValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
  }

  /// <summary> Gets a double feature flag value with optional evaluation context. </summary>
  /// <param name="key"> The feature flag key </param>
  /// <param name="defaultValue"> Default value if flag is not found </param>
  /// <param name="context"> Evaluation context with user/tenant information </param>
  /// <param name="ct"> Cancellation token </param>
  /// <returns> The evaluated double value </returns>
  public async Task<double> GetDoubleAsync(string key, double defaultValue = 0d, FeatureContext? context = null, CancellationToken ct = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var openFeatureContext = ConvertToOpenFeatureContext(context);

    return openFeatureContext != null
             ? await featureClient.GetDoubleValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
             : await featureClient.GetDoubleValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
  }

  /// <summary> Converts Game Guild's feature evaluation context to OpenFeature's evaluation context. </summary>
  /// <param name="context"> Game Guild evaluation context </param>
  /// <returns> OpenFeature evaluation context </returns>
  private static EvaluationContext? ConvertToOpenFeatureContext(FeatureContext? context) {
    if (context == null) return null;

    var builder = EvaluationContext.Builder();

    // Convert our domain context to OpenFeature context
    if (context.UserId.HasValue) builder.Set("userId", context.UserId.Value.ToString());

    if (context.TenantId.HasValue) builder.Set("tenantId", context.TenantId.Value.ToString());

    if (!string.IsNullOrEmpty(context.Environment)) builder.Set("environment", context.Environment);

    if (!string.IsNullOrEmpty(context.IpAddress)) builder.Set("ipAddress", context.IpAddress);

    if (!string.IsNullOrEmpty(context.UserAgent)) builder.Set("userAgent", context.UserAgent);

    // Add user roles
    if (context.UserRoles.Any()) {
      builder.Set("userRoles", string.Join(",", context.UserRoles));
    }

    // Add custom attributes
    foreach (var kvp in context.CustomAttributes) {
      // Convert the object to OpenFeature Value
      var value = kvp.Value switch {
        string s => new Value(s),
        int i => new Value(i),
        long l => new Value(l),
        double d => new Value(d),
        bool b => new Value(b),
        DateTime dt => new Value(dt.ToString("O")), // ISO 8601 format
        null => new Value(string.Empty),
        _ => new Value(kvp.Value.ToString() ?? string.Empty),
      };
      builder.Set(kvp.Key, value);
    }

    return builder.Build();
  }
}

/// <summary> Interface for OpenFeature-based feature flag service. Provides type-safe feature flag evaluation with support for different data types. </summary>
public interface IOpenFeatureFlagService {
  /// <summary> Gets a boolean feature flag value. </summary>
  Task<bool> GetBooleanAsync(string key, bool defaultValue = false, FeatureContext? context = null, CancellationToken ct = default);

  /// <summary> Gets a string feature flag value. </summary>
  Task<string> GetStringAsync(string key, string defaultValue = "", FeatureContext? context = null, CancellationToken ct = default);

  /// <summary> Gets an integer feature flag value. </summary>
  Task<int> GetIntAsync(string key, int defaultValue = 0, FeatureContext? context = null, CancellationToken ct = default);

  /// <summary> Gets a double feature flag value. </summary>
  Task<double> GetDoubleAsync(string key, double defaultValue = 0d, FeatureContext? context = null, CancellationToken ct = default);
}
