using OpenFeature;
using OpenFeature.Model;
using IOpenFeatureService = GameGuild.Modules.Features.Abstractions.IFeatureFlagService;
using EvaluationContext = GameGuild.Modules.Features.Models.EvaluationContext;

namespace GameGuild.Modules.Features.Infrastructure;

/// <summary>
/// Infrastructure implementation of feature flag service using OpenFeature.
/// </summary>
internal sealed class FeatureFlagOpenFeatureService(FeatureClient featureClient) : IOpenFeatureService
{
    public async Task<bool> GetBooleanAsync(string key, bool defaultValue = false, EvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetBooleanValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetBooleanValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<string> GetStringAsync(string key, string defaultValue = "", EvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetStringValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetStringValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0, EvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetIntegerValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetIntegerValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<double> GetDoubleAsync(string key, double defaultValue = 0d, EvaluationContext? context = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var openFeatureContext = ConvertToOpenFeatureContext(context);

        return openFeatureContext != null
            ? await featureClient.GetDoubleValueAsync(key, defaultValue, openFeatureContext, cancellationToken: ct).ConfigureAwait(false)
            : await featureClient.GetDoubleValueAsync(key, defaultValue, cancellationToken: ct).ConfigureAwait(false);
    }

    private static OpenFeature.Model.EvaluationContext? ConvertToOpenFeatureContext(EvaluationContext? context)
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

        foreach (var kvp in context.Attributes)
        {
            // Convert the object to OpenFeature Value
            var value = kvp.Value switch
            {
                string s => new Value(s),
                int i => new Value(i),
                long l => new Value(l),
                double d => new Value(d),
                bool b => new Value(b),
                DateTime dt => new Value(dt.ToString("O")), // ISO 8601 format
                null => new Value(string.Empty),
                _ => new Value(kvp.Value.ToString() ?? string.Empty)
            };
            builder.Set(kvp.Key, value);
        }

        return builder.Build();
    }

    // IFeatureFlagService interface implementation
    public Task<bool> IsEnabledAsync(string featureKey, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var context = tenantId.HasValue ? new EvaluationContext { ["tenantId"] = tenantId.Value.ToString() } : null;
        return GetBooleanAsync(featureKey, defaultValue: false, context, cancellationToken);
    }

    public async Task<Models.FeatureAccessResult> GetFeatureAccessAsync(string featureKey, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var isEnabled = await IsEnabledAsync(featureKey, tenantId, cancellationToken);
        return new Models.FeatureAccessResult { IsEnabled = isEnabled, FeatureKey = featureKey };
    }

    public Task EnableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("EnableFeatureAsync requires database repository implementation");
    }

    public Task DisableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("DisableFeatureAsync requires database repository implementation");
    }

    public Task<IEnumerable<Entities.FeatureFlag>> GetEnabledFeaturesAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("GetEnabledFeaturesAsync requires database repository implementation");
    }
}

