using System.Security.Claims;
using GameGuild.Authorization.Identity;
using GameGuild.CQRS;
using GameGuild.GraphQL;
using GameGuild.Modules.Contents;
using GameGuild.Source.Modules.Products.Models;
using GameGuild.Source.Modules.Products.Queries;
using GameGuild.Source.Modules.Products.Services;
using GameGuild.Source.Modules.Products.Services;
using MediatR;
using MediatR;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;
using ProductEntity = GameGuild.Source.Modules.Products.Models.Product;
using ProductTypeEnum = GameGuild.ProductType;


namespace GameGuild.Source.Modules.Products.GraphQL;

/// <summary> GraphQL queries for Product module using CQRS pattern </summary>
[ExtendObjectType<Query>]
public class ProductQueries {
  /// <summary> Gets all products accessible to the current user using CQRS pattern </summary>
  public async Task<IEnumerable<ProductEntity>> GetProducts(
    [Service] IMediator mediator,
    int skip = 0,
    int take = 50,
    ProductTypeEnum? type = null,
    ContentStatus? status = null,
    AccessLevel? visibility = null,
    string? searchTerm = null,
    bool? isBundle = null
  ) {
    var query = new GetProductsQuery { Skip = skip, Take = take, Type = type, Status = status, Visibility = visibility, SearchTerm = searchTerm, IsBundle = isBundle };

    return await mediator.Send(query);
  }

  /// <summary> Gets a product by its unique identifier using CQRS pattern </summary>
  public async Task<ProductEntity?> GetProductById(Guid id, [Service] IMediator mediator, bool includePricing = true, bool includePrograms = true) {
    var query = new GetProductByIdQuery { ProductId = id, IncludePricing = includePricing, IncludePrograms = includePrograms };

    return await mediator.Send(query);
  }

  /// <summary> Gets products by type </summary>
  public async Task<IEnumerable<ProductEntity>> GetProductsByType(ProductTypeEnum type, [Service] IProductService productService, int skip = 0, int take = 50) { return await productService.GetProductsByTypeAsync(type, skip, take); }

  /// <summary> Gets published products visible to the public </summary>
  public async Task<IEnumerable<ProductEntity>> GetPublishedProducts([Service] IProductService productService, int skip = 0, int take = 50) { return await productService.GetPublishedProductsAsync(skip, take); }

  /// <summary> Gets products by creator </summary>
  public async Task<IEnumerable<ProductEntity>> GetProductsByCreator(Guid creatorId, [Service] IProductService productService, int skip = 0, int take = 50) { return await productService.GetProductsByCreatorAsync(creatorId, skip, take); }

  /// <summary> Test resolver without authorization to check if auth pipeline is the issue </summary>
  public Task<string> TestAuth(ClaimsPrincipal claimsPrincipal, [Service] ILogger<ProductQueries> logger) {
    logger.LogInformation("=== TestAuth Debug ===");
    logger.LogInformation("Claims Principal is null: {IsNull}", claimsPrincipal == null);
    logger.LogInformation("Claims Principal Identity Authenticated: {IsAuthenticated}", claimsPrincipal?.Identity?.IsAuthenticated);
    return Task.FromResult($"Auth test - Authenticated: {claimsPrincipal?.Identity?.IsAuthenticated}, Claims: {claimsPrincipal?.Claims?.Count() ?? 0}");
  }

  /// <summary> Gets the current user's products (owned/editable) </summary>
  [Authorize] // Requires authentication
  public async Task<IEnumerable<ProductEntity>> GetMyProducts(ClaimsPrincipal claimsPrincipal, [Service] IProductService productService, [Service] ILogger<ProductQueries> logger, int skip = 0, int take = 50) {
    try {
      // Debug: Log all claims to understand the token structure
      logger.LogInformation("=== MyProducts Debug ===");
      logger.LogInformation("Claims Principal Identity Authenticated: {IsAuthenticated}", claimsPrincipal?.Identity?.IsAuthenticated);
      logger.LogInformation("Total Claims Count: {ClaimsCount}", claimsPrincipal?.Claims?.Count() ?? 0);

      if (claimsPrincipal?.Claims != null) {
        foreach (var claim in claimsPrincipal.Claims) {
          logger.LogInformation("Claim Type: '{Type}', Value: '{Value}'", claim.Type, claim.Value);
        }
      }

      // Try multiple ways to get user ID
      var subClaim = claimsPrincipal?.FindFirst("sub")?.Value;
      var nameIdentifierClaim = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userIdClaim = claimsPrincipal?.FindFirst("user_id")?.Value;

      logger.LogInformation("sub claim: {SubClaim}", subClaim);
      logger.LogInformation("NameIdentifier claim: {NameIdentifierClaim}", nameIdentifierClaim);
      logger.LogInformation("user_id claim: {UserIdClaim}", userIdClaim);

      // Use extension method
      var userId = claimsPrincipal?.GetUserId();
      logger.LogInformation("GetUserId() result: {UserId}", userId);

      if (userId == null) {
        logger.LogError("User ID not found in token - throwing UnauthorizedAccessException");
        throw new UnauthorizedAccessException("User ID not found in token");
      }

      logger.LogInformation("Getting products for user: {UserId}", userId);
      var result = await productService.GetProductsByCreatorAsync(userId.Value, skip, take);
      logger.LogInformation("Found {ProductCount} products for user {UserId}", result?.Count() ?? 0, userId);

      return result ?? [];
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error in GetMyProducts: {Message}", ex.Message);
      throw;
    }
  }

  /// <summary> Searches products by term </summary>
  public async Task<IEnumerable<ProductEntity>> SearchProducts(string searchTerm, [Service] IProductService productService, int skip = 0, int take = 50) { return await productService.SearchProductsAsync(searchTerm, skip, take); }

  /// <summary> Gets products in a price range </summary>
  public async Task<IEnumerable<ProductEntity>> GetProductsInPriceRange(decimal minPrice, decimal maxPrice, [Service] IProductService productService, string currency = "USD", int skip = 0, int take = 50) {
    return await productService.GetProductsInPriceRangeAsync(minPrice, maxPrice, currency, skip, take);
  }

  /// <summary> Gets popular products </summary>
  public async Task<IEnumerable<ProductEntity>> GetPopularProducts([Service] IProductService productService, int count = 10) { return await productService.GetPopularProductsAsync(count); }

  /// <summary> Gets recent products </summary>
  public async Task<IEnumerable<ProductEntity>> GetRecentProducts([Service] IProductService productService, int count = 10) { return await productService.GetRecentProductsAsync(count); }

  /// <summary> Gets bundle items for a product bundle </summary>
  public async Task<IEnumerable<ProductEntity>> GetBundleItems(Guid bundleId, [Service] IProductService productService) { return await productService.GetBundleItemsAsync(bundleId); }

  /// <summary> Gets current pricing for a product </summary>
  public async Task<ProductPricing?> GetCurrentPricing(Guid productId, [Service] IProductService productService) { return await productService.GetCurrentPricingAsync(productId); }

  /// <summary> Gets pricing history for a product </summary>
  public async Task<IEnumerable<ProductPricing>> GetPricingHistory(Guid productId, [Service] IProductService productService) { return await productService.GetPricingHistoryAsync(productId); }

  /// <summary> Gets user's products </summary>
  public async Task<IEnumerable<UserProduct>> GetUserProducts(Guid userId, [Service] IProductService productService) { return await productService.GetUserProductsAsync(userId); }

  /// <summary> Checks if user has access to a product </summary>
  public async Task<bool> HasUserAccess(Guid userId, Guid productId, [Service] IProductService productService) { return await productService.HasUserAccessAsync(userId, productId); }

  /// <summary> Gets a promo code by code </summary>
  public async Task<PromoCode?> GetPromoCode(string code, [Service] IProductService productService) { return await productService.GetPromoCodeAsync(code); }

  /// <summary> Validates a promo code </summary>
  public async Task<bool> IsPromoCodeValid(string code, [Service] IProductService productService, Guid? productId = null) { return await productService.IsPromoCodeValidAsync(code, productId); }

  /// <summary> Gets product count by filters </summary>
  public async Task<int> GetProductCount([Service] IProductService productService, ProductTypeEnum? type = null, AccessLevel? visibility = null) { return await productService.GetProductCountAsync(type, visibility); }

  /// <summary> Gets user count for a product </summary>
  public async Task<int> GetUserCountForProduct(Guid productId, [Service] IProductService productService) { return await productService.GetUserCountForProductAsync(productId); }

  /// <summary> Gets total revenue for a product </summary>
  public async Task<decimal> GetTotalRevenueForProduct(Guid productId, [Service] IProductService productService) { return await productService.GetTotalRevenueForProductAsync(productId); }
}
