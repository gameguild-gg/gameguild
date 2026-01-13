namespace GameGuild.Commerce.Products;

/// <summary> Interface for product pricing services </summary>
public interface IProductPricingService {
  Task<ProductPricing> CreatePricingAsync(ProductPricing pricing);

  Task<ProductPricing?> GetPricingByIdAsync(Guid id);

  Task<IEnumerable<ProductPricing>> GetPricingsByProductIdAsync(Guid productId);

  Task<ProductPricing?> GetDefaultPricingForProductAsync(Guid productId);

  Task<ProductPricing> UpdatePricingAsync(ProductPricing pricing);

  Task<bool> DeletePricingAsync(Guid id);

  Task<decimal> GetEffectivePriceAsync(Guid productId, Guid? promoCodeId = null);
}
