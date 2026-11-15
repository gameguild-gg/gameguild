using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
/// Handler for ToggleFeatureFlagCommand
/// </summary>
public sealed class ToggleFeatureFlagCommandHandler(
    IFeatureFlagQueryRepository repository,
    ILogger<ToggleFeatureFlagCommandHandler> logger,
    IPublisher? publisher = null
) : ICommandHandler<ToggleFeatureFlagCommand>
{
    public async Task<Unit> Handle(ToggleFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Toggling feature flag {Id} to {State}", request.Id, request.IsEnabled);

        var flag = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
                   ?? throw new KeyNotFoundException($"Feature flag '{request.Id}' not found");

        flag.IsEnabled = request.IsEnabled;
        await repository.UpdateAsync(flag, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Feature flag {Id} toggled to {State}", request.Id, request.IsEnabled);

        // Issue #1184 G4: FeatureFlagToggledEvent was declared but never raised; the toggle
        // command is its producing site (cache invalidation/audit consumers react to flips).
        // FeatureFlag rows are global (no per-tenant column) — the event carries a null tenant.
        if (publisher is not null)
        {
            await publisher.Publish(
                new FeatureFlagToggledEvent(request.Id, flag.Key, request.IsEnabled, null),
                cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
