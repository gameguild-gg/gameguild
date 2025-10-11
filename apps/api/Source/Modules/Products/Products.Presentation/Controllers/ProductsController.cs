using GameGuild.Modules.Products.Application.DTOs;
using GameGuild.Modules.Products.Application.Features.GetProduct;
using GameGuild.Modules.Products.Application.Features.ManageProduct;
using GameGuild.Modules.Products.Domain.Entities;
using GameGuild.Modules.Products.Domain.Enums;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Products.Presentation.Controllers;

/// <summary>
/// Products API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid productId, bool includePricing = true)
    {
        var query = new GetProductByIdQuery { ProductId = productId, IncludePricing = includePricing };
        var product = await _mediator.Send(query);

        if (product == null)
            return NotFound();

        return Ok(MapToDto(product));
    }

    /// <summary>
    /// Get products list
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] ProductType? type = null,
        [FromQuery] Guid? creatorId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isBundle = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "DESC")
    {
        var query = new GetProductsQuery
        {
            Type = type,
            CreatorId = creatorId,
            SearchTerm = searchTerm,
            IsBundle = isBundle,
            Skip = skip,
            Take = take,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var products = await _mediator.Send(query);
        return Ok(products.Select(MapToDto));
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var command = new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            ImageUrl = request.ImageUrl,
            Type = request.Type,
            IsBundle = request.IsBundle,
            CreatorId = request.CreatorId,
            BundleItems = request.BundleItems,
            ReferralCommissionPercentage = request.ReferralCommissionPercentage,
            MaxAffiliateDiscount = request.MaxAffiliateDiscount,
            AffiliateCommissionPercentage = request.AffiliateCommissionPercentage
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Retrieve the created product
        var getQuery = new GetProductByIdQuery { ProductId = result.ProductId!.Value };
        var createdProduct = await _mediator.Send(getQuery);

        return CreatedAtAction(nameof(GetProduct), new { productId = result.ProductId }, MapToDto(createdProduct!));
    }

    /// <summary>
    /// Update a product
    /// </summary>
    [HttpPut("{productId:guid}")]
    public async Task<ActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
    {
        var command = new UpdateProductCommand
        {
            ProductId = productId,
            Name = request.Name,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            ImageUrl = request.ImageUrl,
            Type = request.Type,
            IsBundle = request.IsBundle,
            BundleItems = request.BundleItems,
            ReferralCommissionPercentage = request.ReferralCommissionPercentage,
            MaxAffiliateDiscount = request.MaxAffiliateDiscount,
            AffiliateCommissionPercentage = request.AffiliateCommissionPercentage
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{productId:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid productId, bool softDelete = true, string? reason = null)
    {
        var command = new DeleteProductCommand
        {
            ProductId = productId,
            SoftDelete = softDelete,
            Reason = reason
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Restore a soft-deleted product
    /// </summary>
    [HttpPost("{productId:guid}/restore")]
    public async Task<ActionResult> RestoreProduct(Guid productId, string? reason = null)
    {
        var command = new RestoreProductCommand
        {
            ProductId = productId,
            Reason = reason
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Set product pricing
    /// </summary>
    [HttpPost("{productId:guid}/pricing")]
    public async Task<ActionResult> SetProductPricing(Guid productId, [FromBody] SetProductPricingRequest request)
    {
        var command = new SetProductPricingCommand
        {
            ProductId = productId,
            BasePrice = request.BasePrice,
            SalePrice = request.SalePrice,
            Currency = request.Currency,
            SaleStartDate = request.SaleStartDate,
            SaleEndDate = request.SaleEndDate,
            IsDefault = request.IsDefault
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Get product pricing
    /// </summary>
    [HttpGet("{productId:guid}/pricing")]
    public async Task<ActionResult<IEnumerable<ProductPricingDto>>> GetProductPricing(
        Guid productId,
        string? currency = null,
        bool? isDefault = null)
    {
        var query = new GetProductPricingQuery
        {
            ProductId = productId,
            Currency = currency,
            IsDefault = isDefault
        };

        var pricing = await _mediator.Send(query);
        return Ok(pricing.Select(MapPricingToDto));
    }

    /// <summary>
    /// Get product bundle items
    /// </summary>
    [HttpGet("{productId:guid}/bundle-items")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductBundleItems(Guid productId, bool includePricing = true)
    {
        var query = new GetProductBundleItemsQuery
        {
            ProductId = productId,
            IncludePricing = includePricing
        };

        var bundleItems = await _mediator.Send(query);
        return Ok(bundleItems.Select(MapToDto));
    }

    /// <summary>
    /// Check product access for current user
    /// </summary>
    [HttpGet("{productId:guid}/access")]
    public async Task<ActionResult<ProductAccessResult>> CheckProductAccess(Guid productId)
    {
        // Get current user ID from authentication context
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var query = new CheckProductAccessQuery
        {
            UserId = userId,
            ProductId = productId
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private static ProductDto MapToDto(GameGuild.Modules.Products.Domain.Entities.Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            ImageUrl = product.ImageUrl,
            Type = product.Type,
            IsBundle = product.IsBundle,
            CreatorId = product.CreatorId,
            BundleItems = product.GetBundleItemIds(),
            ReferralCommissionPercentage = product.ReferralCommissionPercentage,
            MaxAffiliateDiscount = product.MaxAffiliateDiscount,
            AffiliateCommissionPercentage = product.AffiliateCommissionPercentage,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Pricing = product.Pricing?.Select(MapPricingToDto).ToList()
        };
    }

    private static ProductPricingDto MapPricingToDto(ProductPricing pricing)
    {
        return new ProductPricingDto
        {
            Id = pricing.Id,
            ProductId = pricing.ProductId,
            BasePrice = pricing.BasePrice,
            SalePrice = pricing.SalePrice,
            Currency = pricing.Currency,
            SaleStartDate = pricing.SaleStartDate,
            SaleEndDate = pricing.SaleEndDate,
            IsDefault = pricing.IsDefault
        };
    }
}