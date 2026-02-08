using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving feature flags by tenant
/// </summary>
public sealed class GetFeatureFlagsByTenantQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagsByTenantQuery, IEnumerable<FeatureFlagDto>>
{
    public async Task<IEnumerable<FeatureFlagDto>> Handle(GetFeatureFlagsByTenantQuery request, CancellationToken cancellationToken)
    {
        // Get all feature flags for the tenant
        var featureFlags = await repository.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        // Filter by enabled status if specified
        if (request.IsEnabled.HasValue) { featureFlags = featureFlags.Where(ff => ff.IsEnabled == request.IsEnabled.Value); }

        // Map to DTOs
        return featureFlags.Select(EntityModelMapper.ToDto);
    }
}
