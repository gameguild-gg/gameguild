namespace GameGuild.Commerce.Products;

/// <summary> Interface for product pricing services with immutable versioning support </summary>
public interface IProductPricingService {
  Task<ProductPricing> CreatePricingAsync(ProductPricing pricing);

  Task<ProductPricing?> GetPricingByIdAsync(Guid id);

  Task<IEnumerable<ProductPricing>> GetPricingsByProductIdAsync(Guid productId);

  Task<ProductPricing?> GetDefaultPricingForProductAsync(Guid productId);

  /// <summary>
  /// Updates pricing with immutable version tracking for price changes
  /// </summary>
  /// <param name="pricing">Updated pricing data</param>
  /// <param name="updatedByUserId">User making the change for audit trail</param>
  /// <param name="changeReason">Optional reason for the price change</param>
  Task<ProductPricing> UpdatePricingAsync(ProductPricing pricing, Guid? updatedByUserId = null, string? changeReason = null);

  Task<bool> DeletePricingAsync(Guid id);

  Task<decimal> GetEffectivePriceAsync(Guid productId, Guid? promoCodeId = null);
}
