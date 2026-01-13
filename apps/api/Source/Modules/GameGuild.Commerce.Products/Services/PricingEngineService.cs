namespace GameGuild.Commerce.Products;

/// <summary>
/// Service for calculating prices with discounts, sales, and promo codes
/// </summary>
public class PricingEngineService(
    IProductRepository productRepository,
    IPromoCodeRepository promoCodeRepository) : IPricingEngineService
{
    /// <inheritdoc />
    public async Task<PricingCalculationResult> CalculatePriceAsync(
        Product product,
        ProductPricing? pricing = null,
        IEnumerable<string>? promoCodes = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        // Get default pricing if not specified
        pricing ??= product.Pricing.FirstOrDefault(p => p.IsDefault)
                    ?? product.Pricing.FirstOrDefault();

        if (pricing == null)
        {
            return new PricingCalculationResult(
                BasePrice: 0,
                SalePrice: null,
                IsSaleActive: false,
                PromoDiscount: 0,
                FinalPrice: 0,
                Currency: "USD",
                AppliedPromoCodes: new List<string>()
            );
        }

        var basePrice = pricing.BasePrice;
        var isSaleActive = IsSaleActive(pricing);
        var effectivePrice = isSaleActive && pricing.SalePrice.HasValue
            ? pricing.SalePrice.Value
            : basePrice;

        // Apply promo codes if provided
        var promoDiscount = 0m;
        var appliedCodes = new List<string>();
        var promoCodesList = promoCodes?.ToList();

        if (promoCodesList is { Count: > 0 })
        {
            var promoResult = await ApplyPromoCodesAsync(
                effectivePrice,
                promoCodesList,
                product.Id,
                userId,
                cancellationToken).ConfigureAwait(false);

            promoDiscount = promoResult.TotalDiscount;
            appliedCodes = promoResult.AppliedCodes.Select(c => c.Code).ToList();
        }

        var finalPrice = Math.Max(0, effectivePrice - promoDiscount);

        return new PricingCalculationResult(
            BasePrice: basePrice,
            SalePrice: pricing.SalePrice,
            IsSaleActive: isSaleActive,
            PromoDiscount: promoDiscount,
            FinalPrice: finalPrice,
            Currency: pricing.Currency,
            AppliedPromoCodes: appliedCodes
        );
    }

    /// <inheritdoc />
    public async Task<PricingCalculationResult> CalculatePriceByIdAsync(
        Guid productId,
        Guid? pricingId = null,
        IEnumerable<string>? promoCodes = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(
            productId,
            cancellationToken,
            includePricing: true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(productId);
        }

        ProductPricing? pricing = null;
        if (pricingId.HasValue)
        {
            pricing = product.Pricing.FirstOrDefault(p => p.Id == pricingId.Value);
        }

        return await CalculatePriceAsync(product, pricing, promoCodes, userId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PromoCodeApplicationResult> ApplyPromoCodesAsync(
        decimal orderAmount,
        IEnumerable<string> promoCodes,
        Guid? productId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var appliedCodes = new List<AppliedPromoCode>();
        var rejectedCodes = new List<RejectedPromoCode>();
        var totalDiscount = 0m;
        var remainingAmount = orderAmount;
        var hasExclusiveCode = false;

        foreach (var codeString in promoCodes.Distinct())
        {
            // Skip if we already applied an exclusive code
            if (hasExclusiveCode)
            {
                rejectedCodes.Add(new RejectedPromoCode(codeString, "Cannot stack with exclusive promo code"));
                continue;
            }

            var validation = await ValidatePromoCodeAsync(
                codeString,
                remainingAmount,
                productId,
                userId,
                cancellationToken).ConfigureAwait(false);

            if (!validation.IsValid)
            {
                rejectedCodes.Add(new RejectedPromoCode(codeString, validation.ErrorMessage ?? "Invalid code"));
                continue;
            }

            var promoCode = await promoCodeRepository.GetByCodeAsync(codeString, cancellationToken)
                .ConfigureAwait(false);

            if (promoCode == null)
            {
                rejectedCodes.Add(new RejectedPromoCode(codeString, "Code not found"));
                continue;
            }

            if (promoCode.IsExclusive)
            {
                hasExclusiveCode = true;
            }

            var discountAmount = promoCode.CalculateDiscount(remainingAmount);

            appliedCodes.Add(new AppliedPromoCode(
                codeString,
                discountAmount,
                promoCode.DiscountPercentage
            ));

            totalDiscount += discountAmount;
            remainingAmount = Math.Max(0, remainingAmount - discountAmount);
        }

        return new PromoCodeApplicationResult(
            OriginalAmount: orderAmount,
            FinalAmount: Math.Max(0, orderAmount - totalDiscount),
            TotalDiscount: totalDiscount,
            AppliedCodes: appliedCodes,
            RejectedCodes: rejectedCodes
        );
    }

    /// <inheritdoc />
    public async Task<PromoCodeValidationResult> ValidatePromoCodeAsync(
        string code,
        decimal orderAmount,
        Guid? productId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new PromoCodeValidationResult(false, ErrorMessage: "Promo code is required");
        }

        var promoCode = await promoCodeRepository.GetByCodeAsync(code, cancellationToken)
            .ConfigureAwait(false);

        if (promoCode == null)
        {
            return new PromoCodeValidationResult(false, ErrorMessage: "Promo code not found");
        }

        // Check if active
        if (!promoCode.IsActive)
        {
            return new PromoCodeValidationResult(false, ErrorMessage: "Promo code is not active");
        }

        // Check validity period
        if (!promoCode.IsCurrentlyValid())
        {
            return new PromoCodeValidationResult(false, ErrorMessage: "Promo code has expired or is not yet valid");
        }

        // Check product restriction
        if (promoCode.ProductId.HasValue && productId.HasValue && promoCode.ProductId != productId)
        {
            return new PromoCodeValidationResult(false, ErrorMessage: "Promo code is not valid for this product");
        }

        // Check minimum order amount
        if (promoCode.MinimumOrderAmount.HasValue && orderAmount < promoCode.MinimumOrderAmount.Value)
        {
            return new PromoCodeValidationResult(false,
                ErrorMessage: $"Minimum order amount of {promoCode.MinimumOrderAmount.Value:C} required");
        }

        // Check usage limits
        if (promoCode.MaxUses.HasValue)
        {
            var usageCount = await promoCodeRepository.GetUsageCountAsync(promoCode.Id, cancellationToken)
                .ConfigureAwait(false);
            if (usageCount >= promoCode.MaxUses.Value)
            {
                return new PromoCodeValidationResult(false, ErrorMessage: "Promo code has reached maximum uses");
            }
        }

        // Check per-user usage limits
        if (promoCode.MaxUsesPerUser.HasValue && userId.HasValue)
        {
            var userUsageCount = await promoCodeRepository.GetUserUsageCountAsync(
                promoCode.Id, userId.Value, cancellationToken).ConfigureAwait(false);
            if (userUsageCount >= promoCode.MaxUsesPerUser.Value)
            {
                return new PromoCodeValidationResult(false,
                    ErrorMessage: "You have already used this promo code the maximum number of times");
            }
        }

        // Calculate discount
        var discountAmount = promoCode.CalculateDiscount(orderAmount);

        return new PromoCodeValidationResult(
            IsValid: true,
            Code: promoCode.Code,
            DiscountAmount: discountAmount,
            DiscountPercentage: promoCode.DiscountPercentage
        );
    }

    /// <inheritdoc />
    public async Task<decimal> GetCurrentPriceAsync(
        Guid productId,
        Guid? pricingId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(
            productId,
            cancellationToken,
            includePricing: true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(productId);
        }

        var pricing = pricingId.HasValue
            ? product.Pricing.FirstOrDefault(p => p.Id == pricingId.Value)
            : product.Pricing.FirstOrDefault(p => p.IsDefault) ?? product.Pricing.FirstOrDefault();

        if (pricing == null)
        {
            return 0;
        }

        return IsSaleActive(pricing) && pricing.SalePrice.HasValue
            ? pricing.SalePrice.Value
            : pricing.BasePrice;
    }

    /// <inheritdoc />
    public bool IsSaleActive(ProductPricing pricing)
    {
        if (!pricing.SalePrice.HasValue)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        var startValid = !pricing.SaleStartDate.HasValue || pricing.SaleStartDate.Value <= now;
        var endValid = !pricing.SaleEndDate.HasValue || pricing.SaleEndDate.Value > now;

        return startValid && endValid;
    }
}
