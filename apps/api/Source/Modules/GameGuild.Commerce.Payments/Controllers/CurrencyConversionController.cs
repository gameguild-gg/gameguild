using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Currency conversion API (issue #412 spec D): converts one or many amounts into a
///     tenant-enabled target currency using current (override-aware, TTL-cached) quotations,
///     or persisted historical snapshots when an as-of date is supplied.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/currency")]
[Microsoft.AspNetCore.Http.Tags("currency")]
[Authorize]
public sealed class CurrencyConversionController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Converts a batch of amounts (1..50 entries) into the target currency. Entries keep
    ///     their own source currency; same-currency entries are identity conversions. Per-item
    ///     failures (unknown code, disabled currency, missing quotation) are reported on the
    ///     item, never aborting the batch.
    /// </summary>
    /// <param name="request">Conversion payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("convert")]
    [EndpointSummary("Convert amounts into a currency")]
    [EndpointDescription(
        "Converts one or many amounts into the target currency through the platform FX seam (operator overrides and synced market grids, TTL-cached). Supply asOfUtc to convert with the persisted historical snapshot effective at that date. Both currencies must be enabled for the tenant.")]
    [ProducesResponseType(typeof(CurrencyConversionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Convert([FromBody] ConvertCurrencyRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.Send(
            new ConvertCurrencyCommand(
                request.TargetCurrency,
                request.Items?
                    .Select(item => new CurrencyConversionItemInput(item.Amount, item.Currency))
                    .ToList() ?? [],
                request.AsOfUtc?.UtcDateTime),
            ct).ConfigureAwait(false);

        return Ok(result);
    }
}

/// <summary>Request payload for the currency conversion endpoint.</summary>
/// <param name="TargetCurrency">ISO 4217 code every amount is converted into.</param>
/// <param name="Items">Entries to convert (1..50), each with its own source currency.</param>
/// <param name="AsOfUtc">Optional historical instant (UTC); omit for current quotations.</param>
public sealed record ConvertCurrencyRequest(
    string TargetCurrency,
    IReadOnlyList<ConvertCurrencyItemRequest>? Items,
    DateTimeOffset? AsOfUtc = null);

/// <summary>One entry of a conversion request.</summary>
/// <param name="Amount">Amount to convert (≥ 0).</param>
/// <param name="Currency">ISO 4217 code the amount is denominated in.</param>
public sealed record ConvertCurrencyItemRequest(decimal Amount, string Currency);
