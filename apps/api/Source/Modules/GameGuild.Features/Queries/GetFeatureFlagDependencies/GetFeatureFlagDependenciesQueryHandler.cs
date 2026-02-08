using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving feature flag dependencies
/// </summary>
public sealed class GetFeatureFlagDependenciesQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagDependenciesQuery, IEnumerable<FeatureFlagDependency>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagDependency>> Handle(GetFeatureFlagDependenciesQuery request, CancellationToken cancellationToken)
    {
        // Get dependencies for the feature flag
        var dependencies = await _repository.GetDependenciesAsync(request.FeatureFlagId, request.IncludeInverse, cancellationToken).ConfigureAwait(false);

        return dependencies;
    }
}
