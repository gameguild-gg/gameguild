using GameGuild.Features.Abstractions;
using GameGuild.Features.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Model;
using OFEvaluationContext = OpenFeature.Model.EvaluationContext;

namespace GameGuild.Features.Provider;

/// <summary>
///     Custom OpenFeature provider that integrates with GameGuild feature flag system
///     This provider delegates evaluation to IFeatureFlagEvaluationService for consistency
/// </summary>
public class DatabaseFeatureFlagProvider(IServiceProvider serviceProvider, ILogger<DatabaseFeatureFlagProvider> logger) : FeatureProvider
{
    private readonly ILogger<DatabaseFeatureFlagProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public override Metadata GetMetadata() { return new Metadata("GameGuild Database Provider"); }

    public override async Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(string flagKey, bool defaultValue, OFEvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

            if (!result.IsEnabled) { return new ResolutionDetails<bool>(flagKey, defaultValue); }

            var boolValue = result.Value switch
            {
                string s when bool.TryParse(s, out var parsed) => parsed,
                _ => defaultValue
            };

            return new ResolutionDetails<bool>(flagKey, boolValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving boolean flag '{FlagKey}'", flagKey);

            return new ResolutionDetails<bool>(flagKey, defaultValue, reason : ex.Message);
        }
    }

    public override async Task<ResolutionDetails<string>> ResolveStringValueAsync(string flagKey, string defaultValue, OFEvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

            if (!result.IsEnabled) { return new ResolutionDetails<string>(flagKey, defaultValue); }

            var stringValue = result.Value ?? defaultValue;

            return new ResolutionDetails<string>(flagKey, stringValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving string flag '{FlagKey}'", flagKey);

            return new ResolutionDetails<string>(flagKey, defaultValue, reason : ex.Message);
        }
    }

    public override async Task<ResolutionDetails<int>> ResolveIntegerValueAsync(string flagKey, int defaultValue, OFEvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

            if (!result.IsEnabled) { return new ResolutionDetails<int>(flagKey, defaultValue); }

            var intValue = result.Value switch
            {
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => defaultValue
            };

            return new ResolutionDetails<int>(flagKey, intValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving integer flag '{FlagKey}'", flagKey);

            return new ResolutionDetails<int>(flagKey, defaultValue, reason : ex.Message);
        }
    }

    public override async Task<ResolutionDetails<double>> ResolveDoubleValueAsync(string flagKey, double defaultValue, OFEvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await EvaluateFeatureFlagAsync(flagKey, context, cancellationToken);

            if (!result.IsEnabled) { return new ResolutionDetails<double>(flagKey, defaultValue); }

            var doubleValue = result.Value switch
            {
                string s when double.TryParse(s, out var parsed) => parsed,
                _ => defaultValue
            };

            return new ResolutionDetails<double>(flagKey, doubleValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving double flag '{FlagKey}'", flagKey);

            return new ResolutionDetails<double>(flagKey, defaultValue, reason : ex.Message);
        }
    }

    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(string flagKey, Value defaultValue, OFEvaluationContext? context = null, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Structure value resolution not supported for flag '{FlagKey}'", flagKey);

        return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue, reason : "Structure values not supported"));
    }

    private async Task<FeatureEvaluationResult> EvaluateFeatureFlagAsync(string featureKey, OFEvaluationContext? openFeatureContext, CancellationToken cancellationToken)
    {
        var context = ConvertFromOpenFeatureContext(openFeatureContext);
        using var scope = _serviceProvider.CreateScope();
        var evaluationService = scope.ServiceProvider.GetRequiredService<IFeatureFlagEvaluationService>();

        return await evaluationService.EvaluateAsync(featureKey, context, cancellationToken);
    }

    private static FeatureContext ConvertFromOpenFeatureContext(OFEvaluationContext? openFeatureContext)
    {
        var context = new FeatureContext { RequestTime = DateTime.UtcNow, CustomAttributes = new Dictionary<string, object>() };

        if (openFeatureContext == null) { return context; }

        if (openFeatureContext.TryGetValue("userId", out var userId) && userId.IsString)
        {
            if (Guid.TryParse(userId.AsString, out var userGuid)) { context.UserId = userGuid; }
        }

        if (openFeatureContext.TryGetValue("tenantId", out var tenantId) && tenantId.IsString)
        {
            if (Guid.TryParse(tenantId.AsString, out var tenantGuid)) { context.TenantId = tenantGuid; }
        }

        if (openFeatureContext.TryGetValue("environment", out var environment) && environment.IsString) { context.Environment = environment.AsString ?? "production"; }

        if (openFeatureContext.TryGetValue("ipAddress", out var ipAddress) && ipAddress.IsString) { context.IpAddress = ipAddress.AsString; }

        if (openFeatureContext.TryGetValue("userAgent", out var userAgent) && userAgent.IsString) { context.UserAgent = userAgent.AsString; }

        if (openFeatureContext.TryGetValue("country", out var country) && country.IsString) { context.Country = country.AsString; }

        if (openFeatureContext.TryGetValue("subscriptionPlanId", out var plan) && plan.IsString) { context.SubscriptionPlanId = plan.AsString; }

        if (openFeatureContext.TryGetValue("permissions", out var permissions) && permissions.IsString)
        {
            var perms = permissions.AsString?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
            context.Permissions = perms.ToList();
        }

        var standardKeys = new HashSet<string> { "userId", "tenantId", "environment", "ipAddress", "userAgent", "country", "subscriptionPlanId", "permissions" };

        foreach (var kvp in openFeatureContext)
        {
            if (!standardKeys.Contains(kvp.Key)) { context.CustomAttributes[kvp.Key] = kvp.Value.AsObject ?? string.Empty; }
        }

        return context;
    }
}
