using GameGuild.CQRS;
using FluentValidation;

namespace GameGuild.Assets.Queries;

/// <summary>
/// Query to get assets owned by a user.
/// </summary>
public record GetUserAssetsQuery(
    Guid UserId,
    Guid? TenantId,
    int? Skip = null,
    int? Take = null) : IRequest<IReadOnlyList<AssetDto>>;

public class GetUserAssetsValidator : AbstractValidator<GetUserAssetsQuery>
{
    public GetUserAssetsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
        RuleFor(x => x.Take).InclusiveBetween(1, 100).When(x => x.Take.HasValue);
    }
}

public class GetUserAssetsHandler : IRequestHandler<GetUserAssetsQuery, IReadOnlyList<AssetDto>>
{
    private readonly IAssetReferenceRepository _referenceRepository;

    public GetUserAssetsHandler(IAssetReferenceRepository referenceRepository)
    {
        _referenceRepository = referenceRepository;
    }

    public async Task<IReadOnlyList<AssetDto>> Handle(
        GetUserAssetsQuery request,
        CancellationToken ct = default)
    {
        var references = await _referenceRepository.GetByUserAsync(request.UserId, ct);

        var result = references.Select(reference =>
        {
            AssetContentDto? contentDto = null;
            if (reference.Content != null)
            {
                contentDto = new AssetContentDto(
                    reference.Content.Id,
                    reference.Content.ContentHash,
                    reference.Content.MimeType,
                    reference.Content.SizeBytes,
                    reference.Content.Width,
                    reference.Content.Height,
                    reference.Content.VirusScanStatus,
                    reference.Content.ModerationStatus);
            }

            return new AssetDto(
                reference.Id,
                reference.AssetContentId,
                reference.CreatedByUserId,
                reference.DisplayName,
                reference.AccessPolicy,
                reference.ParentResourceType,
                reference.ParentResourceId,
                reference.AccessCount,
                reference.LastAccessedAt,
                reference.CreatedAt,
                reference.UpdatedAt,
                contentDto);
        }).ToList();

        // Apply pagination
        if (request.Skip.HasValue)
            result = result.Skip(request.Skip.Value).ToList();
        if (request.Take.HasValue)
            result = result.Take(request.Take.Value).ToList();

        return result;
    }
}
