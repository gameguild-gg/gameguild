using System.Text.Json;



using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Features;

/// <summary>
///     Service for managing feature flags (CRUD operations and targeting rules).
///     Implements IFeatureFlagManagementService following the Interface Segregation Principle.
/// </summary>
public class FeatureFlagManagementService(
    IFeatureFlagQueryRepository queryRepository,
    IFeatureFlagTargetingRepository targetingRepository,
    ILogger<FeatureFlagManagementService> logger,
    IOptions<FeatureFlagOptions> options
) : IFeatureFlagManagementService
{
    private readonly ILogger<FeatureFlagManagementService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

#pragma warning disable IDE0052 // Remove unread private member - Options validated but reserved for future configuration needs
    private readonly FeatureFlagOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));
#pragma warning restore IDE0052

    private readonly IFeatureFlagQueryRepository _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));

    private readonly IFeatureFlagTargetingRepository _targetingRepository = targetingRepository ?? throw new ArgumentNullException(nameof(targetingRepository));

    /// <inheritdoc />
    public async Task<Guid> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        ValidateRolloutPercentage(request.RolloutPercentage);

        try
        {
            // Check if feature already exists
            var existing = await _queryRepository.GetByKeyAsync(request.Key, cancellationToken).ConfigureAwait(false);

            if (existing != null) { throw new InvalidOperationException($"Feature flag with key '{request.Key}' already exists"); }

            var featureFlag = new FeatureFlag
            {
                Key = request.Key,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Type = request.Type,
                DefaultValue = request.DefaultValue,
                EnabledValue = request.DefaultValue, // Start with default
                IsEnabled = request.IsEnabled,
                RolloutPercentage = request.RolloutPercentage,
                Environment = request.Environment,
                IsGlobal = true // Default to global
            };

            var id = await _queryRepository.AddAsync(featureFlag, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created feature flag '{FeatureKey}' with ID {FeatureFlagId}", request.Key, id.Id);

            return id.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature flag '{FeatureKey}'", request.Key);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateFeatureFlagAsync(Guid featureFlagId, UpdateFeatureFlagRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RolloutPercentage.HasValue) { ValidateRolloutPercentage(request.RolloutPercentage.Value); }

        try
        {
            var featureFlag = await _queryRepository.GetByIdAsync(featureFlagId, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null) { throw new InvalidOperationException($"Feature flag with ID '{featureFlagId}' not found"); }

            // Update only provided properties
            if (!string.IsNullOrWhiteSpace(request.Name)) { featureFlag.Name = request.Name; }

            if (request.Description != null) { featureFlag.Description = request.Description; }

            if (request.DefaultValue != null) { featureFlag.DefaultValue = request.DefaultValue; }

            if (request.IsEnabled.HasValue) { featureFlag.IsEnabled = request.IsEnabled.Value; }

            if (request.RolloutPercentage.HasValue) { featureFlag.RolloutPercentage = request.RolloutPercentage.Value; }

            await _queryRepository.UpdateAsync(featureFlag, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated feature flag '{FeatureKey}' (ID: {FeatureFlagId})", featureFlag.Key, featureFlagId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature flag with ID '{FeatureFlagId}'", featureFlagId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteFeatureFlagAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        try
        {
            var featureFlag = await _queryRepository.GetByIdAsync(featureFlagId, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null) { throw new InvalidOperationException($"Feature flag with ID '{featureFlagId}' not found"); }

            // Delete all targeting rules first
            await _targetingRepository.DeleteTargetsByFeatureFlagAsync(featureFlagId, cancellationToken).ConfigureAwait(false);

            // Delete the feature flag
            await _queryRepository.RemoveAsync(featureFlagId, cancellationToken).ConfigureAwait(false);
            await _queryRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleted feature flag '{FeatureKey}' (ID: {FeatureFlagId})", featureFlag.Key, featureFlagId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature flag with ID '{FeatureFlagId}'", featureFlagId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task EnableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default) { await SetFeatureEnabledStateAsync(featureFlagId, true, cancellationToken).ConfigureAwait(false); }

    /// <inheritdoc />
    public async Task DisableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default) { await SetFeatureEnabledStateAsync(featureFlagId, false, cancellationToken).ConfigureAwait(false); }

    /// <inheritdoc />
    public async Task<Guid> CreateTargetingRuleAsync(FeatureFlagTargetingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetIdentifier);

        if (request.RolloutPercentage.HasValue) { ValidateRolloutPercentage(request.RolloutPercentage.Value); }

        try
        {
            // Verify feature flag exists
            var featureFlag = await _queryRepository.GetByIdAsync(request.FeatureFlagId, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null) { throw new InvalidOperationException($"Feature flag with ID '{request.FeatureFlagId}' not found"); }

            var target = new FeatureFlagTarget
            {
                FeatureFlagId = request.FeatureFlagId,
                TargetType = request.TargetType,
                TargetIdentifier = request.TargetIdentifier,
                IsEnabled = request.IsEnabled,
                RolloutPercentage = request.RolloutPercentage ?? 100,
                CustomValue = request.CustomValue,
                Priority = request.Priority,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
            };

            var targetId = await _targetingRepository.CreateTargetAsync(target, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Created targeting rule for feature '{FeatureKey}': {TargetType}={TargetIdentifier}", featureFlag.Key, request.TargetType, request.TargetIdentifier);

            return targetId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating targeting rule for feature flag '{FeatureFlagId}'", request.FeatureFlagId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateTargetingRuleAsync(Guid targetId, FeatureFlagTargetingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RolloutPercentage.HasValue) { ValidateRolloutPercentage(request.RolloutPercentage.Value); }

        try
        {
            var target = await _targetingRepository.GetTargetByIdAsync(targetId, cancellationToken).ConfigureAwait(false);

            if (target == null) { throw new InvalidOperationException($"Targeting rule with ID '{targetId}' not found"); }

            // Update properties
            target.TargetType = request.TargetType ?? target.TargetType;
            target.TargetIdentifier = request.TargetIdentifier ?? target.TargetIdentifier;
            target.IsEnabled = request.IsEnabled;
            target.RolloutPercentage = request.RolloutPercentage ?? target.RolloutPercentage;
            target.CustomValue = request.CustomValue;
            target.Priority = request.Priority;
            target.Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : target.Metadata;

            await _targetingRepository.UpdateTargetAsync(target, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated targeting rule with ID {TargetId}", targetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating targeting rule with ID '{TargetId}'", targetId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteTargetingRuleAsync(Guid targetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var target = await _targetingRepository.GetTargetByIdAsync(targetId, cancellationToken).ConfigureAwait(false);

            if (target == null) { throw new InvalidOperationException($"Targeting rule with ID '{targetId}' not found"); }

            await _targetingRepository.DeleteTargetAsync(targetId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleted targeting rule with ID {TargetId}", targetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting targeting rule with ID '{TargetId}'", targetId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateRolloutPercentageAsync(Guid featureFlagId, int percentage, CancellationToken cancellationToken = default)
    {
        ValidateRolloutPercentage(percentage);

        try
        {
            var featureFlag = await _queryRepository.GetByIdAsync(featureFlagId, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null) { throw new InvalidOperationException($"Feature flag with ID '{featureFlagId}' not found"); }

            featureFlag.RolloutPercentage = percentage;
            await _queryRepository.UpdateAsync(featureFlag, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Updated rollout percentage for feature '{FeatureKey}' to {Percentage}%", featureFlag.Key, percentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rollout percentage for feature flag '{FeatureFlagId}'", featureFlagId);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByIdAsync(Guid featureFlagId, CancellationToken cancellationToken = default)
    {
        try { return await _queryRepository.GetByIdAsync(featureFlagId, cancellationToken).ConfigureAwait(false); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flag with ID '{FeatureFlagId}'", featureFlagId);

            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetByKeyAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);

        try { return await _queryRepository.GetByKeyAsync(featureKey, cancellationToken).ConfigureAwait(false); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flag '{FeatureKey}'", featureKey);

            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureFlag>> GetAllAsync(string? environment = null, bool enabledOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<FeatureFlag> features;

            if (!string.IsNullOrWhiteSpace(environment)) { features = await _queryRepository.GetByEnvironmentAsync(environment, cancellationToken).ConfigureAwait(false); }
            else if (enabledOnly) { features = await _queryRepository.GetEnabledAsync(cancellationToken).ConfigureAwait(false); }
            else { features = await _queryRepository.GetAllAsync(cancellationToken).ConfigureAwait(false); }

            // Apply enabled filter if requested and not already filtered
            if (enabledOnly && !string.IsNullOrWhiteSpace(environment)) { features = features.Where(f => f.IsEnabled); }

            return features.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flags");

            return [];
        }
    }

    #region Private Helper Methods

    private async Task SetFeatureEnabledStateAsync(Guid featureFlagId, bool isEnabled, CancellationToken cancellationToken)
    {
        try
        {
            var featureFlag = await _queryRepository.GetByIdAsync(featureFlagId, cancellationToken).ConfigureAwait(false);

            if (featureFlag == null) { throw new InvalidOperationException($"Feature flag with ID '{featureFlagId}' not found"); }

            featureFlag.IsEnabled = isEnabled;
            await _queryRepository.UpdateAsync(featureFlag, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("{Action} feature flag '{FeatureKey}' (ID: {FeatureFlagId})", isEnabled ? "Enabled" : "Disabled", featureFlag.Key, featureFlagId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error {Action} feature flag with ID '{FeatureFlagId}'", isEnabled ? "enabling" : "disabling", featureFlagId);

            throw;
        }
    }

    private static void ValidateRolloutPercentage(int percentage)
    {
        if (!RolloutHashCalculator.IsValidPercentage(percentage))
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), percentage, $"Rollout percentage must be between {FeatureFlagConstants.MinRolloutPercentage} and {FeatureFlagConstants.MaxRolloutPercentage}");
        }
    }

    #endregion
}
