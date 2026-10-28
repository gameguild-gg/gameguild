using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Fraud blacklist administration (issue #409): lists, checks, adds and removes the
///     tenant's blacklist entries and manual whitelist overrides. The tenant scope always
///     comes from the actor context, never from the route or body. Authorization is
///     additionally enforced per request through the <c>payments:admin</c> permission on
///     the underlying queries and commands.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fraud/blacklist")]
[Microsoft.AspNetCore.Http.Tags("fraud/blacklist")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class FraudBlacklistController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    /// <summary>
    ///     List the tenant's fraud blacklist entries, newest first.
    /// </summary>
    /// <param name="entryType">Optional entry-type filter (IpAddress, EmailDomain, Email, CardBin, DeviceFingerprint, PaymentMethodToken, CardFingerprint, ExternalCustomerId).</param>
    /// <param name="includeExpired">When true, expired entries are included.</param>
    /// <param name="page">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("List fraud blacklist entries")]
    [EndpointDescription("Lists the caller's tenant fraud blacklist and whitelist-override entries, newest first, with optional entry-type and expiry filters. Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistPage>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] FraudBlacklistEntryType? entryType = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        return Ok(await sender.Send(
            new ListFraudBlacklistQuery(tenantId, entryType, includeExpired, page, pageSize),
            ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Check whether a concrete value matches the tenant blacklist (same evaluation the
    ///     payment fraud gate performs).
    /// </summary>
    /// <param name="entryType">Entry kind to evaluate.</param>
    /// <param name="value">Concrete value to check (IP address, email domain, BIN, fingerprint, token...).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("check")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Check a value against the fraud blacklist")]
    [EndpointDescription("Evaluates a concrete value (IP address, email domain, card BIN, device fingerprint, payment-method token, card fingerprint or provider customer id) against the caller's tenant blacklist, honoring wildcard patterns, expiry dates and manual whitelist overrides. Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistCheckResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Check(
        [FromQuery] FraudBlacklistEntryType entryType,
        [FromQuery] string value,
        CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();

        return Ok(await sender.Send(
            new CheckFraudBlacklistQuery(tenantId, entryType, value),
            ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Add a blacklist entry or a manual whitelist override for the caller's tenant.
    /// </summary>
    /// <param name="body">Entry to add.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [EndpointSummary("Add a fraud blacklist entry")]
    [EndpointDescription("Adds a fraud blacklist entry (or a manual whitelist override) for the caller's tenant. Values support * wildcard patterns (e.g. *.malicious.com, 192.168.*) and optional expiry dates. Idempotent per (type, value, kind). Requires the payments:admin permission.")]
    [ProducesResponseType<FraudBlacklistEntryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Add([FromBody] AddFraudBlacklistEntryRequest body, CancellationToken ct)
    {
        if (!Enum.TryParse<FraudBlacklistEntryType>(body.EntryType, ignoreCase: true, out var entryType))
        {
            return BadRequest(new { error = $"Unknown entryType '{body.EntryType}'." });
        }

        var result = await sender.Send(
            new AddFraudBlacklistEntryCommand(entryType, body.Value, body.Reason, body.IsWhitelist, body.ExpiresAt),
            ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Remove a blacklist entry owned by the caller's tenant.
    /// </summary>
    /// <param name="entryId">Entry to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{entryId:guid}")]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [EndpointSummary("Remove a fraud blacklist entry")]
    [EndpointDescription("Removes (soft-deletes) a fraud blacklist entry owned by the caller's tenant. Requires the payments:admin permission.")]
    [ProducesResponseType<RemoveFraudBlacklistEntryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid entryId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveFraudBlacklistEntryCommand(entryId), ct).ConfigureAwait(false);

        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Request body for adding a blacklist entry.</summary>
    /// <param name="EntryType">Entry kind (IpAddress, EmailDomain, Email, CardBin, DeviceFingerprint, PaymentMethodToken, CardFingerprint, ExternalCustomerId).</param>
    /// <param name="Value">Pattern to store; * wildcards supported.</param>
    /// <param name="Reason">Optional bounded reason.</param>
    /// <param name="IsWhitelist">True for a manual whitelist override.</param>
    /// <param name="ExpiresAt">Optional expiry instant (ISO-8601).</param>
    public sealed record AddFraudBlacklistEntryRequest(
        string EntryType,
        string Value,
        string? Reason = null,
        bool IsWhitelist = false,
        DateTime? ExpiresAt = null);

    private Guid ResolveTenantId()
        => actorContextAccessor.ActorContext.TenantId
           ?? throw new UnauthorizedAccessException("A tenant context is required to manage the fraud blacklist.");
}
