using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features.Commands.Handlers;

/// <summary>
///     Handler for CreateFeatureCommand
/// </summary>
public sealed class CreateFeatureCommandHandler(IFeatureFlagRepository repository, ILogger<CreateFeatureCommandHandler> logger) : ICommandHandler<CreateFeatureCommand, Guid>
{
    private readonly ILogger<CreateFeatureCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IFeatureFlagRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Guid> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating feature with key: {Key}", request.Key);

        // TODO: Implement actual create logic
        // This is a placeholder - you'll need to adapt this to your actual entity creation
        await Task.CompletedTask;

        var featureId = Guid.NewGuid();

        _logger.LogInformation("Feature created successfully with ID: {FeatureId}", featureId);

        return featureId;
    }
}
