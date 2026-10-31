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
///     Fraud-program statistics (issue #409 reporting): assessed/flagged/blocked counts and
///     rates, the chargeback rate, the false-positive proxy, the manual-review backlog with
///     average time-to-review, and the top fraud signals by frequency. Authorization is
///     enforced per request through the <c>payments:admin</c> permission
///     (<see cref="GetFraudStatisticsQuery"/>) in addition to the controller-level
///     authentication requirement.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fraud/statistics")]
[Microsoft.AspNetCore.Http.Tags("fraud/statistics")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
public sealed class FraudStatisticsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    /// <summary>
    ///     Get the fraud statistics report for a date window (defaults to the last 30 days).
    /// </summary>
    /// <param name="tenantId">Optional tenant filter (system administrators only; other callers are scoped to their own tenant).</param>
    /// <param name="fromUtc">Optional inclusive window start (ISO-8601); defaults to 30 days ago.</param>
    /// <param name="toUtc">Optional inclusive window end (ISO-8601); defaults to now.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [EndpointSummary("Get fraud statistics")]
    [EndpointDescription("Reports fraud-program statistics over a date window: assessed, flagged and blocked payment counts with rates, the chargeback rate among succeeded payments, the false-positive proxy (held reviews later approved), the manual-review backlog with average time-to-review, and the top fraud signals by frequency. Requires the payments:admin permission.")]
    [ProducesResponseType<FraudStatisticsReport>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var actorContext = actorContextAccessor.ActorContext;
        if (actorContext.IsAuthenticated && !actorContext.IsSystemAdmin)
        {
            tenantId = actorContext.TenantId;
        }

        return Ok(await sender.Send(
            new GetFraudStatisticsQuery(tenantId, fromUtc, toUtc),
            ct).ConfigureAwait(false));
    }

    /// <summary>
    ///     Export the fraud statistics report for a date window as CSV (issue #409: export
    ///     fraud reports). Same authorization and window semantics as the JSON report.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter (system administrators only; other callers are scoped to their own tenant).</param>
    /// <param name="fromUtc">Optional inclusive window start (ISO-8601); defaults to 30 days ago.</param>
    /// <param name="toUtc">Optional inclusive window end (ISO-8601); defaults to now.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("export")]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [EndpointSummary("Export fraud statistics as CSV")]
    [EndpointDescription("Exports the fraud statistics report over a date window as CSV: one summary block plus per-country, per-provider and daily-trend sections. Requires the payments:admin permission.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportStatistics(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var actorContext = actorContextAccessor.ActorContext;
        if (actorContext.IsAuthenticated && !actorContext.IsSystemAdmin)
        {
            tenantId = actorContext.TenantId;
        }

        var report = await sender.Send(
            new GetFraudStatisticsQuery(tenantId, fromUtc, toUtc),
            ct).ConfigureAwait(false);

        return File(
            FraudStatisticsCsvComposer.Compose(report),
            "text/csv; charset=utf-8",
            $"fraud-statistics-{report.FromUtc:yyyyMMdd}-{report.ToUtc:yyyyMMdd}.csv");
    }
}

/// <summary>
///     Serializes a <see cref="FraudStatisticsReport"/> to CSV (issue #409: export fraud
///     reports). Sections: summary, per-country, per-provider, daily trend.
/// </summary>
public static class FraudStatisticsCsvComposer
{
    /// <summary>Composes the CSV bytes of the report (UTF-8, RFC 4180 quoting).</summary>
    public static byte[] Compose(FraudStatisticsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new System.Text.StringBuilder();

        builder.AppendLine("section,key,created,review_held,blocked");
        builder.AppendLine($"summary,totals,{report.Totals.PaymentsCreated},{report.Totals.ReviewHeld},{report.Totals.Blocked}");
        builder.AppendLine($"summary,assessed,{report.Totals.Assessed},,");
        builder.AppendLine($"summary,succeeded,{report.Totals.Succeeded},,");
        builder.AppendLine($"summary,disputed,{report.Totals.Disputed},,");
        builder.AppendLine(NullableRate("summary,flagged_percent", report.Rates.FlaggedPercent));
        builder.AppendLine(NullableRate("summary,blocked_percent", report.Rates.BlockedPercent));
        builder.AppendLine(NullableRate("summary,chargeback_percent", report.Rates.ChargebackPercent));
        builder.AppendLine(NullableRate("summary,false_positive_percent", report.Rates.FalsePositivePercent));

        foreach (var country in report.ByCountry)
        {
            builder.AppendLine($"by_country,{Field(country.Country)},{country.Created},{country.ReviewHeld},{country.Blocked}");
        }

        foreach (var method in report.ByPaymentMethod)
        {
            builder.AppendLine($"by_payment_method,{Field(method.Provider)},{method.Created},{method.ReviewHeld},{method.Blocked}");
        }

        foreach (var point in report.DailyTrend)
        {
            builder.AppendLine($"daily_trend,{point.Day:yyyy-MM-dd},{point.Created},{point.ReviewHeld},{point.Blocked}");
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string NullableRate(string prefix, double? rate) =>
        rate.HasValue ? $"{prefix},{rate.Value:0.00},," : $"{prefix},,,";

    private static string Field(string value) =>
        value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
