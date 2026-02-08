using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for GetAllFeatureFlagsQuery
/// </summary>
public sealed class GetAllFeatureFlagsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetAllFeatureFlagsQuery, IEnumerable<FeatureFlagDto>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagDto>> Handle(GetAllFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var featureFlags = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        // Filter by environment if specified
        if (!string.IsNullOrEmpty(request.Environment)) { featureFlags = featureFlags.Where(f => string.IsNullOrEmpty(f.Environment) || f.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase)); }

        // Filter by enabled status if specified
        if (request.IsEnabled.HasValue) { featureFlags = featureFlags.Where(f => f.IsEnabled == request.IsEnabled.Value); }

        // Filter by global/tenant if specified
        if (request.IsGlobal.HasValue) { featureFlags = featureFlags.Where(f => request.IsGlobal.Value ? !f.TenantId.HasValue : f.TenantId.HasValue); }

        // Map to DTOs
        return featureFlags.Select(EntityModelMapper.ToDto).ToList();
    }
}
