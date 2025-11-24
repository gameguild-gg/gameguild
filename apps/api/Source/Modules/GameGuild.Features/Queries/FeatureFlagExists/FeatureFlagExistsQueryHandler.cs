using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Entities;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for FeatureFlagExistsQuery
/// </summary>
public sealed class FeatureFlagExistsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<FeatureFlagExistsQuery, bool>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<bool> Handle(FeatureFlagExistsQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await _repository.GetByKeyAsync(request.Key, cancellationToken);

        return featureFlag != null;
    }
}
