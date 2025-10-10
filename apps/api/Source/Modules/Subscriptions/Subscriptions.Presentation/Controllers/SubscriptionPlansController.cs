using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Features.CreateSubscription;
using GameGuild.Modules.Subscriptions.Features.GetSubscription;
using GameGuild.Modules.Subscriptions.Features.ManageSubscription;
using GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class SubscriptionPlansController(ISender sender) : ControllerBase
{
    // GET /subscription-plans (all) or /subscription-plans/active
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        object result = activeOnly
            ? await sender.Send(new GetActiveSubscriptionPlansQuery(), ct)
            : await sender.Send(new GetAllSubscriptionPlansQuery(), ct);

        return Ok(result);
    }

    // GET /subscription-plans/featured
    [HttpGet("featured")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatured(CancellationToken ct) { return Ok(await sender.Send(new GetFeaturedSubscriptionPlansQuery(), ct)); }

    // GET /subscription-plans/search?term=
    [HttpGet("search")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string term, CancellationToken ct) { return Ok(await sender.Send(new SearchSubscriptionPlansQuery(term), ct)); }

    // GET /subscription-plans/price-range
    [HttpGet("price-range")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ByPriceRange([FromQuery] long min, [FromQuery] long max, CancellationToken ct) { return Ok(await sender.Send(new GetSubscriptionPlansByPriceRangeQuery(min, max), ct)); }

    // GET /subscription-plans/paged?page=&pageSize=&isActive=&isFeatured=&searchTerm=
    [HttpGet("paged")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null, [FromQuery] bool? isFeatured = null, [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPagedSubscriptionPlansQuery(page, pageSize, isActive, isFeatured, searchTerm), ct);

        return Ok(result);
    }

    // GET /subscription-plans/{id}
    [HttpGet("{id:guid}", Name = "GetSubscriptionPlanById")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var plan = await sender.Send(new GetSubscriptionPlanByIdQuery(id), ct);

        return plan is null ? NotFound() : Ok(plan);
    }

    // GET /subscription-plans/slug/{slug}
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var plan = await sender.Send(new GetSubscriptionPlanBySlugQuery(slug), ct);

        return plan is null ? NotFound() : Ok(plan);
    }

    // GET /subscription-plans/{id}/usage
    [HttpGet("{id:guid}/usage")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Usage(Guid id, CancellationToken ct) { return Ok(await sender.Send(new GetSubscriptionPlanUsageStatisticsQuery(id), ct)); }

    // GET /subscription-plans/{id}/suggest-upgrades?users=&storageMb=&apiCalls=
    [HttpGet("{id:guid}/suggest-upgrades")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestUpgrades(Guid id, [FromQuery] int users, [FromQuery] long storageMb, [FromQuery] long apiCalls, CancellationToken ct)
    {
        return Ok(await sender.Send(new SuggestSubscriptionPlanUpgradesQuery(id, users, storageMb, apiCalls), ct));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest body, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSubscriptionPlanCommand(body.Name, body.Slug, body.MonthlyPriceInCents, body.Currency, body.Description), ct);

        return CreatedAtRoute(
            "GetSubscriptionPlanById",
            new
            {
                id
            },
            new
            {
                id, body.Name, body.Slug
            }
        );
    }

    [HttpPatch("{id:guid}/details")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDetails(Guid id, [FromBody] UpdateDetailsRequest body, CancellationToken ct)
    {
        await sender.Send(new UpdateSubscriptionPlanCommand(id, body.Name, body.Description, body.SortOrder), ct);

        return NoContent();
    }

    [HttpPatch("{id:guid}/pricing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePricing(Guid id, [FromBody] UpdatePricingRequest body, CancellationToken ct)
    {
        await sender.Send(new UpdateSubscriptionPlanPricingCommand(id, body.MonthlyPriceInCents, body.AnnualPriceInCents), ct);

        return NoContent();
    }

    [HttpPatch("{id:guid}/limits")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateLimits(Guid id, [FromBody] UpdateLimitsRequest body, CancellationToken ct)
    {
        await sender.Send(new UpdateSubscriptionPlanLimitsCommand(id, body.MaxUsers, body.MaxStorageMb, body.MaxApiCallsPerMonth), ct);

        return NoContent();
    }

    [HttpPatch("{id:guid}/features")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateFeatures(Guid id, [FromBody] UpdateFeaturesRequest body, CancellationToken ct)
    {
        await sender.Send(new UpdateSubscriptionPlanFeaturesCommand(id, body.HasPrioritySupport, body.HasAdvancedAnalytics, body.HasCustomBranding, body.Features), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await sender.Send(new ActivateSubscriptionPlanCommand(id), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeactivateSubscriptionPlanCommand(id), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/featured")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetFeatured(Guid id, [FromBody] SetFeaturedRequest body, CancellationToken ct)
    {
        await sender.Send(new SetSubscriptionPlanFeaturedCommand(id, body.Featured), ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/external-id")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetExternalId(Guid id, [FromBody] SetExternalIdRequest body, CancellationToken ct)
    {
        await sender.Send(new SetSubscriptionPlanExternalIdCommand(id, body.ExternalId), ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSubscriptionPlanCommand(id), ct);

        return NoContent();
    }

    // GET /subscription-plans/compare?basePlanId=&comparePlanIds=guid1&comparePlanIds=guid2
    [HttpGet("compare")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compare([FromQuery] Guid basePlanId, [FromQuery] List<Guid> comparePlanIds, CancellationToken ct)
    {
        var result = await sender.Send(new CompareSubscriptionPlansQuery(basePlanId, comparePlanIds), ct);

        return Ok(result);
    }

    // GET /subscription-plans/{id}/validate-limits?users=&storageMb=&apiCalls=
    [HttpGet("{id:guid}/validate-limits")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateLimits(Guid id, [FromQuery] int users, [FromQuery] long storageMb, [FromQuery] long apiCalls, CancellationToken ct)
    {
        var result = await sender.Send(new ValidateSubscriptionPlanLimitsQuery(id, users, storageMb, apiCalls), ct);

        return Ok(result);
    }

    // POST /subscription-plans
    public record CreatePlanRequest(string Name, string Slug, long MonthlyPriceInCents, string Currency = "USD", string? Description = null);

    // PATCH style updates separated by concern
    public record UpdateDetailsRequest(Guid PlanId, string Name, string? Description, int? SortOrder);

    public record UpdatePricingRequest(long MonthlyPriceInCents, long? AnnualPriceInCents);

    public record UpdateLimitsRequest(int? MaxUsers, long? MaxStorageMb, long? MaxApiCallsPerMonth);

    public record UpdateFeaturesRequest(bool? HasPrioritySupport, bool? HasAdvancedAnalytics, bool? HasCustomBranding, string? Features);

    public record SetFeaturedRequest(bool Featured = true);

    public record SetExternalIdRequest(string ExternalId);
}

