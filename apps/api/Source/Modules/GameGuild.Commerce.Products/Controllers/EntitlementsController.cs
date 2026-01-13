using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Controller for managing product entitlements
/// </summary>
[ApiController]
[Route("api/entitlements")]
[Authorize]
public class EntitlementsController(IEntitlementService entitlementService) : ControllerBase
{
    /// <summary>
    /// Check if current user has access to a product
    /// </summary>
    [HttpGet("check/{productId:guid}")]
    [RequirePermission(EntitlementsPermission.Keys.ReadSelf)]
    public async Task<ActionResult<EntitlementCheckResult>> CheckAccess(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await entitlementService.HasAccessAsync(
            GetUserId(),
            productId,
            cancellationToken).ConfigureAwait(false);

        return Ok(new EntitlementCheckResult(productId, hasAccess));
    }

    /// <summary>
    /// Check if current user has access to multiple products
    /// </summary>
    [HttpPost("check-multiple")]
    [RequirePermission(EntitlementsPermission.Keys.ReadSelf)]
    public async Task<ActionResult<IDictionary<Guid, bool>>> CheckMultipleAccess(
        [FromBody] CheckMultipleAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = await entitlementService.HasAccessAsync(
            GetUserId(),
            request.ProductIds,
            cancellationToken).ConfigureAwait(false);

        return Ok(results);
    }

    /// <summary>
    /// Get current user's entitlements
    /// </summary>
    [HttpGet("my-entitlements")]
    [RequirePermission(EntitlementsPermission.Keys.ReadSelf)]
    public async Task<ActionResult<IEnumerable<EntitlementInfoDto>>> GetMyEntitlements(
        CancellationToken cancellationToken = default)
    {
        var entitlements = await entitlementService.GetUserEntitlementsAsync(
            GetUserId(),
            cancellationToken).ConfigureAwait(false);

        return Ok(entitlements.Select(MapToDto));
    }

    /// <summary>
    /// Get entitlements for a specific user (admin only)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [RequirePermission(EntitlementsPermission.Keys.ReadAll)]
    public async Task<ActionResult<IEnumerable<EntitlementInfoDto>>> GetUserEntitlements(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entitlements = await entitlementService.GetUserEntitlementsAsync(
            userId,
            cancellationToken).ConfigureAwait(false);

        return Ok(entitlements.Select(MapToDto));
    }

    /// <summary>
    /// Grant entitlement to a user (admin only)
    /// </summary>
    [HttpPost("grant")]
    [RequirePermission(EntitlementsPermission.Keys.Grant)]
    public async Task<ActionResult<EntitlementInfoDto>> GrantEntitlement(
        [FromBody] GrantEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await entitlementService.GrantEntitlementAsync(
            request.UserId,
            request.ProductId,
            request.AcquisitionType,
            request.PricePaid,
            request.Currency,
            request.ExpiresAt,
            orderId: null,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        var entitlements = await entitlementService.GetUserEntitlementsAsync(
            request.UserId, cancellationToken).ConfigureAwait(false);
        var entitlement = entitlements.FirstOrDefault(e => e.ProductId == request.ProductId);

        return Ok(entitlement != null ? MapToDto(entitlement) : null);
    }

    /// <summary>
    /// Revoke entitlement from a user (admin only)
    /// </summary>
    [HttpPost("revoke")]
    [RequirePermission(EntitlementsPermission.Keys.Revoke)]
    public async Task<IActionResult> RevokeEntitlement(
        [FromBody] RevokeEntitlementRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await entitlementService.RevokeEntitlementAsync(
            request.UserId,
            request.ProductId,
            request.Reason,
            cancellationToken).ConfigureAwait(false);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Get entitlements expiring soon (admin only)
    /// </summary>
    [HttpGet("expiring")]
    [RequirePermission(EntitlementsPermission.Keys.ReadAll)]
    public async Task<ActionResult<IEnumerable<EntitlementInfoDto>>> GetExpiringEntitlements(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var entitlements = await entitlementService.GetExpiringEntitlementsAsync(
            days,
            cancellationToken).ConfigureAwait(false);

        return Ok(entitlements.Select(MapToDto));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static EntitlementInfoDto MapToDto(EntitlementInfo info) => new(
        info.ProductId,
        info.ProductName,
        info.Status.ToString(),
        info.AcquisitionType.ToString(),
        info.AccessStartDate,
        info.AccessEndDate,
        info.IsSubscription,
        info.SubscriptionStatus?.ToString(),
        info.PricePaid,
        info.Currency);
}

/// <summary>Result of an entitlement check</summary>
public record EntitlementCheckResult(Guid ProductId, bool HasAccess);

/// <summary>Request to check multiple product access</summary>
public record CheckMultipleAccessRequest(IEnumerable<Guid> ProductIds);

/// <summary>Request to grant an entitlement</summary>
public record GrantEntitlementRequest(
    Guid UserId,
    Guid ProductId,
    ProductAcquisitionType AcquisitionType,
    decimal PricePaid = 0,
    string Currency = "USD",
    DateTime? ExpiresAt = null);

/// <summary>Request to revoke an entitlement</summary>
public record RevokeEntitlementRequest(
    Guid UserId,
    Guid ProductId,
    string? Reason = null);

/// <summary>Entitlement info DTO</summary>
public record EntitlementInfoDto(
    Guid ProductId,
    string ProductName,
    string Status,
    string AcquisitionType,
    DateTime? AccessStartDate,
    DateTime? AccessEndDate,
    bool IsSubscription,
    string? SubscriptionStatus,
    decimal PricePaid,
    string Currency);
