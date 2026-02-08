using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handler for CreateFeatureFlagCommand
/// </summary>
#pragma warning disable CS0618 // IFeatureFlagRepository migration pending
public sealed class CreateFeatureFlagCommandHandler(
    IFeatureFlagRepository repository,
    ILogger<CreateFeatureFlagCommandHandler> logger
) : IRequestHandler<CreateFeatureFlagCommand, Guid>
{
    public async Task<Guid> Handle(CreateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating feature flag with key: {Key}", request.Key);

        var featureFlag = new FeatureFlag
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            IsEnabled = request.IsEnabled,
            Type = FeatureFlagType.Toggle,
            Environment = FeatureFlagConstants.DefaultEnvironment
        };

        var created = await repository.AddAsync(featureFlag, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Feature flag created with ID: {Id}", created.Id);

        return created.Id;
    }
}
#pragma warning restore CS0618
