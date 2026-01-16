using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Controller for managing user entitlements (resource-oriented)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/users")]
[Tags("users/entitlements")]
[Authorize]
public class UserEntitlementsController(IEntitlementService entitlementService) : ControllerBase
{
    /// <summary>
    /// Get current user's entitlements
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entitlements for the current user</returns>
    [HttpGet("me/entitlements")]
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
    /// <param name="userId">The user ID to get entitlements for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entitlements for the specified user</returns>
    [HttpGet("{userId:guid}/entitlements")]
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
