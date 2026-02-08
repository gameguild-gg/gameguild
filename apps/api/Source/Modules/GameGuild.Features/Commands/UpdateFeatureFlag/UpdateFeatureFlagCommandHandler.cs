using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handler for UpdateFeatureFlagCommand
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete - IFeatureFlagRepository migration pending
public sealed class UpdateFeatureFlagCommandHandler(IFeatureFlagRepository repository, ILogger<UpdateFeatureFlagCommandHandler> logger) : ICommandHandler<UpdateFeatureFlagCommand, bool>
{
    private readonly ILogger<UpdateFeatureFlagCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IFeatureFlagRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
#pragma warning restore CS0618

    public async Task<bool> Handle(UpdateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating feature flag with ID: {FeatureFlagId}", request.Id);

        var flag = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (flag == null)
        {
            _logger.LogWarning("Feature flag {Id} not found for update", request.Id);
            return false;
        }

        if (request.Name is not null) flag.Name = request.Name;
        if (request.Description is not null) flag.Description = request.Description;
        if (request.IsEnabled.HasValue) flag.IsEnabled = request.IsEnabled.Value;
        if (request.RolloutPercentage.HasValue) flag.RolloutPercentage = request.RolloutPercentage.Value;
        if (request.EnabledValue is not null) flag.EnabledValue = request.EnabledValue;
        if (request.DefaultValue is not null) flag.DefaultValue = request.DefaultValue;

        await _repository.UpdateAsync(flag, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Feature flag updated successfully: {FeatureFlagId}", request.Id);

        return true;
    }
}
