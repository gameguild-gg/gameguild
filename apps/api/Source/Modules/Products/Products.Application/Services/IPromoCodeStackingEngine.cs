using GameGuild.Modules.Products.Domain.Entities;

namespace GameGuild.Modules.Products.Application.Services;

/// <summary>Interface for promo code stacking engine</summary>
public interface IPromoCodeStackingEngine
{
    /// <summary>Validate if promo codes can be stacked together</summary>
    Task<StackingValidationResult> ValidateStackingAsync(
        List<string> promoCodes,
        decimal orderAmount,
        CancellationToken cancellationToken = default);

    /// <summary>Apply stacked promo codes to an order</summary>
    Task<StackingApplicationResult> ApplyStackedCodesAsync(
        List<string> promoCodes,
        decimal orderAmount,
        CancellationToken cancellationToken = default);

    /// <summary>Calculate total discount from stacked codes</summary>
    Task<decimal> CalculateStackedDiscountAsync(
        List<PromoCode> codes,
        decimal orderAmount,
        PromoStackingRule rule,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of stacking validation</summary>
public class StackingValidationResult
{
    /// <summary>Whether stacking is valid</summary>
    public bool IsValid { get; set; }

    /// <summary>Validation errors</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Validated promo codes</summary>
    public List<PromoCode> ValidCodes { get; set; } = new();

    /// <summary>Applied stacking rule</summary>
    public PromoStackingRule? AppliedRule { get; set; }
}

/// <summary>Result of stacking application</summary>
public class StackingApplicationResult
{
    /// <summary>Original order amount</summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>Applied promo codes</summary>
    public List<AppliedPromoCode> AppliedCodes { get; set; } = new();

    /// <summary>Total discount amount</summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>Final amount after discounts</summary>
    public decimal FinalAmount { get; set; }

    /// <summary>Applied stacking rule</summary>
    public string? AppliedRule { get; set; }
}

/// <summary>Applied promo code details</summary>
public class AppliedPromoCode
{
    /// <summary>Promo code</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Discount amount</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Application order</summary>
    public int Order { get; set; }
}
