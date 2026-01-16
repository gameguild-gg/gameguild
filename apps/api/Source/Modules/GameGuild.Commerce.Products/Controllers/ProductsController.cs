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
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/products")]
[Tags("products")]
[Authorize]
public class ProductsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="includePricing">Include pricing information</param>
    /// <returns>Product details</returns>
    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid productId, [FromQuery] bool includePricing = true)
    {
        var query = new GetProductByIdQuery(productId, includePricing);
        var product = await mediator.Send(query);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// Get paginated list of products
    /// </summary>
    /// <param name="type">Filter by product type</param>
    /// <param name="creatorId">Filter by creator ID</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="isBundle">Filter by bundle status</param>
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
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "DESC")
    {
        var query = new GetProductsPagedQuery(type, creatorId, searchTerm, isBundle, skip, take, sortBy, sortDirection);
        var result = await mediator.Send(query);
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

        var product = await mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { productId = product.Id }, product);
    }

    /// <summary>
    /// Update an existing product
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

        var product = await mediator.Send(command);
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
        await mediator.Send(command);
        return NoContent();
    }
}

/// <summary>
/// Request model for creating a product
/// </summary>
public record CreateProductRequest
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
public record UpdateProductRequest
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
