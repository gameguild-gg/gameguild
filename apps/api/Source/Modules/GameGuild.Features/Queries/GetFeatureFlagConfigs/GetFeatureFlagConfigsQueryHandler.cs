using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Models;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving feature flag configurations for SDK
/// </summary>
public sealed class GetFeatureFlagConfigsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagConfigsQuery, IEnumerable<FeatureFlagConfig>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagConfig>> Handle(GetFeatureFlagConfigsQuery request, CancellationToken cancellationToken)
    {
        // Get SDK configuration for the environment
        var configs = await _repository.GetConfigsAsync(request.Environment, request.TenantId, request.FeatureKeys, request.ModifiedSince, cancellationToken);

        return configs;
    }
}
