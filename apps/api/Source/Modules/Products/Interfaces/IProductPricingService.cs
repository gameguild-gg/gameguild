namespace GameGuild.Modules.Products.Interfaces;

/// <summary>
/// Interface for product pricing services
/// </summary>
public interface IProductPricingService {
  Task<Models.ProductPricing> CreatePricingAsync(Models.ProductPricing pricing);

  Task<Models.ProductPricing?> GetPricingByIdAsync(int id);

  Task<IEnumerable<Models.ProductPricing>> GetPricingsByProductIdAsync(int productId);

  Task<Models.ProductPricing?> GetDefaultPricingForProductAsync(int productId);

  Task<Models.ProductPricing> UpdatePricingAsync(Models.ProductPricing pricing);

  Task<bool> DeletePricingAsync(int id);

  Task<decimal> GetEffectivePriceAsync(int productId, int? promoCodeId = null);
}
