using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving targeting rule by ID
/// </summary>
public sealed class GetTargetingRuleByIdQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetTargetingRuleByIdQuery, FeatureFlagTargetDto?>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<FeatureFlagTargetDto?> Handle(GetTargetingRuleByIdQuery request, CancellationToken cancellationToken)
    {
        // Get targeting rule by ID
        var targetingRule = await _repository.GetTargetingRuleByIdAsync(request.Id, cancellationToken);

        return targetingRule;
    }
}
