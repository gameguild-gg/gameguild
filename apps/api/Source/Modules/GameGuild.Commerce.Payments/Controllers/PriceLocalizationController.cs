using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Price localization controller (issue #412 spec C): per-market plan prices with manual
///     overrides (psychological pricing) versus FX-auto-calculated prices, effective/expiry
///     dating, bulk recalculation with progress tracking, preview before applying, and
///     rollback of a finished recalculation run.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/price-localization")]
[Microsoft.AspNetCore.Http.Tags("price-localization")]
[Authorize]
public sealed class PriceLocalizationController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Effective localized price per (plan, enabled currency), annotated with the live
    ///     FX-calculated reference price so operators see override vs auto side by side.
    /// </summary>
    /// <param name="planId">Optional plan filter; null = every localized plan.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("List localized plan prices")]
    [EndpointDescription("Effective localization per plan and enabled currency with the current FX-calculated reference price (override vs auto-calculated view).")]
    [ProducesResponseType(typeof(IReadOnlyList<PlanLocalizedPriceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocalizedPrices([FromQuery] Guid? planId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPlanLocalizedPricesQuery(planId), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Previews a recalculation pass: proposed auto prices against currently effective
    ///     values, with the quotation behind every proposal. Read-only.
    /// </summary>
    /// <param name="planId">Optional plan scope; null = every active plan.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("recalculate/preview")]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("Preview a localized-price recalculation")]
    [EndpointDescription("Computes what a recalculation pass would change (proposed auto prices, rates, deltas) without writing anything.")]
    [ProducesResponseType(typeof(IReadOnlyList<LocalizedPricePreviewItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewRecalculation([FromQuery] Guid? planId, CancellationToken ct)
    {
        var result = await sender.Send(new PreviewLocalizedPricesRecalculationQuery(planId), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Runs a bulk recalculation of auto-calculated prices from the current FX quotations
    ///     (manual overrides are preserved). Creates a progress-tracked, rollback-capable run.
    /// </summary>
    /// <param name="body">Optional scope (plan id) and audit reason.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("recalculate")]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("Recalculate localized prices")]
    [EndpointDescription("Bulk-recalculates auto-calculated localized prices from current FX rates; preserves manual overrides; tracks progress on a run row that can be rolled back.")]
    [ProducesResponseType(typeof(PriceRecalculationOutcome), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Recalculate([FromBody] RecalculateLocalizedPricesRequest? body, CancellationToken ct)
    {
        var result = await sender.Send(
            new RecalculateLocalizedPricesCommand(body?.PlanId, body?.Reason ?? "manual"),
            ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Lists recalculation runs with their progress counters (newest first).
    /// </summary>
    /// <param name="limit">Maximum runs (1..100, default 20).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("recalculations")]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("List recalculation runs")]
    [EndpointDescription("Recalculation runs with progress counters (total/completed items, updated prices, preserved overrides) and rollback state.")]
    [ProducesResponseType(typeof(IReadOnlyList<PriceRecalculationRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuns([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPriceRecalculationRunsQuery(limit), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    ///     Rolls back a finished recalculation run, restoring every auto row it rewrote to its
    ///     snapshotted before-value.
    /// </summary>
    /// <param name="runId">Run to roll back.</param>
    /// <param name="body">Audit reason.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("recalculations/{runId:guid}/rollback")]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("Roll back a recalculation run")]
    [EndpointDescription("Restores the before-values snapshotted by the run; manual overrides were never touched and stay unchanged.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rollback(Guid runId, [FromBody] RollbackRecalculationRequest? body, CancellationToken ct)
    {
        var restored = await sender.Send(
            new RollbackPriceRecalculationCommand(runId, body?.Reason ?? "rollback"),
            ct).ConfigureAwait(false);
        return Ok(new { runId, restored });
    }

    /// <summary>
    ///     Sets a manual localized price (psychological pricing) for a plan in an enabled
    ///     currency. Optional effective date schedules a future change; optional expiry ends a
    ///     temporary override. Audit-logged.
    /// </summary>
    /// <param name="planId">Plan identifier.</param>
    /// <param name="currencyCode">ISO 4217 code.</param>
    /// <param name="body">Price payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("plans/{planId:guid}/currencies/{currencyCode}")]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("Set a manual localized price")]
    [EndpointDescription("Persists an audited manual price override for the plan/currency pair; recalculation runs never touch manual rows.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetPrice(Guid planId, string currencyCode, [FromBody] SetLocalizedPriceRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await sender.Send(
            new SetPlanLocalizedPriceCommand(
                planId,
                currencyCode,
                body.LocalizedPrice,
                body.Reason,
                body.EffectiveDateUtc,
                body.ExpiryDateUtc),
            ct).ConfigureAwait(false);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>
    ///     Clears the manual overrides of a (plan, currency) pair so pricing falls back to the
    ///     auto-calculated/FX-converted price. Audit-logged.
    /// </summary>
    /// <param name="planId">Plan identifier.</param>
    /// <param name="currencyCode">ISO 4217 code.</param>
    /// <param name="body">Audit reason.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("plans/{planId:guid}/currencies/{currencyCode}")]
    [Authorize(Roles = "SystemAdmin,Owner,TenantAdmin,Admin")]
    [EndpointSummary("Clear manual price overrides")]
    [EndpointDescription("Removes the manual overrides of the plan/currency pair (including future-dated rows); auto-calculated rows are untouched.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearPrice(Guid planId, string currencyCode, [FromBody] ClearLocalizedPriceRequest? body, CancellationToken ct)
    {
        var removed = await sender.Send(
            new ClearPlanLocalizedPriceCommand(planId, currencyCode, body?.Reason ?? "clear-override"),
            ct).ConfigureAwait(false);
        return Ok(new { planId, currencyCode, removed });
    }
}

/// <summary>Request payload for the manual localized-price command.</summary>
public sealed record SetLocalizedPriceRequest(
    decimal LocalizedPrice,
    string Reason,
    DateTime? EffectiveDateUtc = null,
    DateTime? ExpiryDateUtc = null);

/// <summary>Request payload for clearing manual overrides.</summary>
public sealed record ClearLocalizedPriceRequest(string Reason);

/// <summary>Request payload for the bulk recalculation command.</summary>
public sealed record RecalculateLocalizedPricesRequest(Guid? PlanId = null, string Reason = "manual");

/// <summary>Request payload for the rollback command.</summary>
public sealed record RollbackRecalculationRequest(string Reason);
