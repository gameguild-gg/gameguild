using GameGuild.Modules.Features.Models;
using OpenFeature;
using OpenFeature.Model;

namespace GameGuild.Modules.Features.Infrastructure;

/// <summary>
/// Infrastructure implementation of feature flag service using OpenFeature.
/// Provides integration between Game Guild's feature flag domain and OpenFeature SDK.
/// </summary>
internal sealed class FeatureFlagOpenFeatureService(FeatureClient featureClient) : IOpenFeatureFlagService
{
    /// <summary>
    /// Gets a boolean feature flag value with optional evaluation context.
    /// </summary>
    /// <param name="key">The feature flag key</param>
    /// <param name="defaultValue">Default value if flag is not found</param>
    /// <param name="context">Evaluation context with user/tenant information</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The evaluated boolean value</returns>
    public async Task<bool> GetBooleanAsync(string key, bool defaultValue = false, FeatureEvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetBooleanValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetBooleanValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a string feature flag value with optional evaluation context.
    /// </summary>
    /// <param name="key">The feature flag key</param>
    /// <param name="defaultValue">Default value if flag is not found</param>
    /// <param name="context">Evaluation context with user/tenant information</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The evaluated string value</returns>
    public async Task<string> GetStringAsync(string key, string defaultValue = "", FeatureEvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetStringValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetStringValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an integer feature flag value with optional evaluation context.
    /// </summary>
    /// <param name="key">The feature flag key</param>
    /// <param name="defaultValue">Default value if flag is not found</param>
    /// <param name="context">Evaluation context with user/tenant information</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The evaluated integer value</returns>
    public async Task<int> GetIntAsync(string key, int defaultValue = 0, FeatureEvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetIntegerValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetIntegerValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a double feature flag value with optional evaluation context.
    /// </summary>
    /// <param name="key">The feature flag key</param>
    /// <param name="defaultValue">Default value if flag is not found</param>
    /// <param name="context">Evaluation context with user/tenant information</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The evaluated double value</returns>
    public async Task<double> GetDoubleAsync(string key, double defaultValue = 0d, FeatureEvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetDoubleValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetDoubleValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts Game Guild's feature evaluation context to OpenFeature's evaluation context.
    /// </summary>
    /// <param name="context">Game Guild evaluation context</param>
    /// <returns>OpenFeature evaluation context</returns>
    private static OpenFeature.Model.EvaluationContext? ConvertToOpenFeatureContext(FeatureEvaluationContext? context)
    {
        if (context == null)
            return null;

        var builder = OpenFeature.Model.EvaluationContext.Builder();

        // Convert our domain context to OpenFeature context
        if (!string.IsNullOrEmpty(context.UserId))
            builder.Set("userId", context.UserId);

        if (!string.IsNullOrEmpty(context.SessionId))
            builder.Set("sessionId", context.SessionId);

        if (!string.IsNullOrEmpty(context.TenantId))
            builder.Set("tenantId", context.TenantId);

        if (!string.IsNullOrEmpty(context.Environment))
            builder.Set("environment", context.Environment);

        if (!string.IsNullOrEmpty(context.Location))
            builder.Set("location", context.Location);

        // Add custom attributes
        foreach (var kvp in context.Attributes)
        {
            // Convert the object to OpenFeature Value
            var value = kvp.Value switch
            {
                string s => new OpenFeature.Model.Value(s),
                int i => new OpenFeature.Model.Value(i),
                long l => new OpenFeature.Model.Value(l),
                double d => new OpenFeature.Model.Value(d),
                bool b => new OpenFeature.Model.Value(b),
                DateTime dt => new OpenFeature.Model.Value(dt.ToString("O")), // ISO 8601 format
                null => new OpenFeature.Model.Value(string.Empty),
                _ => new OpenFeature.Model.Value(kvp.Value.ToString() ?? string.Empty)
            };
            builder.Set(kvp.Key, value);
        }

        return builder.Build();
    }
}

/// <summary>
/// Interface for OpenFeature-based feature flag service.
/// Provides type-safe feature flag evaluation with support for different data types.
/// </summary>
public interface IOpenFeatureFlagService
{
    /// <summary>
    /// Gets a boolean feature flag value.
    /// </summary>
    Task<bool> GetBooleanAsync(string key, bool defaultValue = false, FeatureEvaluationContext? context = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a string feature flag value.
    /// </summary>
    Task<string> GetStringAsync(string key, string defaultValue = "", FeatureEvaluationContext? context = null, CancellationToken ct = default);

    /// <summary>
    /// Gets an integer feature flag value.
    /// </summary>
    Task<int> GetIntAsync(string key, int defaultValue = 0, FeatureEvaluationContext? context = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a double feature flag value.
    /// </summary>
    Task<double> GetDoubleAsync(string key, double defaultValue = 0d, FeatureEvaluationContext? context = null, CancellationToken ct = default);
}
