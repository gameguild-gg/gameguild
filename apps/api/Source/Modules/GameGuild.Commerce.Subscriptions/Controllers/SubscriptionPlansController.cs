using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Commerce.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Subscription Plans API Controller - RESTful API for subscription plan management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Tags("subscriptions-plans")]
public sealed class SubscriptionPlansController(ISender sender) : ControllerBase
{
    #region Collection Operations - /v1/subscription-plans

    /// <summary>
    ///     Create a new subscription plan
    /// </summary>
    /// <param name="body">Subscription plan creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created subscription plan</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans")]
    [EndpointSummary("Create a new subscription plan")]
    [EndpointDescription("Creates a new subscription plan with the provided information.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscriptionPlan([FromBody] CreatePlanRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await sender.Send(new CreateSubscriptionPlanCommand(body.Name, body.Slug, body.MonthlyPriceInCents, body.Currency, body.Description), ct);

        return CreatedAtAction(nameof(GetSubscriptionPlanById), new { planId = id }, new { id, body.Name, body.Slug });
    }

    /// <summary>
    ///     Get subscription plans with pagination and filtering
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of plans per page (default: 20, max: 100)</param>
    /// <param name="activeOnly">Filter to only active plans</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="isFeatured">Filter by featured status (use featured=true to get featured plans)</param>
    /// <param name="q">Search term for filtering by name, description, or features</param>
    /// <param name="slug">Filter by exact slug match</param>
    /// <param name="minPrice">Minimum price in cents for price range filtering</param>
    /// <param name="maxPrice">Maximum price in cents for price range filtering</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of subscription plans</returns>
    [HttpGet("v{version:apiVersion}/subscription-plans")]
    [EndpointSummary("Get subscription plans with pagination and filtering")]
    [EndpointDescription("Retrieves a paginated list of subscription plans with optional filtering. Use query parameters: featured=true for featured plans, q=searchTerm for search, slug=value for slug lookup, minPrice/maxPrice for price range.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionPlans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool activeOnly = false,
        [FromQuery] bool? isActive = null,
        [FromQuery(Name = "featured")] bool? isFeatured = null,
        [FromQuery] string? q = null,
        [FromQuery] string? slug = null,
        [FromQuery] long? minPrice = null,
        [FromQuery] long? maxPrice = null,
        CancellationToken ct = default
    )
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // If slug is specified, return single plan lookup
        if (!string.IsNullOrEmpty(slug))
        {
            var plan = await sender.Send(new GetSubscriptionPlanBySlugQuery(slug), ct);
            return plan is null ? NotFound() : Ok(plan);
        }

        // If price range is specified, filter by price
        if (minPrice.HasValue || maxPrice.HasValue)
        {
            var priceResult = await sender.Send(new GetSubscriptionPlansByPriceRangeQuery(minPrice ?? 0, maxPrice ?? long.MaxValue), ct);
            return Ok(priceResult);
        }

        // If activeOnly is specified, use the simple query, otherwise use the paginated query
        if (activeOnly && page == 1 && pageSize == 20 && !isActive.HasValue && !isFeatured.HasValue && string.IsNullOrEmpty(q))
        {
            var result = await sender.Send(new GetActiveSubscriptionPlansQuery(), ct);
            return Ok(result);
        }

        // If only featured filter is requested without other pagination
        if (isFeatured == true && page == 1 && pageSize == 20 && !isActive.HasValue && string.IsNullOrEmpty(q))
        {
            var featuredResult = await sender.Send(new GetFeaturedSubscriptionPlansQuery(), ct);
            return Ok(featuredResult);
        }

        // If search term is provided without pagination, use search query
        if (!string.IsNullOrEmpty(q) && page == 1 && pageSize == 20 && !isActive.HasValue && !isFeatured.HasValue)
        {
            var searchResult = await sender.Send(new SearchSubscriptionPlansQuery(q), ct);
            return Ok(searchResult);
        }

        var pagedResult = await sender.Send(new GetPagedSubscriptionPlansQuery(page, pageSize, isActive, isFeatured, q), ct);
        return Ok(pagedResult);
    }

    /// <summary>
    ///     Compare subscription plans
    /// </summary>
    /// <param name="body">Plan comparison request with base plan and comparison plan IDs</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Plan comparison results</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans:compare")]
    [EndpointSummary("Compare subscription plans")]
    [EndpointDescription("Compares multiple subscription plans side by side. Custom action per Google API guidelines.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompareSubscriptionPlans([FromBody] ComparePlansRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await sender.Send(new CompareSubscriptionPlansQuery(body.BasePlanId, body.ComparePlanIds), ct);
        return Ok(result);
    }

    #endregion

    #region Individual Item Operations - /v1/subscription-plans/{planId}

    /// <summary>
    ///     Check if subscription plan exists by ID
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 if exists, 404 if not</returns>
    [HttpHead("v{version:apiVersion}/subscription-plans/{planId:guid}")]
    [EndpointSummary("Check if subscription plan exists by ID")]
    [EndpointDescription("Checks if a subscription plan exists by ID without returning the body.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckSubscriptionPlanExistsById(Guid planId, CancellationToken ct)
    {
        var plan = await sender.Send(new GetSubscriptionPlanByIdQuery(planId), ct);
        return plan is null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Get subscription plan by ID
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription plan details</returns>
    [HttpGet("v{version:apiVersion}/subscription-plans/{planId:guid}")]
    [EndpointSummary("Get subscription plan by ID")]
    [EndpointDescription("Retrieves detailed information for a specific subscription plan.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionPlanById(Guid planId, CancellationToken ct)
    {
        var plan = await sender.Send(new GetSubscriptionPlanByIdQuery(planId), ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    /// <summary>
    ///     Delete subscription plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("v{version:apiVersion}/subscription-plans/{planId:guid}")]
    [EndpointSummary("Delete subscription plan")]
    [EndpointDescription("Deletes a subscription plan by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscriptionPlan(Guid planId, CancellationToken ct)
    {
        await sender.Send(new DeleteSubscriptionPlanCommand(planId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Full update of a subscription plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Full plan update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("v{version:apiVersion}/subscription-plans/{planId:guid}")]
    [EndpointSummary("Full update subscription plan")]
    [EndpointDescription("Performs a full replacement of subscription plan data. All fields will be updated.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutSubscriptionPlan(Guid planId, [FromBody] PutSubscriptionPlanRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new FullUpdateSubscriptionPlanCommand(
            planId,
            body.Name,
            body.Slug,
            body.Description,
            body.MonthlyPriceInCents,
            body.AnnualPriceInCents,
            body.MaxUsers,
            body.MaxStorageMb,
            body.MaxApiCallsPerMonth,
            body.HasPrioritySupport,
            body.HasAdvancedAnalytics,
            body.HasCustomBranding,
            body.Features,
            body.SortOrder), ct);
        return NoContent();
    }

    /// <summary>
    ///     Get subscription plan usage statistics
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Usage statistics for the plan</returns>
    [HttpGet("v{version:apiVersion}/subscription-plans/{planId:guid}/usage")]
    [EndpointSummary("Get subscription plan usage statistics")]
    [EndpointDescription("Retrieves usage statistics for a specific subscription plan.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionPlanUsage(Guid planId, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetSubscriptionPlanUsageStatisticsQuery(planId), ct));
    }

    /// <summary>
    ///     Get suggested plan upgrades
    /// </summary>
    /// <param name="planId">Current plan ID</param>
    /// <param name="users">Number of users</param>
    /// <param name="storageMb">Storage requirements in MB</param>
    /// <param name="apiCalls">API calls per month</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Suggested upgrade plans</returns>
    [HttpGet("v{version:apiVersion}/subscription-plans/{planId:guid}/suggest-upgrades")]
    [EndpointSummary("Get suggested plan upgrades")]
    [EndpointDescription("Suggests upgrade plans based on current usage requirements.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestedPlanUpgrades(Guid planId, [FromQuery] int users, [FromQuery] long storageMb, [FromQuery] long apiCalls, CancellationToken ct)
    {
        return Ok(await sender.Send(new SuggestSubscriptionPlanUpgradesQuery(planId, users, storageMb, apiCalls), ct));
    }

    /// <summary>
    ///     Calculate pricing for a subscription plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="tenantId">Optional tenant ID for tenant-specific pricing</param>
    /// <param name="discountCode">Optional discount code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed pricing breakdown</returns>
    [HttpGet("v{version:apiVersion}/subscription-plans/{planId:guid}/pricing")]
    [EndpointSummary("Calculate pricing for a subscription plan")]
    [EndpointDescription("Calculates the total cost for a subscription plan including all applicable taxes, fees, and discounts.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateSubscriptionPlanPricing(Guid planId, [FromQuery] Guid? tenantId, [FromQuery] string? discountCode, CancellationToken ct)
    {
        return Ok(await sender.Send(new CalculatePricingQuery(planId, tenantId, discountCode), ct));
    }

    /// <summary>
    ///     Validate subscription plan limits
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Validation request with usage requirements</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:validate-limits")]
    [EndpointSummary("Validate subscription plan limits")]
    [EndpointDescription("Validates whether the specified usage fits within the plan limits. Custom action per Google API guidelines.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateSubscriptionPlanLimits(Guid planId, [FromBody] ValidateLimitsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await sender.Send(new ValidateSubscriptionPlanLimitsQuery(planId, body.Users, body.StorageMb, body.ApiCalls), ct);
        return Ok(result);
    }

    /// <summary>
    ///     Partially update subscription plan details
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("v{version:apiVersion}/subscription-plans/{planId:guid}/details")]
    [EndpointSummary("Partially update subscription plan details")]
    [EndpointDescription("Updates specific fields of a subscription plan's details.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubscriptionPlanDetails(Guid planId, [FromBody] UpdateDetailsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateSubscriptionPlanCommand(planId, body.Name, body.Description, body.SortOrder), ct);
        return NoContent();
    }

    /// <summary>
    ///     Update subscription plan pricing
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Pricing update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("v{version:apiVersion}/subscription-plans/{planId:guid}/pricing")]
    [EndpointSummary("Update subscription plan pricing")]
    [EndpointDescription("Updates the pricing for a subscription plan.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubscriptionPlanPricing(Guid planId, [FromBody] UpdatePricingRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateSubscriptionPlanPricingCommand(planId, body.MonthlyPriceInCents, body.AnnualPriceInCents), ct);
        return NoContent();
    }

    /// <summary>
    ///     Update subscription plan limits
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Limits update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("v{version:apiVersion}/subscription-plans/{planId:guid}/limits")]
    [EndpointSummary("Update subscription plan limits")]
    [EndpointDescription("Updates the limits for a subscription plan.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubscriptionPlanLimits(Guid planId, [FromBody] UpdateLimitsRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateSubscriptionPlanLimitsCommand(planId, body.MaxUsers, body.MaxStorageMb, body.MaxApiCallsPerMonth), ct);
        return NoContent();
    }

    /// <summary>
    ///     Update subscription plan features
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Features update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("v{version:apiVersion}/subscription-plans/{planId:guid}/features")]
    [EndpointSummary("Update subscription plan features")]
    [EndpointDescription("Updates the features for a subscription plan.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubscriptionPlanFeatures(Guid planId, [FromBody] UpdateFeaturesRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new UpdateSubscriptionPlanFeaturesCommand(planId, body.HasPrioritySupport, body.HasAdvancedAnalytics, body.HasCustomBranding, body.Features), ct);
        return NoContent();
    }

    #endregion

    #region Individual Subscription Plan Actions - /v1/subscription-plans/{planId}:action

    /// <summary>
    ///     Activate subscription plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:activate")]
    [EndpointSummary("Activate subscription plan")]
    [EndpointDescription("Activates a subscription plan by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateSubscriptionPlan(Guid planId, CancellationToken ct)
    {
        await sender.Send(new ActivateSubscriptionPlanCommand(planId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Deactivate subscription plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:deactivate")]
    [EndpointSummary("Deactivate subscription plan")]
    [EndpointDescription("Deactivates a subscription plan by ID.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeactivateSubscriptionPlan(Guid planId, CancellationToken ct)
    {
        await sender.Send(new DeactivateSubscriptionPlanCommand(planId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Archive subscription plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:archive")]
    [EndpointSummary("Archive subscription plan")]
    [EndpointDescription("Archives a subscription plan, making it unavailable for new subscriptions while preserving existing subscriptions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ArchiveSubscriptionPlan(Guid planId, CancellationToken ct)
    {
        await sender.Send(new ArchiveSubscriptionPlanCommand(planId), ct);
        return NoContent();
    }

    /// <summary>
    ///     Clone subscription plan
    /// </summary>
    /// <param name="planId">Plan ID to clone</param>
    /// <param name="body">Clone request with new name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created plan ID</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:clone")]
    [EndpointSummary("Clone subscription plan")]
    [EndpointDescription("Creates a copy of an existing subscription plan with a new name and slug.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloneSubscriptionPlan(Guid planId, [FromBody] CloneSubscriptionPlanRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var newPlanId = await sender.Send(new CloneSubscriptionPlanCommand(planId, body.NewName, body.NewSlug), ct);
        return CreatedAtAction(nameof(GetSubscriptionPlanById), new { planId = newPlanId }, new { id = newPlanId });
    }

    /// <summary>
    ///     Set subscription plan featured status
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">Featured status configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:featured")]
    [EndpointSummary("Set subscription plan featured status")]
    [EndpointDescription("Sets whether a subscription plan is featured or not.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSubscriptionPlanFeatured(Guid planId, [FromBody] SetFeaturedRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new SetSubscriptionPlanFeaturedCommand(planId, body.Featured), ct);
        return NoContent();
    }

    /// <summary>
    ///     Set subscription plan external ID
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="body">External ID configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("v{version:apiVersion}/subscription-plans/{planId:guid}:external-id")]
    [EndpointSummary("Set subscription plan external ID")]
    [EndpointDescription("Sets the external system ID for subscription plan integration.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSubscriptionPlanExternalId(Guid planId, [FromBody] SetExternalIdRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        await sender.Send(new SetSubscriptionPlanExternalIdCommand(planId, body.ExternalId), ct);
        return NoContent();
    }

    #endregion

    // POST /subscription-plans
    public record CreatePlanRequest(string Name, string Slug, long MonthlyPriceInCents, string Currency = "USD", string? Description = null);

    // POST /subscription-plans:compare
    public record ComparePlansRequest(Guid BasePlanId, List<Guid> ComparePlanIds);

    // POST /subscription-plans/{planId}:validate-limits
    public record ValidateLimitsRequest(int Users, long StorageMb, long ApiCalls);

    // PATCH style updates separated by concern
    public record UpdateDetailsRequest(Guid PlanId, string Name, string? Description, int? SortOrder);

    public record UpdatePricingRequest(long MonthlyPriceInCents, long? AnnualPriceInCents);

    public record UpdateLimitsRequest(int? MaxUsers, long? MaxStorageMb, long? MaxApiCallsPerMonth);

    public record UpdateFeaturesRequest(bool? HasPrioritySupport, bool? HasAdvancedAnalytics, bool? HasCustomBranding, string? Features);

    public record SetFeaturedRequest(bool Featured = true);

    public record SetExternalIdRequest(string ExternalId);

    // PUT /subscription-plans/{planId}
    public record PutSubscriptionPlanRequest(
        string Name,
        string Slug,
        string? Description,
        long MonthlyPriceInCents,
        long? AnnualPriceInCents,
        int? MaxUsers,
        long? MaxStorageMb,
        long? MaxApiCallsPerMonth,
        bool? HasPrioritySupport,
        bool? HasAdvancedAnalytics,
        bool? HasCustomBranding,
        string? Features,
        int? SortOrder);

    // POST /subscription-plans/{planId}:clone
    public record CloneSubscriptionPlanRequest(string NewName, string NewSlug);
}
