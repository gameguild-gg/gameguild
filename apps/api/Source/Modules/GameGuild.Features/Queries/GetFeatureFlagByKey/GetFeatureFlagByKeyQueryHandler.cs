using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for GetFeatureFlagByKeyQuery
/// </summary>
public sealed class GetFeatureFlagByKeyQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagByKeyQuery, FeatureFlagDto?>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<FeatureFlagDto?> Handle(GetFeatureFlagByKeyQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await _repository.GetByKeyAsync(request.Key, cancellationToken).ConfigureAwait(false);

        return featureFlag == null ? null : EntityModelMapper.ToDto(featureFlag);
    }
}
