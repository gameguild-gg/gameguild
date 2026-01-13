using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Controller for managing promo codes
/// </summary>
[ApiController]
[Route("api/promo-codes")]
[Authorize]
[RequirePermission(PromoCodesPermission.Keys.Read)]
public class PromoCodesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get all promo codes (paginated)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PromoCodeDto>>> GetPromoCodes(
        [FromQuery] bool? isActive = null,
        [FromQuery] PromoCodeType? type = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCodesQuery(isActive, type, productId, searchTerm, skip, take);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Get active promo codes
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PromoCodeDto>>> GetActivePromoCodes(
        [FromQuery] Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetActivePromoCodesQuery(productId);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Get a promo code by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromoCodeDto>> GetPromoCodeById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCodeByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new promo code
    /// </summary>
    [HttpPost]
    [RequirePermission(PromoCodesPermission.Keys.Create)]
    public async Task<ActionResult<PromoCodeDto>> CreatePromoCode(
        [FromBody] CreatePromoCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePromoCodeCommand(
            request.Code,
            request.Name,
            request.Description,
            request.Type,
            request.DiscountPercentage,
            request.DiscountAmount,
            request.Currency,
            request.MinimumOrderAmount,
            request.MaxUses,
            request.MaxUsesPerUser,
            request.ValidFrom,
            request.ValidUntil,
            request.IsActive,
            request.IsExclusive,
            request.StackingPriority,
            request.ProductId,
            GetUserId()
        );

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetPromoCodeById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing promo code
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCodeDto>> UpdatePromoCode(
        Guid id,
        [FromBody] UpdatePromoCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdatePromoCodeCommand(
            id,
            request.Name,
            request.Description,
            request.Type,
            request.DiscountPercentage,
            request.DiscountAmount,
            request.Currency,
            request.MinimumOrderAmount,
            request.MaxUses,
            request.MaxUsesPerUser,
            request.ValidFrom,
            request.ValidUntil,
            request.IsActive,
            request.IsExclusive,
            request.StackingPriority,
            request.ProductId
        );

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Delete a promo code
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Delete)]
    public async Task<IActionResult> DeletePromoCode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeletePromoCodeCommand(id);
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Validate a promo code
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<PromoCodeValidationResult>> ValidatePromoCode(
        [FromBody] ValidatePromoCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ValidatePromoCodeCommand(
            request.Code,
            request.OrderAmount,
            request.ProductId,
            GetUserId()
        );

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Apply promo codes to an order
    /// </summary>
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<ActionResult<PromoCodeApplicationResult>> ApplyPromoCodes(
        [FromBody] ApplyPromoCodesRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ApplyPromoCodesCommand(
            request.OrderAmount,
            request.PromoCodes,
            request.ProductId,
            GetUserId()
        );

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

/// <summary>
/// Request to create a promo code
/// </summary>
public record CreatePromoCodeRequest(
    string Code,
    string Name,
    string? Description = null,
    PromoCodeType Type = PromoCodeType.PercentageOff,
    decimal? DiscountPercentage = null,
    decimal? DiscountAmount = null,
    string Currency = "USD",
    decimal? MinimumOrderAmount = null,
    int? MaxUses = null,
    int? MaxUsesPerUser = null,
    DateTime? ValidFrom = null,
    DateTime? ValidUntil = null,
    bool IsActive = true,
    bool IsExclusive = false,
    int StackingPriority = 0,
    Guid? ProductId = null
);

/// <summary>
/// Request to update a promo code
/// </summary>
public record UpdatePromoCodeRequest(
    string? Name = null,
    string? Description = null,
    PromoCodeType? Type = null,
    decimal? DiscountPercentage = null,
    decimal? DiscountAmount = null,
    string? Currency = null,
    decimal? MinimumOrderAmount = null,
    int? MaxUses = null,
    int? MaxUsesPerUser = null,
    DateTime? ValidFrom = null,
    DateTime? ValidUntil = null,
    bool? IsActive = null,
    bool? IsExclusive = null,
    int? StackingPriority = null,
    Guid? ProductId = null
);

/// <summary>
/// Request to validate a promo code
/// </summary>
public record ValidatePromoCodeRequest(
    string Code,
    decimal OrderAmount,
    Guid? ProductId = null
);

/// <summary>
/// Request to apply promo codes
/// </summary>
public record ApplyPromoCodesRequest(
    decimal OrderAmount,
    List<string> PromoCodes,
    Guid? ProductId = null
);
