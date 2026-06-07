using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
/// REST API controller for managing products
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/products")]
[Microsoft.AspNetCore.Http.Tags("products")]
[Authorize]
public class ProductsController(IMediator mediator) : BaseApiController
{
    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="includePricing">Include pricing information</param>
    /// <param name="includeUnpublished">Include drafts when authenticated</param>
    /// <returns>Product details</returns>
    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetProduct(
        Guid productId,
        [FromQuery] bool includePricing = true,
        [FromQuery] bool includeUnpublished = false)
    {
        var query = new GetProductByIdQuery(productId, includePricing, IncludeUnpublished: CanIncludeUnpublished(includeUnpublished));
        var product = await mediator.Send(query).ConfigureAwait(false);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Check if a product exists
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="includeUnpublished">Include drafts when authenticated</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ProductExists(Guid productId, [FromQuery] bool includeUnpublished = false)
    {
        var query = new ProductExistsQuery(productId, CanIncludeUnpublished(includeUnpublished));
        var exists = await mediator.Send(query).ConfigureAwait(false);
        return exists ? Ok() : NotFound();
    }

    /// <summary>
    /// Get pricing options for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="includeUnpublished">Include drafts when authenticated</param>
    /// <returns>List of pricing options</returns>
    [HttpGet("{productId:guid}/pricing")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductPricingDto>>> GetProductPricing(
        Guid productId,
        [FromQuery] bool includeUnpublished = false)
    {
        var query = new GetProductPricingQuery(productId, CanIncludeUnpublished(includeUnpublished));
        var pricing = await mediator.Send(query).ConfigureAwait(false);
        return Ok(pricing);
    }

    /// <summary>
    /// Get paginated list of products
    /// </summary>
    /// <param name="type">Filter by product type</param>
    /// <param name="creatorId">Filter by creator ID</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="isBundle">Filter by bundle status</param>
    /// <param name="includeUnpublished">Include drafts when authenticated</param>
    /// <param name="skip">Items to skip</param>
    /// <param name="take">Items to take</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <returns>Paginated products</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] ProductType? type = null,
        [FromQuery] Guid? creatorId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isBundle = null,
        [FromQuery] bool includeUnpublished = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "DESC")
    {
        var query = new GetProductsPagedQuery(
            type,
            creatorId,
            searchTerm,
            isBundle,
            CanIncludeUnpublished(includeUnpublished),
            skip,
            take,
            sortBy,
            sortDirection);
        var result = await mediator.Send(query).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [RequirePermission(ProductsPermission.Keys.Create)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.ShortDescription,
            request.ImageUrl,
            request.Type,
            request.IsBundle,
            request.CreatorId,
            request.BundleItems,
            request.ReferralCommissionPercentage,
            request.MaxAffiliateDiscount,
            request.AffiliateCommissionPercentage,
            request.TenantId
        );

        var product = await mediator.Send(command).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetProduct), new { productId = product.Id }, product);
    }

    /// <summary>
    /// Batch create multiple products
    /// </summary>
    /// <param name="request">Batch create request</param>
    /// <returns>Created products</returns>
    [HttpPost(":batch-create")]
    [RequirePermission(ProductsPermission.Keys.Create)]
    public async Task<ActionResult<List<ProductDto>>> BatchCreateProducts([FromBody] BatchCreateProductsRequest request)
    {
        var command = new BatchCreateProductsCommand(request.Products, request.TenantId);
        var products = await mediator.Send(command).ConfigureAwait(false);
        return StatusCode(StatusCodes.Status201Created, products);
    }

    /// <summary>
    /// Update an existing product (full update)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <returns>Updated product</returns>
    [HttpPut("{productId:guid}")]
    [RequirePermission(ProductsPermission.Keys.Update)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(
            productId,
            request.Name,
            request.Description,
            request.ShortDescription,
            request.ImageUrl,
            request.Type,
            request.IsBundle,
            request.BundleItems,
            request.ReferralCommissionPercentage,
            request.MaxAffiliateDiscount,
            request.AffiliateCommissionPercentage,
            request.ExpectedVersion
        );

        var product = await mediator.Send(command).ConfigureAwait(false);
        return Ok(product);
    }

    /// <summary>
    /// Partially update a product (PATCH)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Partial update request</param>
    /// <returns>Updated product</returns>
    [HttpPatch("{productId:guid}")]
    [RequirePermission(ProductsPermission.Keys.Update)]
    public async Task<ActionResult<ProductDto>> PatchProduct(Guid productId, [FromBody] PatchProductRequest request)
    {
        var command = new PatchProductCommand(
            productId,
            request.Name,
            request.Description,
            request.ShortDescription,
            request.ImageUrl,
            request.Type,
            request.IsBundle,
            request.BundleItems,
            request.ReferralCommissionPercentage,
            request.MaxAffiliateDiscount,
            request.AffiliateCommissionPercentage,
            request.ExpectedVersion
        );

        var product = await mediator.Send(command).ConfigureAwait(false);
        return Ok(product);
    }

    /// <summary>
    /// Activate a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Activated product</returns>
    [HttpPost("{productId:guid}:activate")]
    [RequirePermission(ProductsPermission.Keys.Update)]
    public async Task<ActionResult<ProductDto>> ActivateProduct(Guid productId)
    {
        var command = new ActivateProductCommand(productId);
        var product = await mediator.Send(command).ConfigureAwait(false);
        return Ok(product);
    }

    /// <summary>
    /// Deactivate a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Deactivated product</returns>
    [HttpPost("{productId:guid}:deactivate")]
    [RequirePermission(ProductsPermission.Keys.Update)]
    public async Task<ActionResult<ProductDto>> DeactivateProduct(Guid productId)
    {
        var command = new DeactivateProductCommand(productId);
        var product = await mediator.Send(command).ConfigureAwait(false);
        return Ok(product);
    }

    /// <summary>
    /// Archive a product (soft delete)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Archived product</returns>
    [HttpPost("{productId:guid}:archive")]
    [RequirePermission(ProductsPermission.Keys.Delete)]
    public async Task<ActionResult<ProductDto>> ArchiveProduct(Guid productId)
    {
        var command = new ArchiveProductCommand(productId);
        var product = await mediator.Send(command).ConfigureAwait(false);
        return Ok(product);
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="softDelete">Soft delete (default) or hard delete</param>
    /// <param name="reason">Deletion reason</param>
    /// <returns>No content</returns>
    [HttpDelete("{productId:guid}")]
    [RequirePermission(ProductsPermission.Keys.Delete)]
    public async Task<ActionResult> DeleteProduct(
        Guid productId,
        [FromQuery] bool softDelete = true,
        [FromQuery] string? reason = null)
    {
        var command = new DeleteProductCommand(productId, softDelete, reason);
        await mediator.Send(command).ConfigureAwait(false);
        return NoContent();
    }

    private bool CanIncludeUnpublished(bool includeUnpublished)
    {
        if (!includeUnpublished)
            return false;

        var identity = User?.Identity;
        return identity is { IsAuthenticated: true };
    }
}

/// <summary>
/// Request model for creating a product
/// </summary>
public sealed record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public ProductType Type { get; init; } = ProductType.Program;
    public bool IsBundle { get; init; }
    public Guid? CreatorId { get; init; }
    public List<Guid>? BundleItems { get; init; }
    public decimal ReferralCommissionPercentage { get; init; } = 30m;
    public decimal MaxAffiliateDiscount { get; init; }
    public decimal AffiliateCommissionPercentage { get; init; } = 30m;
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Request model for updating a product
/// </summary>
public sealed record UpdateProductRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public ProductType? Type { get; init; }
    public bool? IsBundle { get; init; }
    public List<Guid>? BundleItems { get; init; }
    public decimal? ReferralCommissionPercentage { get; init; }
    public decimal? MaxAffiliateDiscount { get; init; }
    public decimal? AffiliateCommissionPercentage { get; init; }
    public long? ExpectedVersion { get; init; }
}

/// <summary>
/// Request model for partial product update (PATCH)
/// </summary>
public sealed record PatchProductRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public ProductType? Type { get; init; }
    public bool? IsBundle { get; init; }
    public List<Guid>? BundleItems { get; init; }
    public decimal? ReferralCommissionPercentage { get; init; }
    public decimal? MaxAffiliateDiscount { get; init; }
    public decimal? AffiliateCommissionPercentage { get; init; }
    public long? ExpectedVersion { get; init; }
}

/// <summary>
/// Request model for batch creating products
/// </summary>
public sealed record BatchCreateProductsRequest
{
    public List<BatchProductCreateItem> Products { get; init; } = new();
    public Guid? TenantId { get; init; }
}

