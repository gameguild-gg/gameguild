using FluentValidation;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get a promo campaign by id (issue #418 campaign management).
/// </summary>
public sealed record GetPromoCampaignByIdQuery(Guid CampaignId) : IQuery<PromoCampaignDto>;

/// <summary>Handler for <see cref="GetPromoCampaignByIdQuery"/>.</summary>
public sealed class GetPromoCampaignByIdQueryHandler(
    IPromoCampaignRepository campaignRepository,
    IPromoCodeRepository promoCodeRepository)
    : IQueryHandler<GetPromoCampaignByIdQuery, PromoCampaignDto>
{
    /// <inheritdoc />
    public async Task<PromoCampaignDto> Handle(GetPromoCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken).ConfigureAwait(false)
            ?? throw new PromoCampaignNotFoundException(request.CampaignId);

        var codeCount = await promoCodeRepository
            .GetByCampaignAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
        var attributed = await campaignRepository
            .CountAttributionsForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);

        return campaign.ToDto(codeCount.Count, attributed);
    }
}

/// <summary>
/// Query to list promo campaigns (paged) with optional status filter
/// (issue #418 campaign management).
/// </summary>
/// <param name="Status">Optional status filter</param>
/// <param name="SearchTerm">Optional name/key/description search</param>
/// <param name="Skip">Items to skip</param>
/// <param name="Take">Items to take</param>
public sealed record GetPromoCampaignsQuery(
    PromoCampaignStatus? Status = null,
    string? SearchTerm = null,
    int Skip = 0,
    int Take = 50
) : IQuery<PagedResult<PromoCampaignDto>>;

/// <summary>Handler for <see cref="GetPromoCampaignsQuery"/>.</summary>
public sealed class GetPromoCampaignsQueryHandler(
    IPromoCampaignRepository campaignRepository)
    : IQueryHandler<GetPromoCampaignsQuery, PagedResult<PromoCampaignDto>>
{
    /// <inheritdoc />
    public async Task<PagedResult<PromoCampaignDto>> Handle(GetPromoCampaignsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (items, totalCount) = await campaignRepository.GetPagedAsync(
            request.Status,
            request.SearchTerm,
            request.Skip,
            request.Take,
            cancellationToken).ConfigureAwait(false);

        var dtos = new List<PromoCampaignDto>(items.Count);
        foreach (var campaign in items)
        {
            var codeCount = await campaignRepository
                .CountCodesForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
            var attributed = await campaignRepository
                .CountAttributionsForKeyAsync(campaign.Key, cancellationToken).ConfigureAwait(false);
            dtos.Add(campaign.ToDto(codeCount, attributed));
        }

        return new PagedResult<PromoCampaignDto>(dtos, totalCount, request.Skip, request.Take);
    }
}

/// <summary>Validator for <see cref="GetPromoCampaignsQuery"/>.</summary>
public sealed class GetPromoCampaignsQueryValidator : AbstractValidator<GetPromoCampaignsQuery>
{
    /// <summary>Initializes the validator.</summary>
    public GetPromoCampaignsQueryValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("Skip cannot be negative.");
        RuleFor(x => x.Take).InclusiveBetween(1, 200).WithMessage("Take must be between 1 and 200.");
    }
}
