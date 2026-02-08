using System.Text.Json;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handler for adding a targeting rule to a feature flag
/// </summary>
public sealed class AddTargetingRuleCommandHandler(IFeatureFlagTargetingRepository targetingRepository, ILogger<AddTargetingRuleCommandHandler> logger) : ICommandHandler<AddTargetingRuleCommand, Guid>
{
    private readonly ILogger<AddTargetingRuleCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IFeatureFlagTargetingRepository _targetingRepository = targetingRepository ?? throw new ArgumentNullException(nameof(targetingRepository));

    public async Task<Guid> Handle(AddTargetingRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding targeting rule of type {TargetType} to feature flag {FeatureFlagId}", request.TargetType, request.FeatureFlagId);

        // Create the targeting rule entity
        var targetingRule = new FeatureFlagTarget
        {
            Id = Guid.NewGuid(),
            FeatureFlagId = request.FeatureFlagId,
            TargetType = request.TargetType,
            TargetIdentifier = request.TargetIdentifier,
            IsEnabled = request.IsEnabled,
            RolloutPercentage = request.RolloutPercentage,
            CustomValue = request.CustomValue,
            Priority = request.Priority,
            Metadata = request.Metadata.Count > 0 ? JsonSerializer.Serialize(request.Metadata) : null
        };

        // Add the targeting rule
        var targetId = await _targetingRepository.CreateTargetAsync(targetingRule, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully added targeting rule {TargetingRuleId} to feature flag {FeatureFlagId}", targetId, request.FeatureFlagId);

        return targetId;
    }
}
