using GameGuild.Modules.Subscriptions.Models;
using GameGuild.Modules.Subscriptions.Services;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Subscriptions.Controllers;

/// <summary>
/// REST API controller for managing user subscriptions
/// </summary>
/// <remarks>
/// Handles subscription lifecycle operations including creation, cancellation, resumption,
/// billing management, and user subscription access control. Provides both user-specific
/// and administrative endpoints for subscription management.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase {

  /// <summary>
  /// Retrieves all subscriptions for the authenticated user
  /// </summary>
  /// <returns>Collection of user's subscriptions with plan details</returns>
  /// <response code="200">Returns the user's subscriptions</response>
  /// <response code="401">If user is not authenticated or token is invalid</response>
  [HttpGet("me")]
  [ProducesResponseType(typeof(IEnumerable<UserSubscription>), 200)]
  [ProducesResponseType(401)]
  public async Task<ActionResult<IEnumerable<UserSubscription>>> GetMySubscriptions() {
    // Extract user ID from JWT token claims
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      return Unauthorized(new { message = "User ID not found in token" });
    }

    var subscriptions = await subscriptionService.GetUserSubscriptionsAsync(userId);
    return Ok(subscriptions);
  }

  /// <summary>
  /// Retrieves the active subscription for the authenticated user
  /// </summary>
  /// <returns>Active subscription with plan details, or 404 if none found</returns>
  /// <response code="200">Returns the user's active subscription</response>
  /// <response code="401">If user is not authenticated or token is invalid</response>
  /// <response code="404">If user has no active subscription</response>
  [HttpGet("me/active")]
  [ProducesResponseType(typeof(UserSubscription), 200)]
  [ProducesResponseType(401)]
  [ProducesResponseType(404)]
  public async Task<ActionResult<UserSubscription>> GetMyActiveSubscription() {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      return Unauthorized(new { message = "User ID not found in token" });
    }

    var subscription = await subscriptionService.GetActiveSubscriptionAsync(userId);
    if (subscription is null) {
      return NotFound(new { message = "No active subscription found" });
    }

    return Ok(subscription);
  }

  /// <summary>
  /// Retrieves a specific subscription by ID (administrative access required)
  /// </summary>
  /// <param name="id">The unique identifier of the subscription</param>
  /// <returns>Subscription details with plan and user information</returns>
  /// <response code="200">Returns the requested subscription</response>
  /// <response code="403">If user lacks read permissions</response>
  /// <response code="404">If subscription is not found</response>
  [HttpGet("{id}")]
  [RequireTenantPermission(PermissionType.Read)]
  [ProducesResponseType(typeof(UserSubscription), 200)]
  [ProducesResponseType(403)]
  [ProducesResponseType(404)]
  public async Task<ActionResult<UserSubscription>> GetSubscription(Guid id) {
    var subscription = await subscriptionService.GetSubscriptionByIdAsync(id);
    if (subscription is null) {
      return NotFound();
    }

    return Ok(subscription);
  }

  /// <summary>
  /// Retrieves paginated list of all subscriptions (administrative access required)
  /// </summary>
  /// <param name="skip">Number of records to skip for pagination</param>
  /// <param name="take">Number of records to take (max 50)</param>
  /// <param name="status">Optional status filter for subscriptions</param>
  /// <returns>Paginated collection of subscriptions</returns>
  /// <response code="200">Returns paginated subscription list</response>
  /// <response code="403">If user lacks read permissions</response>
  [HttpGet]
  [RequireTenantPermission(PermissionType.Read)]
  [ProducesResponseType(typeof(IEnumerable<UserSubscription>), 200)]
  [ProducesResponseType(403)]
  public async Task<ActionResult<IEnumerable<UserSubscription>>> GetSubscriptions(
      [FromQuery] int skip = 0,
      [FromQuery] int take = 50,
      [FromQuery] SubscriptionStatus? status = null) {

    var subscriptions = await subscriptionService.GetSubscriptionsAsync(skip, take, status);
    return Ok(subscriptions);
  }

  /// <summary>
  /// Creates a new subscription for the authenticated user
  /// </summary>
  /// <param name="createDto">Subscription creation configuration</param>
  /// <returns>Created subscription with full details</returns>
  /// <response code="201">Subscription created successfully</response>
  /// <response code="400">If request data is invalid</response>
  /// <response code="401">If user is not authenticated</response>
  [HttpPost]
  [ProducesResponseType(typeof(UserSubscription), 201)]
  [ProducesResponseType(400)]
  [ProducesResponseType(401)]
  public async Task<ActionResult<UserSubscription>> CreateSubscription([FromBody] CreateSubscriptionDto createDto) {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      return Unauthorized(new { message = "User ID not found in token" });
    }

    if (!ModelState.IsValid) {
      return BadRequest(ModelState);
    }

    // Create subscription through service layer
    var subscription = await subscriptionService.CreateSubscriptionAsync(userId, createDto);

    return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
  }

  /// <summary>
  /// Cancels the specified subscription for the authenticated user
  /// </summary>
  /// <param name="id">The unique identifier of the subscription to cancel</param>
  /// <returns>Cancelled subscription details</returns>
  /// <response code="200">Subscription cancelled successfully</response>
  /// <response code="401">If user is not authenticated</response>
  /// <response code="404">If subscription is not found or not owned by user</response>
  [HttpPost("{id}/cancel")]
  [ProducesResponseType(typeof(UserSubscription), 200)]
  [ProducesResponseType(401)]
  [ProducesResponseType(404)]
  public async Task<ActionResult<UserSubscription>> CancelSubscription(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      return Unauthorized(new { message = "User ID not found in token" });
    }

    // Cancel subscription with ownership validation
    var subscription = await subscriptionService.CancelSubscriptionAsync(id, userId);
    if (subscription is null) {
      return NotFound(new { message = "Subscription not found or not owned by user" });
    }

    return Ok(subscription);
  }

  /// <summary>
  /// Resumes a cancelled subscription for the authenticated user
  /// </summary>
  /// <param name="id">The unique identifier of the subscription to resume</param>
  /// <returns>Resumed subscription details</returns>
  /// <response code="200">Subscription resumed successfully</response>
  /// <response code="401">If user is not authenticated</response>
  /// <response code="404">If subscription is not found or not owned by user</response>
  [HttpPost("{id}/resume")]
  [ProducesResponseType(typeof(UserSubscription), 200)]
  [ProducesResponseType(401)]
  [ProducesResponseType(404)]
  public async Task<ActionResult<UserSubscription>> ResumeSubscription(Guid id) {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      return Unauthorized(new { message = "User ID not found in token" });
    }

    // Resume subscription with ownership validation
    var subscription = await subscriptionService.ResumeSubscriptionAsync(id, userId);
    if (subscription is null) {
      return NotFound(new { message = "Subscription not found or not owned by user" });
    }

    return Ok(subscription);
  }

  /// <summary>
  /// Updates the payment method for the specified subscription
  /// </summary>
  /// <param name="id">The unique identifier of the subscription</param>
  /// <param name="updateDto">Payment method update configuration</param>
  /// <returns>Updated subscription details</returns>
  /// <response code="200">Payment method updated successfully</response>
  /// <response code="400">If request data is invalid</response>
  /// <response code="401">If user is not authenticated</response>
  /// <response code="404">If subscription is not found or not owned by user</response>
  [HttpPut("{id}/payment-method")]
  [ProducesResponseType(typeof(UserSubscription), 200)]
  [ProducesResponseType(400)]
  [ProducesResponseType(401)]
  [ProducesResponseType(404)]
  public async Task<ActionResult<UserSubscription>> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodDto updateDto) {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) {
      return Unauthorized(new { message = "User ID not found in token" });
    }

    if (!ModelState.IsValid) {
      return BadRequest(ModelState);
    }

    // Update payment method with ownership validation
    var subscription = await subscriptionService.UpdatePaymentMethodAsync(id, userId, updateDto.PaymentMethodId);
    if (subscription is null) {
      return NotFound(new { message = "Subscription not found or not owned by user" });
    }

    return Ok(subscription);
  }
}
