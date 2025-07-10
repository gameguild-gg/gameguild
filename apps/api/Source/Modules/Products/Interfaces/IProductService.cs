using GameGuild.Common;


namespace GameGuild.Modules.Products.Interfaces;

/// <summary>
/// Interface for product-related services
/// </summary>
public interface IProductService {
  Task<Models.Product> CreateProductAsync(Models.Product product);

  Task<Models.Product?> GetProductByIdAsync(int id);

  Task<IEnumerable<Models.Product>> GetProductsByTypeAsync(ProductType type);

  Task<Models.Product> UpdateProductAsync(Models.Product product);

  Task<bool> DeleteProductAsync(int id);

  Task<IEnumerable<Models.Product>> GetActiveProductsAsync();

  Task<IEnumerable<Models.Product>> GetProductsBundleAsync();

  Task<bool> AddProductToBundleAsync(int bundleId, int productId);

  Task<bool> RemoveProductFromBundleAsync(int bundleId, int productId);
}
