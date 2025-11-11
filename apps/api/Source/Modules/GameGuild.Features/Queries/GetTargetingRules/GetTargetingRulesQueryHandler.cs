using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving all targeting rules for a feature flag
/// </summary>
public sealed class GetTargetingRulesQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetTargetingRulesQuery, IEnumerable<FeatureFlagTargetDto>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagTargetDto>> Handle(GetTargetingRulesQuery request, CancellationToken cancellationToken)
    {
        // Get all targeting rules for the feature flag
        var targetingRules = await _repository.GetTargetingRulesAsync(request.FeatureFlagId, cancellationToken);

        return targetingRules;
    }
}
