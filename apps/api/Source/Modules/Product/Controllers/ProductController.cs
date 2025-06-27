using Microsoft.AspNetCore.Mvc;
using GameGuild.Common.Enums;
using GameGuild.Common.Entities;
using GameGuild.Modules.Product.Models;
using GameGuild.Common.Attributes;
using GameGuild.Modules.Product.Services;
using ProductEntity = GameGuild.Modules.Product.Models.Product;


namespace GameGuild.Modules.Product.Controllers;

/// <summary>
/// REST API controller for managing products
/// Implements 3-layer DAC permission system for all routes
/// 
/// DAC Attribute Usage Examples:
/// - Tenant Level: [RequireTenantPermission(PermissionType.Create)]
/// - Content-Type Level: [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
/// - Resource Level (Preferred): [RequireResourcePermission<ProductEntity>(PermissionType.Update)]
/// - Resource Level (Explicit): [RequireResourcePermission<ProductPermission, ProductEntity>(PermissionType.Update)]
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductController(IProductService productService) : ControllerBase {
  // ===== CONTENT-TYPE LEVEL OPERATIONS =====

  /// <summary>
  /// Get all products (content-type level read permission)
  /// </summary>
  [HttpGet]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetProducts(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var products = await productService.GetProductsAsync(skip, take);

    return Ok(products);
  }

  /// <summary>
  /// Get products by type (content-type level read permission)
  /// </summary>
  [HttpGet("type/{type}")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetProductsByType(
    ProductType type,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var products = await productService.GetProductsByTypeAsync(type, skip, take);

    return Ok(products);
  }

  /// <summary>
  /// Get published products (no permission required - public access)
  /// </summary>
  [HttpGet("published")]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetPublishedProducts(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var products = await productService.GetPublishedProductsAsync(skip, take);

    return Ok(products);
  }

  /// <summary>
  /// Create a new product (content-type level draft permission)
  /// </summary>
  [HttpPost]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Draft)]
  public async Task<ActionResult<ProductEntity>> CreateProduct([FromBody] ProductEntity product) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var createdProduct = await productService.CreateProductAsync(product);

    return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
  }

  /// <summary>
  /// Search products (content-type level read permission)
  /// </summary>
  [HttpGet("search")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> SearchProducts(
    [FromQuery] string searchTerm,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var products = await productService.SearchProductsAsync(searchTerm, skip, take);

    return Ok(products);
  }

  /// <summary>
  /// Get products by creator (content-type level read permission)
  /// </summary>
  [HttpGet("creator/{creatorId}")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetProductsByCreator(
    Guid creatorId,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var products = await productService.GetProductsByCreatorAsync(creatorId, skip, take);

    return Ok(products);
  }

  /// <summary>
  /// Get products in price range (content-type level read permission)
  /// </summary>
  [HttpGet("price-range")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetProductsInPriceRange(
    [FromQuery] decimal minPrice,
    [FromQuery] decimal maxPrice, [FromQuery] string currency = "USD", [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var products = await productService.GetProductsInPriceRangeAsync(minPrice, maxPrice, currency, skip, take);

    return Ok(products);
  }

  /// <summary>
  /// Get popular products (content-type level read permission)
  /// </summary>
  [HttpGet("popular")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetPopularProducts([FromQuery] int count = 10) {
    var products = await productService.GetPopularProductsAsync(count);

    return Ok(products);
  }

  /// <summary>
  /// Get recent products (content-type level read permission)
  /// </summary>
  [HttpGet("recent")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetRecentProducts([FromQuery] int count = 10) {
    var products = await productService.GetRecentProductsAsync(count);

    return Ok(products);
  }

  // ===== RESOURCE-LEVEL OPERATIONS =====

  /// <summary>
  /// Get a specific product by ID (resource-level read permission)
  /// </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductEntity>> GetProduct(Guid id) {
    var product = await productService.GetProductByIdAsync(id);

    if (product == null) return NotFound();

    return Ok(product);
  }

  /// <summary>
  /// Get a specific product with details (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/details")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductEntity>> GetProductWithDetails(Guid id) {
    var product = await productService.GetProductByIdWithDetailsAsync(id);

    if (product == null) return NotFound();

    return Ok(product);
  }

  /// <summary>
  /// Update a product (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> UpdateProduct(Guid id, [FromBody] ProductEntity product) {
    if (id != product.Id) return BadRequest("Product ID mismatch");

    if (!ModelState.IsValid) return BadRequest(ModelState);

    var existingProduct = await productService.GetProductByIdAsync(id);

    if (existingProduct == null) return NotFound();

    var updatedProduct = await productService.UpdateProductAsync(product);

    return Ok(updatedProduct);
  }

  /// <summary>
  /// Delete a product (resource-level delete permission)
  /// </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProduct(Guid id) {
    var existingProduct = await productService.GetProductByIdAsync(id);

    if (existingProduct == null) return NotFound();

    await productService.DeleteProductAsync(id);

    return NoContent();
  }

  /// <summary>
  /// Publish a product (resource-level publish permission)
  /// </summary>
  [HttpPost("{id}/publish")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Publish)]
  public async Task<ActionResult<ProductEntity>> PublishProduct(Guid id) {
    var product = await productService.PublishProductAsync(id);

    return Ok(product);
  }

  /// <summary>
  /// Unpublish a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/unpublish")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> UnpublishProduct(Guid id) {
    var product = await productService.UnpublishProductAsync(id);

    return Ok(product);
  }

  /// <summary>
  /// Archive a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/archive")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> ArchiveProduct(Guid id) {
    var product = await productService.ArchiveProductAsync(id);

    return Ok(product);
  }

  /// <summary>
  /// Set product visibility (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}/visibility")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> SetProductVisibility(
    Guid id,
    [FromBody] Common.Entities.AccessLevel visibility
  ) {
    var product = await productService.SetVisibilityAsync(id, visibility);

    return Ok(product);
  }

  // ===== BUNDLE MANAGEMENT =====

  /// <summary>
  /// Get bundle items (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/bundle-items")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetBundleItems(Guid id) {
    var products = await productService.GetBundleItemsAsync(id);

    return Ok(products);
  }

  /// <summary>
  /// Add product to bundle (resource-level edit permission)
  /// </summary>
  [HttpPost("{bundleId}/bundle-items/{productId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit, "bundleId")]
  public async Task<ActionResult<ProductEntity>> AddToBundle(Guid bundleId, Guid productId) {
    var bundle = await productService.AddToBundleAsync(bundleId, productId);

    return Ok(bundle);
  }

  /// <summary>
  /// Remove product from bundle (resource-level edit permission)
  /// </summary>
  [HttpDelete("{bundleId}/bundle-items/{productId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit, "bundleId")]
  public async Task<ActionResult<ProductEntity>> RemoveFromBundle(Guid bundleId, Guid productId) {
    var bundle = await productService.RemoveFromBundleAsync(bundleId, productId);

    return Ok(bundle);
  }

  // ===== PRICING MANAGEMENT =====

  /// <summary>
  /// Get current pricing for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing/current")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductPricing>> GetCurrentPricing(Guid id) {
    var pricing = await productService.GetCurrentPricingAsync(id);

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  /// <summary>
  /// Get pricing history for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing/history")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductPricing>>> GetPricingHistory(Guid id) {
    var pricing = await productService.GetPricingHistoryAsync(id);

    return Ok(pricing);
  }

  /// <summary>
  /// Set pricing for a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/pricing")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductPricing>> SetPricing(Guid id, [FromBody] SetPricingRequest request) {
    var pricing = await productService.SetPricingAsync(id, request.BasePrice, request.Currency);

    return Ok(pricing);
  }

  // ===== SUBSCRIPTION MANAGEMENT =====

  /// <summary>
  /// Get subscription plans for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/subscription-plans")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductSubscriptionPlan>>> GetSubscriptionPlans(Guid id) {
    var plans = await productService.GetSubscriptionPlansAsync(id);

    return Ok(plans);
  }

  /// <summary>
  /// Create subscription plan for a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/subscription-plans")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductSubscriptionPlan>> CreateSubscriptionPlan(
    Guid id,
    [FromBody] ProductSubscriptionPlan plan
  ) {
    plan.ProductId = id; // Ensure the product ID matches the route
    var createdPlan = await productService.CreateSubscriptionPlanAsync(plan);

    return CreatedAtAction(nameof(GetSubscriptionPlan), new { planId = createdPlan.Id }, createdPlan);
  }

  /// <summary>
  /// Get specific subscription plan (no specific permission - handled by service logic)
  /// </summary>
  [HttpGet("subscription-plans/{planId}")]
  public async Task<ActionResult<ProductSubscriptionPlan>> GetSubscriptionPlan(Guid planId) {
    var plan = await productService.GetSubscriptionPlanAsync(planId);

    if (plan == null) return NotFound();

    return Ok(plan);
  }

  // ===== USER ACCESS MANAGEMENT =====

  /// <summary>
  /// Check if user has access to product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/access/{userId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<bool>> HasUserAccess(Guid id, Guid userId) {
    var hasAccess = await productService.HasUserAccessAsync(userId, id);

    return Ok(hasAccess);
  }

  /// <summary>
  /// Get user product details (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/user-product/{userId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<UserProduct>> GetUserProduct(Guid id, Guid userId) {
    var userProduct = await productService.GetUserProductAsync(userId, id);

    if (userProduct == null) return NotFound();

    return Ok(userProduct);
  }

  /// <summary>
  /// Grant user access to product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/access/{userId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<UserProduct>> GrantUserAccess(
    Guid id, Guid userId,
    [FromBody] GrantAccessRequest request
  ) {
    var userProduct = await productService.GrantUserAccessAsync(
                        userId,
                        id,
                        request.AcquisitionType,
                        request.PurchasePrice,
                        request.Currency,
                        request.ExpiresAt
                      );

    return Ok(userProduct);
  }

  /// <summary>
  /// Revoke user access to product (resource-level edit permission)
  /// </summary>
  [HttpDelete("{id}/access/{userId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult> RevokeUserAccess(Guid id, Guid userId) {
    await productService.RevokeUserAccessAsync(userId, id);

    return NoContent();
  }

  // ===== ANALYTICS AND STATISTICS =====

  /// <summary>
  /// Get product count (content-type level read permission)
  /// </summary>
  [HttpGet("analytics/count")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<int>> GetProductCount(
    [FromQuery] ProductType? type = null,
    [FromQuery] Common.Entities.AccessLevel? visibility = null
  ) {
    var count = await productService.GetProductCountAsync(type, visibility);

    return Ok(count);
  }

  /// <summary>
  /// Get user count for product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/analytics/user-count")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<int>> GetUserCountForProduct(Guid id) {
    var count = await productService.GetUserCountForProductAsync(id);

    return Ok(count);
  }

  /// <summary>
  /// Get total revenue for product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/analytics/revenue")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<decimal>> GetTotalRevenueForProduct(Guid id) {
    var revenue = await productService.GetTotalRevenueForProductAsync(id);

    return Ok(revenue);
  }
}

// ===== DTOs FOR REQUEST BODIES =====

public class SetPricingRequest {
  private decimal _basePrice;

  private string _currency = "USD";

  public decimal BasePrice {
    get => _basePrice;
    set => _basePrice = value;
  }

  public string Currency {
    get => _currency;
    set => _currency = value;
  }
}

public class GrantAccessRequest {
  private ProductAcquisitionType _acquisitionType;

  private decimal _purchasePrice = 0;

  private string _currency = "USD";

  private DateTime? _expiresAt;

  public ProductAcquisitionType AcquisitionType {
    get => _acquisitionType;
    set => _acquisitionType = value;
  }

  public decimal PurchasePrice {
    get => _purchasePrice;
    set => _purchasePrice = value;
  }

  public string Currency {
    get => _currency;
    set => _currency = value;
  }

  public DateTime? ExpiresAt {
    get => _expiresAt;
    set => _expiresAt = value;
  }
}
