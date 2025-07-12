using GameGuild.Modules.Contents;
using GameGuild.Modules.Products.Services;
using GameGuild.Common;
using MediatR;
using ProductEntity = GameGuild.Modules.Products.Models.Product;
using ProductTypeEnum = GameGuild.Common.ProductType;


namespace GameGuild.Modules.Products.GraphQL;

/// <summary>
/// GraphQL queries for Product module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class ProductQueries {
  /// <summary>
  /// Gets all products accessible to the current user using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetProducts(
    [Service] IMediator mediator, [Service] IProductService productService, int skip = 0, int take = 50
  ) {
    // TODO: Replace with proper CQRS query when GetAllProductsQuery is implemented
    // var query = new GetAllProductsQuery { Skip = skip, Take = take };
    // return await mediator.Send(query);
    
    // Temporary fallback to service until CQRS queries are implemented
    return await productService.GetProductsAsync(skip, take);
  }

  /// <summary>
  /// Gets a product by its unique identifier using CQRS pattern
  /// </summary>
  public async Task<ProductEntity?> GetProductById(Guid id, [Service] IMediator mediator, [Service] IProductService productService) { 
    // TODO: Replace with proper CQRS query when GetProductByIdQuery is implemented
    // var query = new GetProductByIdQuery { ProductId = id };
    // return await mediator.Send(query);
    
    // Temporary fallback to service until CQRS queries are implemented
    return await productService.GetProductByIdAsync(id); 
  }

  /// <summary>
  /// Gets products by type
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetProductsByType(
    ProductTypeEnum type,
    [Service] IProductService productService, int skip = 0, int take = 50
  ) {
    return await productService.GetProductsByTypeAsync(type, skip, take);
  }

  /// <summary>
  /// Gets published products visible to the public
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetPublishedProducts(
    [Service] IProductService productService,
    int skip = 0, int take = 50
  ) {
    return await productService.GetPublishedProductsAsync(skip, take);
  }

  /// <summary>
  /// Gets products by creator
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetProductsByCreator(
    Guid creatorId,
    [Service] IProductService productService, int skip = 0, int take = 50
  ) {
    return await productService.GetProductsByCreatorAsync(creatorId, skip, take);
  }

  /// <summary>
  /// Searches products by term
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> SearchProducts(
    string searchTerm,
    [Service] IProductService productService, int skip = 0, int take = 50
  ) {
    return await productService.SearchProductsAsync(searchTerm, skip, take);
  }

  /// <summary>
  /// Gets products in a price range
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetProductsInPriceRange(
    decimal minPrice, decimal maxPrice,
    [Service] IProductService productService, string currency = "USD", int skip = 0, int take = 50
  ) {
    return await productService.GetProductsInPriceRangeAsync(minPrice, maxPrice, currency, skip, take);
  }

  /// <summary>
  /// Gets popular products
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetPopularProducts(
    [Service] IProductService productService,
    int count = 10
  ) {
    return await productService.GetPopularProductsAsync(count);
  }

  /// <summary>
  /// Gets recent products
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetRecentProducts(
    [Service] IProductService productService,
    int count = 10
  ) {
    return await productService.GetRecentProductsAsync(count);
  }

  /// <summary>
  /// Gets bundle items for a product bundle
  /// </summary>
  public async Task<IEnumerable<ProductEntity>> GetBundleItems(Guid bundleId, [Service] IProductService productService) { return await productService.GetBundleItemsAsync(bundleId); }

  /// <summary>
  /// Gets current pricing for a product
  /// </summary>
  public async Task<Models.ProductPricing?> GetCurrentPricing(Guid productId, [Service] IProductService productService) { return await productService.GetCurrentPricingAsync(productId); }

  /// <summary>
  /// Gets pricing history for a product
  /// </summary>
  public async Task<IEnumerable<Models.ProductPricing>> GetPricingHistory(
    Guid productId,
    [Service] IProductService productService
  ) {
    return await productService.GetPricingHistoryAsync(productId);
  }

  /// <summary>
  /// Gets user's products
  /// </summary>
  public async Task<IEnumerable<Models.UserProduct>> GetUserProducts(
    Guid userId,
    [Service] IProductService productService
  ) {
    return await productService.GetUserProductsAsync(userId);
  }

  /// <summary>
  /// Checks if user has access to a product
  /// </summary>
  public async Task<bool> HasUserAccess(Guid userId, Guid productId, [Service] IProductService productService) { return await productService.HasUserAccessAsync(userId, productId); }

  /// <summary>
  /// Gets a promo code by code
  /// </summary>
  public async Task<Models.PromoCode?> GetPromoCode(string code, [Service] IProductService productService) { return await productService.GetPromoCodeAsync(code); }

  /// <summary>
  /// Validates a promo code
  /// </summary>
  public async Task<bool> IsPromoCodeValid(
    string code, [Service] IProductService productService,
    Guid? productId = null
  ) {
    return await productService.IsPromoCodeValidAsync(code, productId);
  }

  /// <summary>
  /// Gets product count by filters
  /// </summary>
  public async Task<int> GetProductCount(
    [Service] IProductService productService, ProductTypeEnum? type = null,
    AccessLevel? visibility = null
  ) {
    return await productService.GetProductCountAsync(type, visibility);
  }

  /// <summary>
  /// Gets user count for a product
  /// </summary>
  public async Task<int> GetUserCountForProduct(Guid productId, [Service] IProductService productService) { return await productService.GetUserCountForProductAsync(productId); }

  /// <summary>
  /// Gets total revenue for a product
  /// </summary>
  public async Task<decimal> GetTotalRevenueForProduct(Guid productId, [Service] IProductService productService) { return await productService.GetTotalRevenueForProductAsync(productId); }
}
