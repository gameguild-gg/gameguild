using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;
using GameGuild.Features.Entities;
using GameGuild.Features.Services.Utilities;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for GetFeatureFlagByIdQuery
/// </summary>
public sealed class GetFeatureFlagByIdQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagByIdQuery, FeatureFlagDto?>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<FeatureFlagDto?> Handle(GetFeatureFlagByIdQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await _repository.GetByIdAsync(request.Id, cancellationToken);

        return featureFlag == null ? null : EntityModelMapper.ToDto(featureFlag);
    }
}
