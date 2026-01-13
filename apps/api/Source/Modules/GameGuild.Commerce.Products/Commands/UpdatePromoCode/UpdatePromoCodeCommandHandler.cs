using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for updating an existing promo code
/// </summary>
public class UpdatePromoCodeCommandHandler(IPromoCodeRepository promoCodeRepository)
    : ICommandHandler<UpdatePromoCodeCommand, PromoCodeDto>
{
    public async Task<PromoCodeDto> Handle(UpdatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var promoCode = await promoCodeRepository.GetByIdAsync(request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (promoCode == null)
        {
            throw new PromoCodeNotFoundException(request.Id);
        }

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
            promoCode.Name = request.Name;

        if (request.Description != null)
            promoCode.Description = request.Description;

        if (request.Type.HasValue)
            promoCode.Type = request.Type.Value;

        if (request.DiscountPercentage.HasValue)
            promoCode.DiscountPercentage = request.DiscountPercentage.Value;

        if (request.DiscountAmount.HasValue)
            promoCode.DiscountAmount = request.DiscountAmount.Value;

        if (!string.IsNullOrWhiteSpace(request.Currency))
            promoCode.Currency = request.Currency;

        if (request.MinimumOrderAmount.HasValue)
            promoCode.MinimumOrderAmount = request.MinimumOrderAmount.Value;

        if (request.MaxUses.HasValue)
            promoCode.MaxUses = request.MaxUses.Value;

        if (request.MaxUsesPerUser.HasValue)
            promoCode.MaxUsesPerUser = request.MaxUsesPerUser.Value;

        if (request.ValidFrom.HasValue)
            promoCode.ValidFrom = request.ValidFrom.Value;

        if (request.ValidUntil.HasValue)
            promoCode.ValidUntil = request.ValidUntil.Value;

        if (request.IsActive.HasValue)
            promoCode.IsActive = request.IsActive.Value;

        if (request.IsExclusive.HasValue)
            promoCode.IsExclusive = request.IsExclusive.Value;

        if (request.StackingPriority.HasValue)
            promoCode.StackingPriority = request.StackingPriority.Value;

        if (request.ProductId.HasValue)
            promoCode.ProductId = request.ProductId.Value;

        promoCode.UpdatedAt = DateTime.UtcNow;

        // Update in repository
        await promoCodeRepository.UpdateAsync(promoCode, cancellationToken).ConfigureAwait(false);
        await promoCodeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Get usage count
        var usageCount = await promoCodeRepository.GetUsageCountAsync(promoCode.Id, cancellationToken)
            .ConfigureAwait(false);

        // Map to DTO
        return new PromoCodeDto(
            promoCode.Id,
            promoCode.Code,
            promoCode.Name,
            promoCode.Description,
            promoCode.Type,
            promoCode.DiscountPercentage,
            promoCode.DiscountAmount,
            promoCode.Currency,
            promoCode.MinimumOrderAmount,
            promoCode.MaxUses,
            promoCode.MaxUsesPerUser,
            promoCode.ValidFrom,
            promoCode.ValidUntil,
            promoCode.IsActive,
            promoCode.IsExclusive,
            promoCode.StackingPriority,
            promoCode.ProductId,
            usageCount,
            promoCode.CreatedAt,
            promoCode.UpdatedAt
        );
    }
}
