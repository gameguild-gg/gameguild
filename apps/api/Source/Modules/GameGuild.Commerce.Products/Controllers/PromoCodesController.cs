using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Controller for managing promo codes
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/promo-codes")]
[Tags("promo-codes")]
[Authorize]
[RequirePermission(PromoCodesPermission.Keys.Read)]
public class PromoCodesController(IMediator mediator) : BaseApiController
{
    /// <summary>
    /// Get all promo codes (paginated) with optional status filter
    /// </summary>
    /// <param name="status">Filter by status ('active' for active codes only)</param>
    /// <param name="isActive">Filter by active flag</param>
    /// <param name="type">Filter by promo code type</param>
    /// <param name="productId">Filter by product ID</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="skip">Items to skip</param>
    /// <param name="take">Items to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PromoCodeDto>>> GetPromoCodes(
        [FromQuery] string? status = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] PromoCodeType? type = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        // If status=active is passed, filter for active codes
        var activeFilter = string.Equals(status, "active", StringComparison.OrdinalIgnoreCase) ? true : isActive;
        
        var query = new GetPromoCodesQuery(activeFilter, type, productId, searchTerm, skip, take);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Get a promo code by ID
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{promoCodeId:guid}")]
    public async Task<ActionResult<PromoCodeDto>> GetPromoCodeById(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCodeByIdQuery(promoCodeId);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Check if a promo code exists
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpHead("{promoCodeId:guid}")]
    public async Task<IActionResult> PromoCodeExists(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        var query = new PromoCodeExistsQuery(promoCodeId);
        var exists = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return exists ? Ok() : NotFound();
    }

    /// <summary>
    /// Get a promo code by its code string
    /// </summary>
    /// <param name="code">The promo code string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<PromoCodeDto>> GetPromoCodeByCode(
        string code,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCodeByCodeQuery(code);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Get usage statistics for a promo code
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{promoCodeId:guid}/usage")]
    public async Task<ActionResult<PromoCodeUsageDto>> GetPromoCodeUsage(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPromoCodeUsageQuery(promoCodeId);
        var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
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
        return CreatedAtAction(nameof(GetPromoCodeById), new { promoCodeId = result.Id }, result);
    }

    /// <summary>
    /// Update an existing promo code (full update)
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{promoCodeId:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCodeDto>> UpdatePromoCode(
        Guid promoCodeId,
        [FromBody] UpdatePromoCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdatePromoCodeCommand(
            promoCodeId,
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
    /// Partially update a promo code (PATCH)
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="request">Partial update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPatch("{promoCodeId:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCodeDto>> PatchPromoCode(
        Guid promoCodeId,
        [FromBody] PatchPromoCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new PatchPromoCodeCommand(
            promoCodeId,
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
    /// Activate a promo code
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{promoCodeId:guid}:activate")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCodeDto>> ActivatePromoCode(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivatePromoCodeCommand(promoCodeId);
        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a promo code
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{promoCodeId:guid}:deactivate")]
    [RequirePermission(PromoCodesPermission.Keys.Update)]
    public async Task<ActionResult<PromoCodeDto>> DeactivatePromoCode(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeactivatePromoCodeCommand(promoCodeId);
        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Delete a promo code
    /// </summary>
    /// <param name="promoCodeId">The promo code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{promoCodeId:guid}")]
    [RequirePermission(PromoCodesPermission.Keys.Delete)]
    public async Task<IActionResult> DeletePromoCode(
        Guid promoCodeId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeletePromoCodeCommand(promoCodeId);
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Validate a promo code
    /// </summary>
    [HttpPost(":validate")]
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
    [HttpPost(":apply")]
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
public sealed record CreatePromoCodeRequest(
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
public sealed record UpdatePromoCodeRequest(
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
/// Request to partially update a promo code (PATCH)
/// </summary>
public sealed record PatchPromoCodeRequest(
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
public sealed record ValidatePromoCodeRequest(
    string Code,
    decimal OrderAmount,
    Guid? ProductId = null
);

/// <summary>
/// Request to apply promo codes
/// </summary>
public sealed record ApplyPromoCodesRequest(
    decimal OrderAmount,
    List<string> PromoCodes,
    Guid? ProductId = null
);
