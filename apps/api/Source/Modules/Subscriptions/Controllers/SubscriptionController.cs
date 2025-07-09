using GameGuild.Common.Application.Attributes;
using GameGuild.Common.Domain.Enums;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Subscriptions.Models;
using GameGuild.Modules.Subscriptions.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Modules.Subscriptions.Controllers;

/// <summary>
/// REST API controller for managing user subscriptions
/// Handles subscription lifecycle, billing, and user subscription access
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>
    /// Get current user's subscriptions
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<UserSubscription>>> GetMySubscriptions()
    {
        // Extract user ID from JWT token claims
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }

        var subscriptions = await subscriptionService.GetUserSubscriptionsAsync(userId);
        return Ok(subscriptions);
    }

    /// <summary>
    /// Get active subscription for current user
    /// </summary>
    [HttpGet("me/active")]
    public async Task<ActionResult<UserSubscription>> GetMyActiveSubscription()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }

        var subscription = await subscriptionService.GetActiveSubscriptionAsync(userId);
        if (subscription == null)
        {
            return NotFound(new { message = "No active subscription found" });
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Get subscription by ID (admin only)
    /// </summary>
    [HttpGet("{id}")]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<UserSubscription>> GetSubscription(Guid id)
    {
        var subscription = await subscriptionService.GetSubscriptionByIdAsync(id);
        if (subscription == null)
        {
            return NotFound();
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Get all subscriptions (admin only)
    /// </summary>
    [HttpGet]
    [RequireTenantPermission(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<UserSubscription>>> GetSubscriptions(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] SubscriptionStatus? status = null)
    {
        var subscriptions = await subscriptionService.GetSubscriptionsAsync(skip, take, status);
        return Ok(subscriptions);
    }

    /// <summary>
    /// Create a new subscription
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserSubscription>> CreateSubscription([FromBody] CreateSubscriptionDto createDto)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var subscription = await subscriptionService.CreateSubscriptionAsync(userId, createDto);
        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<UserSubscription>> CancelSubscription(Guid id)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }

        var subscription = await subscriptionService.CancelSubscriptionAsync(id, userId);
        if (subscription == null)
        {
            return NotFound(new { message = "Subscription not found or not owned by user" });
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Resume a canceled subscription
    /// </summary>
    [HttpPost("{id}/resume")]
    public async Task<ActionResult<UserSubscription>> ResumeSubscription(Guid id)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }

        var subscription = await subscriptionService.ResumeSubscriptionAsync(id, userId);
        if (subscription == null)
        {
            return NotFound(new { message = "Subscription not found or not owned by user" });
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Update subscription payment method
    /// </summary>
    [HttpPut("{id}/payment-method")]
    public async Task<ActionResult<UserSubscription>> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodDto updateDto)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var subscription = await subscriptionService.UpdatePaymentMethodAsync(id, userId, updateDto.PaymentMethodId);
        if (subscription == null)
        {
            return NotFound(new { message = "Subscription not found or not owned by user" });
        }

        return Ok(subscription);
    }
}
