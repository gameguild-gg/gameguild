namespace GameGuild.Modules.Products;

/// <summary>
/// Interface for product pricing services
/// </summary>
public interface IProductPricingService {
  Task<ProductPricing> CreatePricingAsync(ProductPricing pricing);

  Task<ProductPricing?> GetPricingByIdAsync(int id);

  Task<IEnumerable<ProductPricing>> GetPricingsByProductIdAsync(int productId);

  Task<ProductPricing?> GetDefaultPricingForProductAsync(int productId);

  Task<ProductPricing> UpdatePricingAsync(ProductPricing pricing);

  Task<bool> DeletePricingAsync(int id);

  Task<decimal> GetEffectivePriceAsync(int productId, int? promoCodeId = null);
}
