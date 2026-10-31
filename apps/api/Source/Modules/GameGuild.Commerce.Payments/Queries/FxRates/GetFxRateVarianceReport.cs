using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>One row of the FX rate variance report (issue #412 spec G).</summary>
/// <param name="BaseCurrency">Override base code.</param>
/// <param name="QuoteCurrency">Override quote code.</param>
/// <param name="OverrideRate">Active operator override (expected rate).</param>
/// <param name="OverrideAsOfUtc">When the override was last set.</param>
/// <param name="OverrideExpiresAtUtc">Override expiry, when set.</param>
/// <param name="MarketRate">Newest synced market quotation for the same direction (actual rate).</param>
/// <param name="MarketRateEffectiveDateUtc">Effective date of the market quotation.</param>
/// <param name="MarketRateProvider">Provider of the market quotation.</param>
/// <param name="VariancePercent">|override − market| / market × 100, when both exist.</param>
/// <param name="ExceedsThreshold">Whether the variance exceeds the configured anomaly threshold.</param>
public sealed record FxRateVarianceEntry(
    string BaseCurrency,
    string QuoteCurrency,
    decimal OverrideRate,
    DateTime OverrideAsOfUtc,
    DateTime? OverrideExpiresAtUtc,
    decimal? MarketRate,
    DateTime? MarketRateEffectiveDateUtc,
    string? MarketRateProvider,
    decimal? VariancePercent,
    bool ExceedsThreshold);

/// <summary>FX rate variance report: actual (market) vs expected (operator override) rates.</summary>
/// <param name="GeneratedAtUtc">Report generation instant.</param>
/// <param name="ThresholdPercent">Configured variance threshold (%).</param>
/// <param name="Entries">One row per active override.</param>
public sealed record FxRateVarianceReport(
    DateTime GeneratedAtUtc,
    decimal ThresholdPercent,
    IReadOnlyList<FxRateVarianceEntry> Entries);

/// <summary>
///     Query (issue #412 spec G): exchange rate variance report — every active operator rate
///     override compared against the newest synced market quotation of the same pair
///     (actual vs expected). Variances beyond the configured threshold are flagged so
///     operators can review stale or mistyped pins.
/// </summary>
[AuthorizeRequest("", RequireSystemAdmin = true)]
public sealed record GetFxRateVarianceReportQuery : IQuery<FxRateVarianceReport>;

/// <summary>Handler for <see cref="GetFxRateVarianceReportQuery"/>.</summary>
public sealed class GetFxRateVarianceReportQueryHandler(
    IApplicationDbContext context,
    Microsoft.Extensions.Options.IOptions<FxSyncOptions> options,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetFxRateVarianceReportQuery, FxRateVarianceReport>
{
    /// <inheritdoc />
    public async Task<FxRateVarianceReport> Handle(GetFxRateVarianceReportQuery request, CancellationToken cancellationToken)
    {
        FxAdministrationActor.RequireSystemAdmin(actorContextAccessor);
        var now = SystemClock.UtcNow;
        var threshold = options.Value.MaxRateChangePercentPerDay;

        var overrides = await context.Set<FxRateSnapshot>()
            .AsNoTracking()
            .Where(snapshot => snapshot.IsOverride
                && (snapshot.ExpiresAtUtc == null || snapshot.ExpiresAtUtc > now))
            .OrderByDescending(snapshot => snapshot.AsOfUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Latest override per exact direction wins.
        var latestOverrides = new Dictionary<(string Base, string Quote), FxRateSnapshot>();
        foreach (var row in overrides)
        {
            latestOverrides.TryAdd((row.BaseCurrency, row.QuoteCurrency), row);
        }

        var entries = new List<FxRateVarianceEntry>();
        foreach (var ((baseCurrency, quoteCurrency), @override) in latestOverrides.OrderBy(pair => pair.Key.Base, StringComparer.Ordinal).ThenBy(pair => pair.Key.Quote, StringComparer.Ordinal))
        {
            var market = await context.Set<FxRateSnapshot>()
                .AsNoTracking()
                .Where(snapshot => !snapshot.IsOverride
                    && snapshot.BaseCurrency == baseCurrency
                    && snapshot.QuoteCurrency == quoteCurrency)
                .OrderByDescending(snapshot => snapshot.EffectiveDateUtc)
                .ThenByDescending(snapshot => snapshot.AsOfUtc)
                .Select(snapshot => new { snapshot.Rate, snapshot.EffectiveDateUtc, snapshot.ProviderName })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            decimal? variance = market is { Rate: > 0m }
                ? Math.Abs(@override.Rate - market.Rate) / market.Rate * 100m
                : null;

            entries.Add(new FxRateVarianceEntry(
                baseCurrency,
                quoteCurrency,
                @override.Rate,
                @override.AsOfUtc,
                @override.ExpiresAtUtc,
                market?.Rate,
                market?.EffectiveDateUtc,
                market?.ProviderName,
                variance is null ? null : decimal.Round(variance.Value, 4),
                variance is not null && threshold > 0m && variance.Value > threshold));
        }

        return new FxRateVarianceReport(now, threshold, entries);
    }
}
