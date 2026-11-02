using GameGuild.CQRS;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for creating a new promo code. Validity edges are normalized to UTC
/// instants in the code's timezone (or the configured tenant default) before
/// persistence, so runtime evaluation is timezone-aware end to end (issue #418).
/// </summary>
public sealed class CreatePromoCodeCommandHandler(
    IPromoCodeRepository promoCodeRepository,
    IOptions<PromoEngineOptions>? engineOptions = null)
    : ICommandHandler<CreatePromoCodeCommand, PromoCodeDto>
{
    public async Task<PromoCodeDto> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check if code already exists
        if (await promoCodeRepository.CodeExistsAsync(request.Code, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Promo code '{request.Code}' already exists");
        }

        // Issue #418: resolve the window timezone (code-specific, else tenant default)
        // and normalize the validity edges to UTC instants before storage.
        var defaultTimeZoneId = engineOptions?.Value.DefaultTimeZoneId;
        var (timeZone, _) = PromoValidityWindow.ResolveTimeZone(
            string.IsNullOrWhiteSpace(request.TimeZoneId) ? defaultTimeZoneId : request.TimeZoneId);

        // Create new promo code entity (code is normalized to the stored uppercase form)
        var promoCode = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = PromoCodeNormalizer.Normalize(request.Code) ?? request.Code,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = request.DiscountAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            Currency = request.Currency,
            MinimumOrderAmount = request.MinimumOrderAmount,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            FreeTrialExtensionDays = request.FreeTrialExtensionDays,
            DurationMonths = request.DurationMonths,
            ApplyToRenewal = request.ApplyToRenewal,
            ValidFrom = PromoValidityWindow.NormalizeToUtc(request.ValidFrom, timeZone),
            ValidUntil = PromoValidityWindow.NormalizeToUtc(request.ValidUntil, timeZone),
            TimeZoneId = timeZone.Id,
            EligibleCustomerType = request.EligibleCustomerType,
            IsActive = request.IsActive,
            IsExclusive = request.IsExclusive,
            StackingPriority = request.StackingPriority,
            CampaignId = request.CampaignId,
            ProductId = request.ProductId,
            PlanId = request.PlanId,
            CreatedBy = request.CreatedBy ?? Guid.Empty
        };

        // Add to repository
        await promoCodeRepository.AddAsync(promoCode, cancellationToken).ConfigureAwait(false);
        await promoCodeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Map to DTO
        return promoCode.ToDto(0);
    }
}
