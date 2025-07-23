using GameGuild.Common;
using GameGuild.Common.Extensions;
using System.Security.Claims;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Products;

/// <summary>
/// REST API controller for managing products using CQRS pattern
/// Implements 3-layer DAC permission system for all routes
/// 
/// DAC Attribute Usage Examples:
/// - Tenant Level: [RequireTenantPermission(PermissionType.Create)]
/// - Content-Type Level: [RequireContentTypePermission<Product>(PermissionType.Read)]
/// - Resource Level (Preferred): [RequireResourcePermission<Product>(PermissionType.Update)]
/// - Resource Level (Explicit): [RequireResourcePermission<ProductPermission, Product>(PermissionType.Update)]
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductController(IMediator mediator) : ControllerBase {
  // ===== CONTENT-TYPE LEVEL OPERATIONS =====

  /// <summary>
  /// Get all products using CQRS pattern (content-type level read permission)
  /// </summary>
  [HttpGet]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetProducts() {
    var query = new GetProductsQuery();
    var products = await mediator.Send(query);

    return Ok(products);
  }

  /// <summary>
  /// Create a new product using CQRS pattern (content-type level create permission)
  /// </summary>
  [HttpPost]
  [RequireContentTypePermission<Product>(PermissionType.Create)]
  public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductCommand command) {
    var result = await mediator.Send(command);

    if (!result.Success) return BadRequest(result.Error);

    return CreatedAtAction(nameof(GetProduct), new { id = result.Product!.Id }, result.Product);
  }

  // ===== RESOURCE LEVEL OPERATIONS =====

  /// <summary>
  /// Get a specific product by ID using CQRS pattern (resource level read permission)
  /// </summary>
  [HttpGet("{id:guid}")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<Product>> GetProduct(Guid id) {
    var query = new GetProductByIdQuery { ProductId = id };
    var product = await mediator.Send(query);

    if (product == null) return NotFound();

    return Ok(product);
  }

  /// <summary>
  /// Update a product using CQRS pattern (resource level update permission)
  /// </summary>
  [HttpPut("{id:guid}")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<Product>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command) {
    var updateCommand = command with { ProductId = id, UpdatedBy = User.GetUserId() ?? Guid.Empty };
    var result = await mediator.Send(updateCommand);

    if (!result.Success) return BadRequest(result.Error);

    return Ok(result.Product);
  }

  /// <summary>
  /// Delete a product using CQRS pattern (resource level delete permission)
  /// </summary>
  [HttpDelete("{id:guid}")]
  [RequireResourcePermission<Product>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProduct(Guid id) {
    var command = new DeleteProductCommand { ProductId = id, DeletedBy = User.GetUserId() ?? Guid.Empty };
    var result = await mediator.Send(command);

    if (!result.Success) return BadRequest(result.Error);

    return NoContent();
  }

  /// <summary>
  /// Get products by type (content-type level read permission)
  /// </summary>
  [HttpGet("type/{type}")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetProductsByType(
    Common.ProductType type,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var products = await mediator.Send(new GetProductsByTypeQuery { Type = type, Skip = skip, Take = take });

    return Ok(products);
  }

  /// <summary>
  /// Get published products (no permission required - public access)
  /// </summary>
  [HttpGet("published")]
  public async Task<ActionResult<IEnumerable<Product>>> GetPublishedProducts(
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var products = await mediator.Send(new GetPublishedProductsQuery { Skip = skip, Take = take });

    return Ok(products);
  }

  /// <summary>
  /// Search products (content-type level read permission)
  /// </summary>
  [HttpGet("search")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> SearchProducts(
    [FromQuery] string searchTerm,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var products = await mediator.Send(new SearchProductsQuery { SearchTerm = searchTerm, Skip = skip, Take = take });

    return Ok(products);
  }

  /// <summary>
  /// Get products by creator (content-type level read permission)
  /// </summary>
  [HttpGet("creator/{creatorId}")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCreator(
    Guid creatorId,
    [FromQuery] int skip = 0, [FromQuery] int take = 50
  ) {
    var products = await mediator.Send(new GetProductsByCreatorQuery { CreatorId = creatorId, Skip = skip, Take = take });

    return Ok(products);
  }

  /// <summary>
  /// Get products in price range (content-type level read permission)
  /// </summary>
  [HttpGet("price-range")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetProductsInPriceRange(
    [FromQuery] decimal minPrice,
    [FromQuery] decimal maxPrice, [FromQuery] string currency = "USD", [FromQuery] int skip = 0,
    [FromQuery] int take = 50
  ) {
    var products = await mediator.Send(new GetProductsInPriceRangeQuery { MinPrice = minPrice, MaxPrice = maxPrice, Currency = currency, Skip = skip, Take = take });

    return Ok(products);
  }

  /// <summary>
  /// Get popular products (content-type level read permission)
  /// </summary>
  [HttpGet("popular")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetPopularProducts([FromQuery] int count = 10) {
    var products = await mediator.Send(new GetPopularProductsQuery { Take = count });

    return Ok(products);
  }

  /// <summary>
  /// Get recent products (content-type level read permission)
  /// </summary>
  [HttpGet("recent")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetRecentProducts([FromQuery] int count = 10) {
    var products = await mediator.Send(new GetRecentProductsQuery { Take = count });

    return Ok(products);
  }

  /// <summary>
  /// Publish a product (resource-level publish permission)
  /// </summary>
  [HttpPost("{id}/publish")]
  [RequireResourcePermission<Product>(PermissionType.Publish)]
  public async Task<ActionResult<Product>> PublishProduct(Guid id) {
    var product = await mediator.Send(new PublishProductCommand { ProductId = id, PublishedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(product);
  }

  /// <summary>
  /// Unpublish a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/unpublish")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<Product>> UnpublishProduct(Guid id) {
    var product = await mediator.Send(new UnpublishProductCommand { ProductId = id, UnpublishedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(product);
  }

  /// <summary>
  /// Archive a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/archive")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<Product>> ArchiveProduct(Guid id) {
    var product = await mediator.Send(new ArchiveProductCommand { ProductId = id, ArchivedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(product);
  }

  /// <summary>
  /// Set product visibility (resource-level edit permission)
  /// </summary>
  [HttpPut("{id}/visibility")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<Product>> SetProductVisibility(
    Guid id,
    [FromBody] AccessLevel visibility
  ) {
    var product = await mediator.Send(new SetProductVisibilityCommand { ProductId = id, Visibility = visibility, UpdatedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(product);
  }

  // ===== BUNDLE MANAGEMENT =====

  /// <summary>
  /// Get bundle items (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/bundle-items")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Product>>> GetBundleItems(Guid id) {
    var products = await mediator.Send(new GetBundleItemsQuery { BundleId = id });

    return Ok(products);
  }

  /// <summary>
  /// Add product to bundle (resource-level edit permission)
  /// </summary>
  [HttpPost("{bundleId}/bundle-items/{productId}")]
  [RequireResourcePermission<Product>(PermissionType.Edit, "bundleId")]
  public async Task<ActionResult<Product>> AddToBundle(Guid bundleId, Guid productId) {
    var bundle = await mediator.Send(new AddToBundleCommand { BundleId = bundleId, ProductId = productId, UpdatedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(bundle);
  }

  /// <summary>
  /// Remove product from bundle (resource-level edit permission)
  /// </summary>
  [HttpDelete("{bundleId}/bundle-items/{productId}")]
  [RequireResourcePermission<Product>(PermissionType.Edit, "bundleId")]
  public async Task<ActionResult<Product>> RemoveFromBundle(Guid bundleId, Guid productId) {
    var bundle = await mediator.Send(new RemoveFromBundleCommand { BundleId = bundleId, ProductId = productId, UpdatedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(bundle);
  }

  // ===== PRICING MANAGEMENT =====

  /// <summary>
  /// Get current pricing for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing/current")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<ProductPricing>> GetCurrentPricing(Guid id) {
    var pricing = await mediator.Send(new GetCurrentPricingQuery { ProductId = id });

    if (pricing == null) return NotFound();

    return Ok(pricing);
  }

  /// <summary>
  /// Get pricing history for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/pricing/history")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductPricing>>> GetPricingHistory(Guid id) {
    var pricing = await mediator.Send(new GetPricingHistoryQuery { ProductId = id });

    return Ok(pricing);
  }

  /// <summary>
  /// Set pricing for a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/pricing")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<ProductPricing>> SetPricing(Guid id, [FromBody] SetPricingRequest request) {
    var pricing = await mediator.Send(new SetPricingCommand { ProductId = id, Price = request.BasePrice, Currency = request.Currency, UpdatedBy = User.GetUserId() ?? Guid.Empty });

    return Ok(pricing);
  }

  // ===== SUBSCRIPTION MANAGEMENT =====

  /// <summary>
  /// Get subscription plans for a product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/subscription-plans")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProductSubscriptionPlan>>> GetSubscriptionPlans(Guid id) {
    var plans = await mediator.Send(new GetSubscriptionPlansQuery { ProductId = id });

    return Ok(plans);
  }

  /// <summary>
  /// Create subscription plan for a product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/subscription-plans")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<ProductSubscriptionPlan>> CreateSubscriptionPlan(
    Guid id,
    [FromBody] ProductSubscriptionPlan plan
  ) {
    plan.ProductId = id; // Ensure the product ID matches the route
    var createdPlan = await mediator.Send(new CreateSubscriptionPlanCommand { 
      Name = plan.Name, 
      Description = plan.Description, 
      Price = plan.Price, 
      Currency = plan.Currency, 
      Interval = plan.BillingInterval.ToString().ToLower(), 
      IntervalCount = plan.IntervalCount, 
      TrialDays = plan.TrialPeriodDays, 
      IsActive = plan.IsActive, 
      CreatedBy = User.GetUserId() ?? Guid.Empty,
    });

    return CreatedAtAction(nameof(GetSubscriptionPlan), new { planId = createdPlan.Plan?.Id }, createdPlan.Plan); // Fixed: use createdPlan.Plan.Id instead of createdPlan.Id
  }

  /// <summary>
  /// Get specific subscription plan (no specific permission - handled by service logic)
  /// </summary>
  [HttpGet("subscription-plans/{planId}")]
  public async Task<ActionResult<ProductSubscriptionPlan>> GetSubscriptionPlan(Guid planId) {
    var plan = await mediator.Send(new GetSubscriptionPlanQuery { PlanId = planId });

    if (plan == null) return NotFound();

    return Ok(plan);
  }

  // ===== USER ACCESS MANAGEMENT =====

  /// <summary>
  /// Check if user has access to product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/access/{userId}")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<bool>> HasUserAccess(Guid id, Guid userId) {
    var hasAccess = await mediator.Send(new HasUserAccessQuery { UserId = userId, ProductId = id });

    return Ok(hasAccess);
  }

  /// <summary>
  /// Get user product details (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/user-product/{userId}")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<UserProduct>> GetUserProduct(Guid id, Guid userId) {
    var userProduct = await mediator.Send(new GetUserProductQuery { UserId = userId, ProductId = id });

    if (userProduct == null) return NotFound();

    return Ok(userProduct);
  }

  /// <summary>
  /// Grant user access to product (resource-level edit permission)
  /// </summary>
  [HttpPost("{id}/access/{userId}")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult<UserProduct>> GrantUserAccess(
    Guid id, Guid userId,
    [FromBody] GrantAccessRequest request
  ) {
    var userProduct = await mediator.Send(
                        new GrantUserAccessCommand {
                          UserId = userId,
                          ProductId = id,
                          AcquisitionType = request.AcquisitionType,
                          ExpiresAt = request.ExpiresAt,
                          GrantedBy = User.GetUserId() ?? Guid.Empty,
                        }
                      );

    return Ok(userProduct);
  }

  /// <summary>
  /// Revoke user access to product (resource-level edit permission)
  /// </summary>
  [HttpDelete("{id}/access/{userId}")]
  [RequireResourcePermission<Product>(PermissionType.Edit)]
  public async Task<ActionResult> RevokeUserAccess(Guid id, Guid userId) {
    await mediator.Send(new RevokeUserAccessCommand { UserId = userId, ProductId = id });

    return NoContent();
  }

  // ===== ANALYTICS AND STATISTICS =====

  /// <summary>
  /// Get product count (content-type level read permission)
  /// </summary>
  [HttpGet("analytics/count")]
  [RequireContentTypePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<int>> GetProductCount(
    [FromQuery] Common.ProductType? type = null,
    [FromQuery] ContentStatus? status = null
  ) {
    var count = await mediator.Send(new GetProductCountQuery { Type = type, Status = status });

    return Ok(count);
  }

  /// <summary>
  /// Get user count for product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/analytics/user-count")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<int>> GetUserCountForProduct(Guid id) {
    var count = await mediator.Send(new GetUserCountForProductQuery { ProductId = id });

    return Ok(count);
  }

  /// <summary>
  /// Get total revenue for product (resource-level read permission)
  /// </summary>
  [HttpGet("{id}/analytics/revenue")]
  [RequireResourcePermission<Product>(PermissionType.Read)]
  public async Task<ActionResult<decimal>> GetTotalRevenueForProduct(Guid id) {
    var revenue = await mediator.Send(new GetTotalRevenueForProductQuery { ProductId = id });

    return Ok(revenue);
  }
}

// ===== DTOs FOR REQUEST BODIES =====
