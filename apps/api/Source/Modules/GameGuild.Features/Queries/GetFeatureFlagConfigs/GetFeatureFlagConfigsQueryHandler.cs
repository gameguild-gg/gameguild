using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving feature flag configurations for SDK
/// </summary>
public sealed class GetFeatureFlagConfigsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagConfigsQuery, IEnumerable<FeatureFlagConfig>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagConfig>> Handle(GetFeatureFlagConfigsQuery request, CancellationToken cancellationToken)
    {
        // Get SDK configuration for the environment
        var configs = await _repository.GetConfigsAsync(request.Environment, request.TenantId, request.FeatureKeys, request.ModifiedSince, cancellationToken).ConfigureAwait(false);

        return configs;
    }
}
