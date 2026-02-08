using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for FeatureFlagExistsQuery
/// </summary>
public sealed class FeatureFlagExistsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<FeatureFlagExistsQuery, bool>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<bool> Handle(FeatureFlagExistsQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await _repository.GetByKeyAsync(request.Key, cancellationToken).ConfigureAwait(false);

        return featureFlag != null;
    }
}
