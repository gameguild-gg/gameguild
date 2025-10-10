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
/// Promo Codes API controller
/// </summary>
[ApiController]
[Route("api/promo-codes")]
[Authorize]
public class PromoCodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromoCodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get promo code by code
    /// </summary>
    [HttpGet("{code}")]
    public async Task<ActionResult<PromoCodeDto>> GetPromoCode(string code, bool includeUsages = false)
    {
        var query = new GetPromoCodeQuery { Code = code, IncludeUsages = includeUsages };
        var promoCode = await _mediator.Send(query);

        if (promoCode == null)
            return NotFound();

        return Ok(MapToDto(promoCode));
    }

    /// <summary>
    /// Get promo codes list
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PromoCodeDto>>> GetPromoCodes(
        [FromQuery] PromoCodeType? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isExpired = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetPromoCodesQuery
        {
            Type = type,
            IsActive = isActive,
            IsExpired = isExpired,
            SearchTerm = searchTerm,
            Skip = skip,
            Take = take
        };

        var promoCodes = await _mediator.Send(query);
        return Ok(promoCodes.Select(MapToDto));
    }

    /// <summary>
    /// Create a new promo code
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PromoCodeDto>> CreatePromoCode([FromBody] CreatePromoCodeRequest request)
    {
        var command = new CreatePromoCodeCommand
        {
            Code = request.Code,
            Description = request.Description,
            Type = request.Type,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = request.DiscountAmount,
            Currency = request.Currency,
            StartDate = request.StartDate,
            ExpiryDate = request.ExpiryDate,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            IsActive = request.IsActive,
            ProductIds = request.ProductIds
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Retrieve the created promo code
        var getQuery = new GetPromoCodeQuery { Code = request.Code };
        var createdPromoCode = await _mediator.Send(getQuery);

        return CreatedAtAction(nameof(GetPromoCode), new { code = request.Code }, MapToDto(createdPromoCode!));
    }

    /// <summary>
    /// Update a promo code
    /// </summary>
    [HttpPut("{promoCodeId:guid}")]
    public async Task<ActionResult> UpdatePromoCode(Guid promoCodeId, [FromBody] CreatePromoCodeRequest request)
    {
        var command = new UpdatePromoCodeCommand
        {
            PromoCodeId = promoCodeId,
            Code = request.Code,
            Description = request.Description,
            Type = request.Type,
            DiscountPercentage = request.DiscountPercentage,
            DiscountAmount = request.DiscountAmount,
            Currency = request.Currency,
            StartDate = request.StartDate,
            ExpiryDate = request.ExpiryDate,
            MaxUses = request.MaxUses,
            MaxUsesPerUser = request.MaxUsesPerUser,
            IsActive = request.IsActive,
            ProductIds = request.ProductIds
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    /// <summary>
    /// Validate promo code for a product
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<PromoCodeValidationResult>> ValidatePromoCode([FromBody] ApplyPromoCodeRequest request)
    {
        // Get current user ID from authentication context
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var query = new ValidatePromoCodeQuery
        {
            Code = request.Code,
            UserId = userId,
            ProductId = request.ProductId,
            ProductPrice = request.OriginalPrice
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Apply promo code to a purchase
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<ApplyPromoCodeResult>> ApplyPromoCode([FromBody] ApplyPromoCodeRequest request)
    {
        // Get current user ID from authentication context
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new ApplyPromoCodeCommand
        {
            Code = request.Code,
            UserId = userId,
            ProductId = request.ProductId,
            OriginalPrice = request.OriginalPrice,
            Currency = request.Currency
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result);
    }

    private static PromoCodeDto MapToDto(PromoCode promoCode)
    {
        return new PromoCodeDto
        {
            Id = promoCode.Id,
            Code = promoCode.Code,
            Description = promoCode.Description,
            Type = promoCode.Type,
            DiscountPercentage = promoCode.DiscountPercentage,
            DiscountAmount = promoCode.DiscountAmount,
            Currency = promoCode.Currency,
            StartDate = promoCode.StartDate,
            ExpiryDate = promoCode.ExpiryDate,
            MaxUses = promoCode.MaxUses,
            MaxUsesPerUser = promoCode.MaxUsesPerUser,
            IsActive = promoCode.IsActive,
            CurrentUses = promoCode.PromoCodeUses?.Count ?? 0,
            IsCurrentlyValid = promoCode.IsCurrentlyValid(),
            CreatedAt = promoCode.CreatedAt
        };
    }
}