using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for GetFeatureFlagByIdQuery
/// </summary>
public sealed class GetFeatureFlagByIdQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagByIdQuery, FeatureFlagDto?>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<FeatureFlagDto?> Handle(GetFeatureFlagByIdQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        return featureFlag == null ? null : EntityModelMapper.ToDto(featureFlag);
    }
}
