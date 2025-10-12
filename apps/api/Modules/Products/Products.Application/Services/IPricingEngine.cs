using GameGuild.Modules.Products.Domain.Entities;


namespace GameGuild.Modules.Products.Application.Services;

/// <summary>Interface for dynamic pricing engine</summary>
public interface IPricingEngine
{
    /// <summary>Calculate the final price for a product with given parameters</summary>
    Task<PricingCalculationResult> CalculatePriceAsync(
        Guid productId,
        int quantity,
        string? region = null,
        string? customerSegment = null,
        DateTime? checkDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Get all applicable pricing rules for a product</summary>
    Task<IEnumerable<PricingRule>> GetApplicableRulesAsync(
        Guid productId,
        int quantity,
        string? region = null,
        string? customerSegment = null,
        DateTime? checkDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Get the best pricing tier for a product and quantity</summary>
    Task<PricingTier?> GetBestTierAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>Calculate volume discount for a product</summary>
    Task<decimal> CalculateVolumeDiscountAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>Suggest optimal pricing based on market data</summary>
    Task<decimal> SuggestOptimalPricingAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of pricing calculation</summary>
public class PricingCalculationResult
{
    /// <summary>Product ID</summary>
    public Guid ProductId { get; set; }

    /// <summary>Quantity</summary>
    public int Quantity { get; set; }

    /// <summary>Base price per unit</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Applied pricing rules</summary>
    public List<string> AppliedRules { get; set; } = new();

    /// <summary>Total discount percentage</summary>
    public decimal TotalDiscountPercentage { get; set; }

    /// <summary>Total discount amount</summary>
    public decimal TotalDiscountAmount { get; set; }

    /// <summary>Final unit price</summary>
    public decimal FinalUnitPrice { get; set; }

    /// <summary>Final total price</summary>
    public decimal FinalTotalPrice { get; set; }

    /// <summary>Currency code</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Calculation timestamp</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
