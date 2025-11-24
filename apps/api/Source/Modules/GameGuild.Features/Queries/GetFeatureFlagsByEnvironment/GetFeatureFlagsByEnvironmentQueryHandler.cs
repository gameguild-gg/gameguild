using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;
using GameGuild.Features.Services.Utilities;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving feature flags by environment
/// </summary>
public sealed class GetFeatureFlagsByEnvironmentQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagsByEnvironmentQuery, IEnumerable<FeatureFlagDto>>
{
    public async Task<IEnumerable<FeatureFlagDto>> Handle(GetFeatureFlagsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        // Get all feature flags for the environment
        var featureFlags = await repository.GetByEnvironmentAsync(request.Environment, cancellationToken);

        // Filter by enabled status if specified
        if (request.IsEnabled.HasValue) { featureFlags = featureFlags.Where(ff => ff.IsEnabled == request.IsEnabled.Value); }

        // Map to DTOs
        return featureFlags.Select(EntityModelMapper.ToDto);
    }
}
