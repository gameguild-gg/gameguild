using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Modules.Features.Abstractions;
using GameGuild.Modules.Features.Entities;
using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Services;

/// <summary>
///     Advanced feature flag service with targeting, analytics, and SDK capabilities
/// </summary>
public class AdvancedFeatureFlagService : IAdvancedFeatureFlagService {
    private readonly Dictionary<string, FeatureFlagConfig> _cache = new Dictionary<string, FeatureFlagConfig>();

    private readonly DateTime _lastCacheUpdate = DateTime.UtcNow;

    private readonly ILogger<AdvancedFeatureFlagService> _logger;

    private readonly IFeatureFlagRepository _repository;

    public AdvancedFeatureFlagService(
        IFeatureFlagRepository repository,
        ILogger<AdvancedFeatureFlagService> logger) {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    ///     Evaluates a feature flag with advanced targeting and analytics
    /// </summary>
    public async Task<FeatureEvaluationResult> EvaluateFeatureAsync(
        string featureKey,
        FeatureContext context,
        CancellationToken cancellationToken = default) {
        DateTime startTime = DateTime.UtcNow;

        try {
            // Get feature flag configuration
            FeatureFlag? featureFlag = await GetFeatureFlagByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null) {
                _logger.LogWarning("Feature flag '{FeatureKey}' not found", featureKey);

                return new FeatureEvaluationResult {
                    FeatureKey = featureKey,
                    IsEnabled = false,
                    Reason = "Feature flag not found"
                };
            }

            // Check environment
            if (!string.IsNullOrEmpty(featureFlag.Environment) &&
                featureFlag.Environment != context.Environment) {
                return new FeatureEvaluationResult {
                    FeatureKey = featureKey,
                    IsEnabled = false,
                    Reason = $"Environment mismatch: expected {featureFlag.Environment}, got {context.Environment}"
                };
            }

            // Evaluate targeting rules
            FeatureEvaluationResult? targetingResult = await EvaluateTargetingRulesAsync(featureFlag, context, cancellationToken).ConfigureAwait(false);

            if (targetingResult != null) {
                // Record usage analytics
                await RecordUsageAsync(featureFlag.Id, context, targetingResult.IsEnabled, targetingResult.Value, cancellationToken).ConfigureAwait(false);

                return targetingResult;
            }

            // Global feature flag evaluation
            bool isEnabled = featureFlag.IsEnabled;

            // Apply percentage rollout
            if (isEnabled && featureFlag.RolloutPercentage < 100) {
                isEnabled = IsUserInRollout(context, featureFlag.RolloutPercentage);
            }

            string? value = isEnabled ? featureFlag.EnabledValue : featureFlag.DefaultValue;

            var result = new FeatureEvaluationResult {
                FeatureKey = featureKey,
                IsEnabled = isEnabled,
                Value = value,
                Reason = isEnabled ? "Global feature enabled" : "Global feature disabled",
                RolloutPercentage = featureFlag.RolloutPercentage,
                EvaluatedAt = startTime
            };

            // Record usage analytics
            await RecordUsageAsync(featureFlag.Id, context, isEnabled, value, cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error evaluating feature flag '{FeatureKey}'", featureKey);

            return new FeatureEvaluationResult {
                FeatureKey = featureKey,
                IsEnabled = false,
                Reason = $"Evaluation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     Evaluates multiple feature flags in bulk
    /// </summary>
    public async Task<BulkEvaluationResponse> EvaluateFeaturesAsync(
        BulkEvaluationRequest request,
        CancellationToken cancellationToken = default) {
        var response = new BulkEvaluationResponse {
            Environment = request.Context.Environment
        };

        var tasks = request.FeatureKeys.Select(async key => {
            FeatureEvaluationResult result = await EvaluateFeatureAsync(key, request.Context, cancellationToken).ConfigureAwait(false);

            return new {
                Key = key,
                Result = result
            };
        }
        );

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var result in results) {
            response.Results[result.Key] = result.Result;
        }

        return response;
    }

    /// <summary>
    ///     Gets feature flag configuration for SDK
    /// </summary>
    public async Task<FeatureFlagConfig?> GetFeatureFlagConfigAsync(
        string featureKey,
        CancellationToken cancellationToken = default) {
        FeatureFlag? featureFlag = await GetFeatureFlagByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false);

        if (featureFlag == null)
            return null;

        return new FeatureFlagConfig {
            Key = featureFlag.Key,
            Name = featureFlag.Name,
            Description = featureFlag.Description,
            IsEnabled = featureFlag.IsEnabled,
            Type = featureFlag.Type,
            DefaultValue = featureFlag.DefaultValue,
            EnabledValue = featureFlag.EnabledValue,
            IsGlobal = featureFlag.IsGlobal,
            RolloutPercentage = featureFlag.RolloutPercentage,
            Environment = featureFlag.Environment,
            LastModified = featureFlag.UpdatedAt
        };
    }

    /// <summary>
    ///     Gets all feature flag configurations for SDK
    /// </summary>
    public async Task<IEnumerable<FeatureFlagConfig>> GetAllFeatureFlagConfigsAsync(
        string environment = "production",
        CancellationToken cancellationToken = default) {
        var featureFlags = await _repository.GetByEnvironmentAsync(environment, cancellationToken);

        return featureFlags.Select(ff => new FeatureFlagConfig {
            Key = ff.Key,
            Name = ff.Name,
            Description = ff.Description,
            IsEnabled = ff.IsEnabled,
            Type = ff.Type,
            DefaultValue = ff.DefaultValue,
            EnabledValue = ff.EnabledValue,
            IsGlobal = ff.IsGlobal,
            RolloutPercentage = ff.RolloutPercentage,
            Environment = ff.Environment,
            LastModified = ff.UpdatedAt
        }
        );
    }

    /// <summary>
    ///     Creates or updates a targeting rule
    /// </summary>
    public async Task<Guid> CreateTargetingRuleAsync(
        FeatureFlagTargetingRequest request,
        CancellationToken cancellationToken = default) {
        var target = new FeatureFlagTarget {
            FeatureFlagId = request.FeatureFlagId,
            TargetType = request.TargetType,
            TargetIdentifier = request.TargetIdentifier,
            IsEnabled = request.IsEnabled,
            RolloutPercentage = request.RolloutPercentage,
            CustomValue = request.CustomValue,
            Priority = request.Priority,
            Metadata = JsonSerializer.Serialize(request.Metadata)
        };

        return await _repository.CreateTargetAsync(target, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets analytics for a feature flag
    /// </summary>
    public async Task<FeatureFlagAnalytics> GetAnalyticsAsync(
        string featureKey,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default) {
        DateTime start = startDate ?? DateTime.UtcNow.AddDays(-30);
        DateTime end = endDate ?? DateTime.UtcNow;

        var usage = await _repository.GetUsageAnalyticsAsync(featureKey, start, end, cancellationToken).ConfigureAwait(false);

        var totalAccesses = usage.Sum(u => u.AccessCount);
        var enabledAccesses = usage.Where(u => u.WasEnabled).Sum(u => u.AccessCount);

        return new FeatureFlagAnalytics {
            FeatureKey = featureKey,
            TotalAccesses = totalAccesses,
            EnabledAccesses = enabledAccesses,
            DisabledAccesses = totalAccesses - enabledAccesses,
            EnabledPercentage = totalAccesses > 0 ? (double)enabledAccesses / totalAccesses * 100 : 0,
            FirstAccess = usage.Any() ? usage.Min(u => u.FirstAccessAt) : DateTime.MinValue,
            LastAccess = usage.Any() ? usage.Max(u => u.LastAccessAt) : DateTime.MinValue,
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    #region Private Methods

    private async Task<FeatureFlag?> GetFeatureFlagByKeyAsync(string key, CancellationToken cancellationToken) {
        // Try cache first (in a real implementation, use proper caching like Redis)
        if (_cache.TryGetValue(key, out FeatureFlagConfig? cachedConfig) &&
            (DateTime.UtcNow - _lastCacheUpdate).TotalMinutes < 5) {
            // Convert back to FeatureFlag for consistency
            // In a real implementation, cache the actual FeatureFlag entity
        }

        return await _repository.GetByKeyAsync(key, cancellationToken);
    }

    private async Task<FeatureEvaluationResult?> EvaluateTargetingRulesAsync(
        FeatureFlag featureFlag,
        FeatureContext context,
        CancellationToken cancellationToken) {
        if (!featureFlag.Targets.Any())
            return null;

        // Sort by priority (highest first)
        var sortedTargets = featureFlag.Targets.OrderByDescending(t => t.Priority);

        foreach (FeatureFlagTarget target in sortedTargets) {
            if (await MatchesTargetingRuleAsync(target, context, cancellationToken).ConfigureAwait(false)) {
                bool isEnabled = target.IsEnabled;

                // Apply percentage rollout for this target
                if (isEnabled && target.RolloutPercentage < 100) {
                    isEnabled = IsUserInRollout(context, target.RolloutPercentage, target.TargetIdentifier);
                }

                string? value = isEnabled ? target.CustomValue ?? featureFlag.EnabledValue : featureFlag.DefaultValue;

                return new FeatureEvaluationResult {
                    FeatureKey = featureFlag.Key,
                    IsEnabled = isEnabled,
                    Value = value,
                    Reason = $"Targeted rule matched: {target.TargetType}={target.TargetIdentifier}",
                    RolloutPercentage = target.RolloutPercentage,
                    IsTargeted = true,
                    TargetType = target.TargetType
                };
            }
        }

        return null;
    }

    private async Task<bool> MatchesTargetingRuleAsync(
        FeatureFlagTarget target,
        FeatureContext context,
        CancellationToken cancellationToken) {
        return target.TargetType.ToLowerInvariant() switch {
            "tenant" => context.TenantId?.ToString() == target.TargetIdentifier,
            "user" => context.UserId?.ToString() == target.TargetIdentifier,
            "plan" => context.SubscriptionPlanId == target.TargetIdentifier,
            "country" => context.Country == target.TargetIdentifier,
            "environment" => context.Environment == target.TargetIdentifier,
            _ => await EvaluateCustomTargetingRuleAsync(target, context, cancellationToken).ConfigureAwait(false)
        };
    }

    private async Task<bool> EvaluateCustomTargetingRuleAsync(
        FeatureFlagTarget target,
        FeatureContext context,
        CancellationToken cancellationToken) {
        // Custom targeting rule evaluation
        // Could be extended to support complex conditions, regex, etc.
        await Task.CompletedTask; // Placeholder for async operations

        if (!string.IsNullOrEmpty(target.Metadata)) {
            try {
                var conditions = JsonSerializer.Deserialize<Dictionary<string, object>>(target.Metadata);

                // Evaluate custom conditions here
                return false;
            }
            catch {
                return false;
            }
        }

        return false;
    }

    private static bool IsUserInRollout(FeatureContext context, int percentage, string? salt = null) {
        if (percentage >= 100) return true;
        if (percentage <= 0) return false;

        // Use deterministic hash to ensure consistent rollout
        string identifier = context.TenantId?.ToString() ?? context.UserId?.ToString() ?? context.IpAddress ?? "anonymous";
        var hashInput = $"{identifier}:{salt ?? "default"}";

        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        uint bucketValue = hashValue % 100;

        return bucketValue < percentage;
    }

    private async Task RecordUsageAsync(
        Guid featureFlagId,
        FeatureContext context,
        bool wasEnabled,
        string? returnedValue,
        CancellationToken cancellationToken) {
        try {
            var usage = new FeatureFlagUsage {
                FeatureFlagId = featureFlagId,
                TenantId = context.TenantId,
                UserId = context.UserId,
                Environment = context.Environment,
                WasEnabled = wasEnabled,
                ReturnedValue = returnedValue,
                FirstAccessAt = DateTime.UtcNow,
                LastAccessAt = DateTime.UtcNow,
                ContextData = JsonSerializer.Serialize(
                    new {
                        context.UserAgent,
                        context.IpAddress,
                        context.Country,
                        context.RequestTime
                    }
                )
            };

            await _repository.RecordUsageAsync(usage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to record feature flag usage for {FeatureFlagId}", featureFlagId);
        }
    }

    #endregion
}

