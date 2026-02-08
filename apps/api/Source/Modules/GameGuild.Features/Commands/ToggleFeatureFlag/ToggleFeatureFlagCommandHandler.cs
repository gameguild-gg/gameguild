using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handler for ToggleFeatureFlagCommand
/// </summary>
#pragma warning disable CS0618 // IFeatureFlagRepository migration pending
public sealed class ToggleFeatureFlagCommandHandler(
    IFeatureFlagRepository repository,
    ILogger<ToggleFeatureFlagCommandHandler> logger
) : IRequestHandler<ToggleFeatureFlagCommand>
{
    public async Task<Unit> Handle(ToggleFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Toggling feature flag {Id} to {State}", request.Id, request.IsEnabled);

        var flag = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
                   ?? throw new KeyNotFoundException($"Feature flag '{request.Id}' not found");

        flag.IsEnabled = request.IsEnabled;
        await repository.UpdateAsync(flag, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Feature flag {Id} toggled to {State}", request.Id, request.IsEnabled);

        return Unit.Value;
    }
}
#pragma warning restore CS0618
