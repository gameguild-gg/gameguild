using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for creating a new promo code
/// </summary>
public sealed class CreatePromoCodeCommandHandler(IPromoCodeRepository promoCodeRepository)
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

        // Create new promo code entity
        var promoCode = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = request.DiscountAmount,
            Currency = request.Currency,
            MinimumOrderAmount = request.MinimumOrderAmount,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            IsActive = request.IsActive,
            IsExclusive = request.IsExclusive,
            StackingPriority = request.StackingPriority,
            ProductId = request.ProductId,
            CreatedBy = request.CreatedBy ?? Guid.Empty
        };

        // Add to repository
        await promoCodeRepository.AddAsync(promoCode, cancellationToken).ConfigureAwait(false);
        await promoCodeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Map to DTO
        return MapToDto(promoCode, 0);
    }

    private static PromoCodeDto MapToDto(PromoCode promoCode, int usageCount)
    {
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
