using GameGuild.Modules.Products.Application.Services;
using GameGuild.Database;
using GameGuild.Modules.Products.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Products.Infrastructure.Services;

/// <summary>Implementation of promo code stacking engine</summary>
public class PromoCodeStackingEngine : IPromoCodeStackingEngine
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PromoCodeStackingEngine> _logger;

    public PromoCodeStackingEngine(ApplicationDbContext context, ILogger<PromoCodeStackingEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StackingValidationResult> ValidateStackingAsync(
        List<string> promoCodes,
        decimal orderAmount,
        CancellationToken cancellationToken = default)
    {
        var result = new StackingValidationResult();

        // Get active stacking rule
        var stackingRule = await _context.Set<PromoStackingRule>()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        if (stackingRule == null)
        {
            result.Errors.Add("No active stacking rule found");
            return result;
        }

        result.AppliedRule = stackingRule;

        // Check count limit
        if (promoCodes.Count > stackingRule.MaxStackableCount)
        {
            result.Errors.Add($"Maximum {stackingRule.MaxStackableCount} codes can be stacked");
            return result;
        }

        // Check minimum order amount
        if (stackingRule.MinOrderAmountForStacking.HasValue &&
            orderAmount < stackingRule.MinOrderAmountForStacking.Value)
        {
            result.Errors.Add($"Minimum order amount ${stackingRule.MinOrderAmountForStacking} required for stacking");
            return result;
        }

        // Load promo codes
        var codes = await _context.Set<PromoCode>()
            .Where(pc => promoCodes.Contains(pc.Code) && pc.IsActive)
            .ToListAsync(cancellationToken);

        if (codes.Count != promoCodes.Count)
        {
            result.Errors.Add("One or more promo codes are invalid or inactive");
            return result;
        }

        // Validate each code
        foreach (var code in codes)
        {
            if (!code.IsCurrentlyValid())
            {
                result.Errors.Add($"Code {code.Code} is not currently valid");
                continue;
            }

            if (code.MinimumOrderAmount.HasValue && orderAmount < code.MinimumOrderAmount.Value)
            {
                result.Errors.Add($"Code {code.Code} requires minimum order ${code.MinimumOrderAmount}");
                continue;
            }

            result.ValidCodes.Add(code);
        }

        // Check for exclusive codes
        var exclusiveCodes = result.ValidCodes.Where(c => c.GetIsExclusive()).ToList();
        if (exclusiveCodes.Any() && !stackingRule.AllowExclusiveStacking)
        {
            result.Errors.Add($"Exclusive codes cannot be stacked: {string.Join(", ", exclusiveCodes.Select(c => c.Code))}");
            result.ValidCodes.Clear();
            return result;
        }

        // Check same-type stacking
        if (!stackingRule.AllowSameTypeStacking)
        {
            var duplicateTypes = result.ValidCodes
                .GroupBy(c => c.Type)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateTypes.Any())
            {
                result.Errors.Add($"Cannot stack multiple codes of same type: {string.Join(", ", duplicateTypes)}");
                result.ValidCodes.Clear();
                return result;
            }
        }

        result.IsValid = result.ValidCodes.Count > 0 && result.Errors.Count == 0;

        _logger.LogInformation(
            "Stacking validation: Valid={IsValid}, Codes={Count}, Errors={ErrorCount}",
            result.IsValid, result.ValidCodes.Count, result.Errors.Count);

        return result;
    }

    public async Task<StackingApplicationResult> ApplyStackedCodesAsync(
        List<string> promoCodes,
        decimal orderAmount,
        CancellationToken cancellationToken = default)
    {
        var result = new StackingApplicationResult
        {
            OriginalAmount = orderAmount
        };

        // Validate first
        var validation = await ValidateStackingAsync(promoCodes, orderAmount, cancellationToken);
        if (!validation.IsValid || validation.AppliedRule == null)
        {
            result.FinalAmount = orderAmount;
            return result;
        }

        result.AppliedRule = validation.AppliedRule.Name;

        // Calculate total discount
        var totalDiscount = await CalculateStackedDiscountAsync(
            validation.ValidCodes,
            orderAmount,
            validation.AppliedRule,
            cancellationToken);

        // Apply max caps
        if (validation.AppliedRule.MaxTotalDiscountPercentage.HasValue)
        {
            var maxDiscount = orderAmount * validation.AppliedRule.MaxTotalDiscountPercentage.Value / 100;
            totalDiscount = Math.Min(totalDiscount, maxDiscount);
        }

        if (validation.AppliedRule.MaxTotalDiscountAmount.HasValue)
        {
            totalDiscount = Math.Min(totalDiscount, validation.AppliedRule.MaxTotalDiscountAmount.Value);
        }

        result.TotalDiscount = totalDiscount;
        result.FinalAmount = Math.Max(0, orderAmount - totalDiscount);

        // Track applied codes
        int order = 1;
        foreach (var code in validation.ValidCodes.OrderByDescending(c => c.StackingPriority))
        {
            result.AppliedCodes.Add(new AppliedPromoCode
            {
                Code = code.Code,
                DiscountAmount = code.CalculateDiscount(orderAmount),
                Order = order++
            });
        }

        _logger.LogInformation(
            "Applied stacked codes: Original=${Original}, Discount=${Discount}, Final=${Final}",
            orderAmount, totalDiscount, result.FinalAmount);

        return result;
    }

    public async Task<decimal> CalculateStackedDiscountAsync(
        List<PromoCode> codes,
        decimal orderAmount,
        PromoStackingRule rule,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async for consistency

        decimal totalDiscount = 0;
        decimal currentAmount = orderAmount;

        // Sort by priority
        var sortedCodes = codes.OrderByDescending(c => c.StackingPriority).ToList();

        switch (rule.ConflictStrategy)
        {
            case ConflictResolutionStrategy.HighestDiscount:
                totalDiscount = sortedCodes.Max(c => c.CalculateDiscount(orderAmount));
                break;

            case ConflictResolutionStrategy.LowestDiscount:
                totalDiscount = sortedCodes.Min(c => c.CalculateDiscount(orderAmount));
                break;

            case ConflictResolutionStrategy.FirstCodeOnly:
                totalDiscount = sortedCodes.First().CalculateDiscount(orderAmount);
                break;

            case ConflictResolutionStrategy.LastCodeOnly:
                totalDiscount = sortedCodes.Last().CalculateDiscount(orderAmount);
                break;

            case ConflictResolutionStrategy.Sequential:
                foreach (var code in sortedCodes)
                {
                    var discount = code.CalculateDiscount(currentAmount);
                    totalDiscount += discount;
                    currentAmount -= discount;
                }
                break;

            case ConflictResolutionStrategy.Additive:
                totalDiscount = sortedCodes.Sum(c => c.CalculateDiscount(orderAmount));
                break;
        }

        return totalDiscount;
    }
}
