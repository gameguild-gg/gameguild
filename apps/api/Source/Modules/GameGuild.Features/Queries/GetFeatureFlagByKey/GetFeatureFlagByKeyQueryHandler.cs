using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;
using GameGuild.Features.Entities;
using GameGuild.Features.Services.Utilities;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for GetFeatureFlagByKeyQuery
/// </summary>
public sealed class GetFeatureFlagByKeyQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagByKeyQuery, FeatureFlagDto?>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<FeatureFlagDto?> Handle(GetFeatureFlagByKeyQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await _repository.GetByKeyAsync(request.Key, cancellationToken);

        return featureFlag == null ? null : EntityModelMapper.ToDto(featureFlag);
    }
}
