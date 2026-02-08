using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handler for EnableFeatureFlagCommand
/// </summary>
#pragma warning disable CS0618 // IFeatureFlagRepository migration pending
public sealed class EnableFeatureFlagCommandHandler(
    IFeatureFlagRepository repository,
    ILogger<EnableFeatureFlagCommandHandler> logger
) : IRequestHandler<EnableFeatureFlagCommand>
{
    public async Task<Unit> Handle(EnableFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Enabling feature flag {Id}", request.Id);

        var flag = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false)
                   ?? throw new KeyNotFoundException($"Feature flag '{request.Id}' not found");

        flag.IsEnabled = true;
        await repository.UpdateAsync(flag, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Feature flag {Id} enabled", request.Id);

        return Unit.Value;
    }
}
#pragma warning restore CS0618
