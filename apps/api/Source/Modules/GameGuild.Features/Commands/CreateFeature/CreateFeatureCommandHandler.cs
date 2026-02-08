using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handler for CreateFeatureCommand
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete - IFeatureFlagRepository migration pending
public sealed class CreateFeatureCommandHandler(IFeatureFlagRepository repository, ILogger<CreateFeatureCommandHandler> logger) : ICommandHandler<CreateFeatureCommand, Guid>
{
    private readonly ILogger<CreateFeatureCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IFeatureFlagRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
#pragma warning restore CS0618

    public async Task<Guid> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating feature with key: {Key}", request.Key);

        var featureFlag = new FeatureFlag
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            IsEnabled = false,
            Type = FeatureFlagType.Toggle,
            Environment = FeatureFlagConstants.DefaultEnvironment
        };

        var created = await _repository.AddAsync(featureFlag, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Feature created successfully with ID: {FeatureId}", created.Id);

        return created.Id;
    }
}
