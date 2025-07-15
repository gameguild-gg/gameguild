using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Products.Models;
using GameGuild.Modules.Products.Commands;
using GameGuild.Modules.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductEntity = GameGuild.Modules.Products.Models.Product;

namespace GameGuild.Modules.Products.Controllers;

/// <summary>
/// REST API controller for managing products using CQRS pattern
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
public class ProductController(IMediator mediator) : ControllerBase {
  // ===== CONTENT-TYPE LEVEL OPERATIONS =====

  /// <summary>
  /// Get all products using CQRS pattern (content-type level read permission)
  /// </summary>
  [HttpGet]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetProducts() {
    var query = new GetProductsQuery();
    var products = await mediator.Send(query);
    return Ok(products);
  }

  /// <summary>
  /// Create a new product using CQRS pattern (content-type level create permission)
  /// </summary>
  [HttpPost]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Create)]
  public async Task<ActionResult<ProductEntity>> CreateProduct([FromBody] CreateProductCommand command) {
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return CreatedAtAction(nameof(GetProduct), new { id = result.Product!.Id }, result.Product);
  }

  // ===== RESOURCE LEVEL OPERATIONS =====

  /// <summary>
  /// Get a specific product by ID using CQRS pattern (resource level read permission)
  /// </summary>
  [HttpGet("{id:guid}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductEntity>> GetProduct(Guid id) {
    var query = new GetProductByIdQuery { ProductId = id };
    var product = await mediator.Send(query);
    if (product == null) return NotFound();
    return Ok(product);
  }

  /// <summary>
  /// Update a product using CQRS pattern (resource level update permission)
  /// </summary>
  [HttpPut("{id:guid}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Update)]
  public async Task<ActionResult<ProductEntity>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command) {
    command.ProductId = id;
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return Ok(result.Product);
  }

  /// <summary>
  /// Delete a product using CQRS pattern (resource level delete permission)
  /// </summary>
  [HttpDelete("{id:guid}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProduct(Guid id) {
    var command = new DeleteProductCommand { ProductId = id };
    var result = await mediator.Send(command);
    if (!result.Success) return BadRequest(result.ErrorMessage);
    return NoContent();
  }
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetProducts(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var products = await mediator.Send(new GetProductsQuery(skip, take));

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
    var products = await mediator.Send(new GetProductsByTypeQuery(type, skip, take));

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
    var products = await mediator.Send(new GetPublishedProductsQuery(skip, take));

    return Ok(products);
  }

  /// <summary>
  /// Create a new product (content-type level draft permission)
  /// </summary>
  [HttpPost]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Draft)]
  public async Task<ActionResult<ProductEntity>> CreateProduct([FromBody] ProductEntity product) {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var createdProduct = await mediator.Send(new CreateProductCommand(product));

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
    var products = await mediator.Send(new SearchProductsQuery(searchTerm, skip, take));

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
    var products = await mediator.Send(new GetProductsByCreatorQuery(creatorId, skip, take));

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
    var products = await mediator.Send(new GetProductsInPriceRangeQuery(minPrice, maxPrice, currency, skip, take));

    return Ok(products);
  }

  /// <summary>
  /// Get popular products (content-type level read permission)
  /// </summary>
  [HttpGet("popular")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetPopularProducts([FromQuery] int count = 10) {
    var products = await mediator.Send(new GetPopularProductsQuery(count));

    return Ok(products);
  }

  /// <summary>
  /// Get recent products (content-type level read permission)
  /// </summary>
  [HttpGet("recent")]
  [RequireContentTypePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetRecentProducts([FromQuery] int count = 10) {
    var products = await mediator.Send(new GetRecentProductsQuery(count));

    return Ok(products);
  }

  // ===== RESOURCE-LEVEL OPERATIONS =====

  /// <summary>
  /// Get a specific product by ID (resource-level read permission)
  /// </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductEntity>> GetProduct(Guid id) {
    var product = await mediator.Send(new GetProductByIdQuery(id));

    if (product == null) return NotFound();

    return Ok(product);
  }

  /// <summary>
  /// Get a specific product with details (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/details")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductEntity>> GetProductWithDetails(Guid id) {
    var product = await mediator.Send(new GetProductByIdWithDetailsQuery(id));

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

    var existingProduct = await mediator.Send(new GetProductByIdQuery(id));

    if (existingProduct == null) return NotFound();

    var updatedProduct = await mediator.Send(new UpdateProductCommand(product));

    return Ok(updatedProduct);
  }

  /// <summary>
  /// Delete a product (resource-level delete permission)
  /// </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProduct(Guid id) {
    var existingProduct = await mediator.Send(new GetProductByIdQuery(id));

    if (existingProduct == null) return NotFound();

    await mediator.Send(new DeleteProductCommand(id));

    return NoContent();
  }

  /// <summary>
  /// Publish a product (resource-level publish permission)
  /// </summary>
  [HttpPost("{id}/publish")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Publish)]
  public async Task<ActionResult<ProductEntity>> PublishProduct(Guid id) {
    var product = await mediator.Send(new PublishProductCommand(id));

    return Ok(product);
  }

  /// <summary>
  /// Unpublish a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/unpublish")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> UnpublishProduct(Guid id) {
    var product = await mediator.Send(new UnpublishProductCommand(id));

    return Ok(product);
  }

  /// <summary>
  /// Archive a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/archive")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> ArchiveProduct(Guid id) {
    var product = await mediator.Send(new ArchiveProductCommand(id));

    return Ok(product);
  }

  /// <summary>
  /// Set product visibility (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}/visibility")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductEntity>> SetProductVisibility(
    Guid id,
    [FromBody] AccessLevel visibility
  ) {
    var product = await mediator.Send(new SetProductVisibilityCommand(id, visibility));

    return Ok(product);
  }

  // ===== BUNDLE MANAGEMENT =====

  /// <summary>
  /// Get bundle items (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/bundle-items")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductEntity>>> GetBundleItems(Guid id) {
    var products = await mediator.Send(new GetBundleItemsQuery(id));

    return Ok(products);
  }

  /// <summary>
  /// Add product to bundle (resource-level edit permission)
  /// </summary>
  [HttpPost("{bundleId}/bundle-items/{productId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit, "bundleId")]
  public async Task<ActionResult<ProductEntity>> AddToBundle(Guid bundleId, Guid productId) {
    var bundle = await mediator.Send(new AddToBundleCommand(bundleId, productId));

    return Ok(bundle);
  }

  /// <summary>
  /// Remove product from bundle (resource-level edit permission)
  /// </summary>
  [HttpDelete("{bundleId}/bundle-items/{productId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit, "bundleId")]
  public async Task<ActionResult<ProductEntity>> RemoveFromBundle(Guid bundleId, Guid productId) {
    var bundle = await mediator.Send(new RemoveFromBundleCommand(bundleId, productId));

    return Ok(bundle);
  }

  // ===== PRICING MANAGEMENT =====

  /// <summary>
  /// Get current pricing for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing/current")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<ProductPricing>> GetCurrentPricing(Guid id) {
    var pricing = await mediator.Send(new GetCurrentPricingQuery(id));

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  /// <summary>
  /// Get pricing history for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing/history")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductPricing>>> GetPricingHistory(Guid id) {
    var pricing = await mediator.Send(new GetPricingHistoryQuery(id));

    return Ok(pricing);
  }

  /// <summary>
  /// Set pricing for a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/pricing")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult<ProductPricing>> SetPricing(Guid id, [FromBody] SetPricingRequest request) {
    var pricing = await mediator.Send(new SetPricingCommand(id, request.BasePrice, request.Currency));

    return Ok(pricing);
  }

  // ===== SUBSCRIPTION MANAGEMENT =====

  /// <summary>
  /// Get subscription plans for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/subscription-plans")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductSubscriptionPlan>>> GetSubscriptionPlans(Guid id) {
    var plans = await mediator.Send(new GetSubscriptionPlansQuery(id));

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
    var createdPlan = await mediator.Send(new CreateSubscriptionPlanCommand(plan));

    return CreatedAtAction(nameof(GetSubscriptionPlan), new { planId = createdPlan.Id }, createdPlan);
  }

  /// <summary>
  /// Get specific subscription plan (no specific permission - handled by service logic)
  /// </summary>
  [HttpGet("subscription-plans/{planId}")]
  public async Task<ActionResult<ProductSubscriptionPlan>> GetSubscriptionPlan(Guid planId) {
    var plan = await mediator.Send(new GetSubscriptionPlanQuery(planId));

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
    var hasAccess = await mediator.Send(new HasUserAccessQuery(userId, id));

    return Ok(hasAccess);
  }

  /// <summary>
  /// Get user product details (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/user-product/{userId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<UserProduct>> GetUserProduct(Guid id, Guid userId) {
    var userProduct = await mediator.Send(new GetUserProductQuery(userId, id));

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
    var userProduct = await mediator.Send(new GrantUserAccessCommand(
                        userId,
                        id,
                        request.AcquisitionType,
                        request.PurchasePrice,
                        request.Currency,
                        request.ExpiresAt
                      ));

    return Ok(userProduct);
  }

  /// <summary>
  /// Revoke user access to product (resource-level edit permission)
  /// </summary>
  [HttpDelete("{id}/access/{userId}")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Edit)]
  public async Task<ActionResult> RevokeUserAccess(Guid id, Guid userId) {
    await mediator.Send(new RevokeUserAccessCommand(userId, id));

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
    [FromQuery] AccessLevel? visibility = null
  ) {
    var count = await mediator.Send(new GetProductCountQuery(type, visibility));

    return Ok(count);
  }

  /// <summary>
  /// Get user count for product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/analytics/user-count")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<int>> GetUserCountForProduct(Guid id) {
    var count = await mediator.Send(new GetUserCountForProductQuery(id));

    return Ok(count);
  }

  /// <summary>
  /// Get total revenue for product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/analytics/revenue")]
  [RequireResourcePermission<ProductEntity>(PermissionType.Read)]
  public async Task<ActionResult<decimal>> GetTotalRevenueForProduct(Guid id) {
    var revenue = await mediator.Send(new GetTotalRevenueForProductQuery(id));

    return Ok(revenue);
  }
}

// ===== DTOs FOR REQUEST BODIES =====
