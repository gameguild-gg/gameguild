using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CalculatePricingQuery.
///     Uses IPlanPricingResolver to fetch plan pricing without direct Subscriptions module dependency.
/// </summary>
/// <remarks>
///     Pricing calculation workflow:
///     1. Fetch subscription plan pricing via IPlanPricingResolver
///     2. Apply pricing rules based on billing cycle
///     3. Apply discount codes if provided
///     4. Return final calculated price
/// </remarks>
public sealed class CalculatePricingQueryHandler : IQueryHandler<CalculatePricingQuery, PricingCalculationResult>
{
    private readonly IPlanPricingResolver _pricingResolver;

    public CalculatePricingQueryHandler(IPlanPricingResolver pricingResolver)
    {
        _pricingResolver = pricingResolver ?? throw new ArgumentNullException(nameof(pricingResolver));
    }

    public async Task<PricingCalculationResult> Handle(CalculatePricingQuery request, CancellationToken cancellationToken)
    {
        // Check if plan exists
        var planExists = await _pricingResolver.PlanExistsAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (!planExists)
        {
            throw new InvalidOperationException($"Subscription plan {request.PlanId} not found");
        }

        // Get base pricing from plan
        var basePrice = await _pricingResolver.GetPlanMonthlyPriceAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        if (basePrice == null)
        {
            throw new InvalidOperationException($"Unable to retrieve pricing for plan {request.PlanId}");
        }
        var discount = Money.Zero();
        var appliedDiscounts = new List<AppliedDiscount>();

        // Apply discount code if provided
        if (!string.IsNullOrEmpty(request.DiscountCode))
        {
            var discountResult = PricingDiscountCalculator.CalculateDiscount(request.DiscountCode, basePrice);
            discount = discountResult.Amount;
            appliedDiscounts.Add(discountResult);
        }

        // Calculate final price
        var totalPrice = basePrice - discount;

        return new ConcretePricingCalculationResult
        {
            BasePrice = basePrice,
            AddOnPrice = Money.Zero(),
            Discount = discount,
            Tax = Money.Zero(), // Tax calculation would integrate with tax service
            TotalPrice = totalPrice,
            BillingCycle = BillingCycle.Monthly,
            AppliedDiscounts = appliedDiscounts
        };
    }
}

/// <summary>
///     Concrete implementation of PricingCalculationResult for instantiation.
/// </summary>
internal sealed class ConcretePricingCalculationResult : PricingCalculationResult;

internal sealed class ConcreteAppliedDiscount : AppliedDiscount;

file static class PricingDiscountCalculator
{
    public static AppliedDiscount CalculateDiscount(string discountCode, Money basePrice)
    {
        var code = discountCode.Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            throw new ArgumentException("Discount code cannot be empty.", nameof(discountCode));
        }

        if (TryParsePrefixedAmount(code, "SAVE", out var savePercent)
            || TryParsePrefixedAmount(code, "PERCENT", out savePercent))
        {
            if (savePercent <= 0m || savePercent > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(discountCode), "Percentage discount must be between 1 and 100.");
            }

            var amount = new Money(basePrice.Amount * (savePercent / 100m), basePrice.Currency);
            return new ConcreteAppliedDiscount
            {
                Code = code,
                Description = $"{savePercent:0.##}% discount",
                Amount = amount,
                Percentage = savePercent,
                Type = DiscountType.Percentage
            };
        }

        if (TryParsePrefixedAmount(code, "FIXED", out var fixedAmount)
            || TryParsePrefixedAmount(code, "OFF", out fixedAmount))
        {
            if (fixedAmount <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(discountCode), "Fixed discount must be greater than zero.");
            }

            var cappedAmount = Math.Min(fixedAmount, basePrice.Amount);
            return new ConcreteAppliedDiscount
            {
                Code = code,
                Description = $"{basePrice.Currency} {cappedAmount:0.00} discount",
                Amount = new Money(cappedAmount, basePrice.Currency),
                Percentage = basePrice.Amount == 0m ? 0m : decimal.Round((cappedAmount / basePrice.Amount) * 100m, 2),
                Type = DiscountType.FixedAmount
            };
        }

        throw new InvalidOperationException(
            $"Discount code '{discountCode}' is not recognized. Supported formats are SAVE10, PERCENT10, FIXED5, and OFF5.");
    }

    private static bool TryParsePrefixedAmount(string code, string prefix, out decimal amount)
    {
        amount = 0m;
        if (!code.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rawAmount = code[prefix.Length..].TrimStart('-', '_');
        return decimal.TryParse(rawAmount, out amount);
    }
}
