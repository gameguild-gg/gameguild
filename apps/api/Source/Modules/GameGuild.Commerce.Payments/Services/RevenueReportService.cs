using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Default <see cref="IRevenueReportService" /> (issue #404): builds compliance-grade
///     summaries, historical daily trends, and RFC 4180 CSV / JSON exports for external
///     accounting and ERP consumption. The #412 spec F/G additions convert per-currency
///     totals into the tenant base currency at the period-end rate and quantify the
///     conversion cost of operator rate overrides.
/// </summary>
public sealed partial class RevenueReportService(
    IRevenueEventRepository revenueEventRepository,
    IRevenueReconciliationRepository reconciliationRepository,
    IApplicationDbContext context) : IRevenueReportService
{
    private static readonly JsonSerializerOptions ExportSerializerOptions = new(JsonSerializerDefaults.Web);

    [GeneratedRegex("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled)]
    private static partial Regex SafeFormatRegex();

    /// <inheritdoc />
    public async Task<RevenueComplianceReport> GetComplianceReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var totalsByEventType = await revenueEventRepository
            .GetGroupedTotalsAsync(fromUtc, toUtc, tenantId, RevenueEventTotalGrouping.EventType, cancellationToken)
            .ConfigureAwait(false);
        var totalsBySource = await revenueEventRepository
            .GetGroupedTotalsAsync(fromUtc, toUtc, tenantId, RevenueEventTotalGrouping.Source, cancellationToken)
            .ConfigureAwait(false);
        var totalsByStatus = await revenueEventRepository
            .GetGroupedTotalsAsync(fromUtc, toUtc, tenantId, RevenueEventTotalGrouping.Status, cancellationToken)
            .ConfigureAwait(false);

        var runs = await reconciliationRepository
            .GetRunsOverlappingPeriodAsync(fromUtc, toUtc, tenantId, cancellationToken)
            .ConfigureAwait(false);

        var coverage = new RevenueReconciliationCoverage(
            ReconciliationRuns: runs.Count,
            MatchedLines: runs.Sum(run => run.MatchedCount),
            Discrepancies: runs.Sum(run => run.DiscrepancyCount),
            LastRunCompletedAtUtc: runs
                .Select(run => run.CompletedAtUtc)
                .Where(completedAt => completedAt is not null)
                .DefaultIfEmpty()
                .Max());

        var uncountedEventCount = totalsByStatus
            .Where(total => total.Key == nameof(RevenueEventStatus.Pending) || total.Key == nameof(RevenueEventStatus.Failed))
            .Sum(total => total.Count);

        var recognizedByCurrency = totalsByEventType
            .GroupBy(total => total.Currency, StringComparer.Ordinal)
            .Select(group => (Currency: group.Key, Total: group.Sum(total => total.Total)))
            .OrderBy(entry => entry.Currency, StringComparer.Ordinal)
            .ToList();
        var grossBreakdown = string.Join(
            ", ",
            recognizedByCurrency.Select(entry => $"{entry.Total.ToString("0.00", CultureInfo.InvariantCulture)} {entry.Currency}"));
        grossBreakdown = grossBreakdown.Length > 0 ? grossBreakdown : "0.00 (no revenue events)";
        var attestation =
            $"Revenue audit summary for {fromUtc:yyyy-MM-ddTHH:mm:ssZ} to {toUtc:yyyy-MM-ddTHH:mm:ssZ}: " +
            $"{totalsByStatus.Sum(total => total.Count)} revenue events recorded, {grossBreakdown} gross across event types, " +
            $"{uncountedEventCount} events not yet processed, {coverage.ReconciliationRuns} reconciliation run(s) with {coverage.Discrepancies} discrepancy(ies). " +
            "Totals are computed from immutable revenue event records as of the report timestamp, reported per currency without conversion or cross-currency consolidation.";

        return new RevenueComplianceReport(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            GeneratedAtUtc: SystemClock.UtcNow,
            TotalsByEventType: totalsByEventType,
            TotalsBySource: totalsBySource,
            TotalsByStatus: totalsByStatus,
            UncountedEventCount: uncountedEventCount,
            Reconciliation: coverage,
            Attestation: attestation);
    }

    /// <inheritdoc />
    public async Task<RevenueTrendReport> GetTrendReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var dailyTotals = await revenueEventRepository
            .GetDailyTotalsAsync(fromUtc, toUtc, tenantId, cancellationToken)
            .ConfigureAwait(false);

        // One trend series per observed currency: zero-activity days are filled per
        // currency, and totals are never merged across currencies (100 USD + 100 EUR
        // must never surface as a unitless 200).
        var currencies = dailyTotals
            .Select(total => total.Currency)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(currency => currency, StringComparer.Ordinal)
            .ToList();

        var points = new List<RevenueTrendPoint>();
        var totalsByCurrency = new List<RevenueTrendCurrencyTotal>();
        foreach (var currency in currencies)
        {
            var totalsByDay = dailyTotals
                .Where(total => string.Equals(total.Currency, currency, StringComparison.Ordinal))
                .ToDictionary(total => total.DateUtc, total => total);

            decimal credit = 0m, debit = 0m, net = 0m;
            for (var day = fromUtc.Date; day <= toUtc.Date; day = day.AddDays(1))
            {
                if (totalsByDay.TryGetValue(day, out var total))
                {
                    points.Add(new RevenueTrendPoint(day, currency, total.CreditTotal, total.DebitTotal, total.NetTotal, total.EventCount));
                    credit += total.CreditTotal;
                    debit += total.DebitTotal;
                    net += total.NetTotal;
                }
                else
                {
                    points.Add(new RevenueTrendPoint(day, currency, 0m, 0m, 0m, 0));
                }
            }

            totalsByCurrency.Add(new RevenueTrendCurrencyTotal(currency, credit, debit, net));
        }

        points.Sort((left, right) =>
        {
            var byDate = left.DateUtc.CompareTo(right.DateUtc);
            return byDate != 0 ? byDate : string.CompareOrdinal(left.Currency, right.Currency);
        });

        return new RevenueTrendReport(
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Points: points,
            TotalsByCurrency: totalsByCurrency);
    }

    /// <inheritdoc />
    public async Task<CurrencyPopularityReport> GetPopularCurrenciesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var dailyTotals = await revenueEventRepository
            .GetDailyTotalsAsync(fromUtc, toUtc, tenantId, cancellationToken)
            .ConfigureAwait(false);

        // Rank per currency only: transaction count first, then net volume (absolute, so a
        // refund-heavy currency is not punished into invisibility), then code for determinism.
        // Totals are never merged across currencies (100 USD + 100 EUR is not a unitless 200).
        var entries = dailyTotals
            .GroupBy(total => total.Currency, StringComparer.Ordinal)
            .Select(group => new
            {
                Currency = group.Key,
                TransactionCount = group.Sum(total => total.EventCount),
                TotalCredit = group.Sum(total => total.CreditTotal),
                TotalDebit = group.Sum(total => total.DebitTotal),
                TotalNet = group.Sum(total => total.NetTotal)
            })
            .OrderByDescending(entry => entry.TransactionCount)
            .ThenByDescending(entry => Math.Abs(entry.TotalNet))
            .ThenBy(entry => entry.Currency, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 50))
            .Select((entry, index) => new CurrencyPopularityEntry(
                index + 1,
                entry.Currency,
                entry.TransactionCount,
                entry.TotalCredit,
                entry.TotalDebit,
                entry.TotalNet))
            .ToList();

        return new CurrencyPopularityReport(
            fromUtc,
            toUtc,
            SystemClock.UtcNow,
            entries);
    }

    /// <inheritdoc />
    public async Task<RevenueReportExport> ExportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        string format,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(format)
            || !SafeFormatRegex().IsMatch(format)
            || !string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
              && !string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Export format must be 'csv' or 'json'.", nameof(format));
        }

        var complianceReport = await GetComplianceReportAsync(fromUtc, toUtc, tenantId, cancellationToken).ConfigureAwait(false);
        var trendReport = await GetTrendReportAsync(fromUtc, toUtc, tenantId, cancellationToken).ConfigureAwait(false);

        var periodLabel = $"{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}";
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return new RevenueReportExport(
                $"revenue-audit-{periodLabel}.csv",
                "text/csv; charset=utf-8",
                BuildCsv(complianceReport, trendReport));
        }

        return new RevenueReportExport(
            $"revenue-audit-{periodLabel}.json",
            "application/json",
            JsonSerializer.Serialize(
                new
                {
                    complianceReport.FromUtc,
                    complianceReport.ToUtc,
                    complianceReport.GeneratedAtUtc,
                    Compliance = complianceReport,
                    Trend = trendReport
                },
                ExportSerializerOptions));
    }

    private static string BuildCsv(RevenueComplianceReport complianceReport, RevenueTrendReport trendReport)
    {
        var builder = new StringBuilder();
        builder.AppendLine("section,key,currency,count,total");

        foreach (var total in complianceReport.TotalsByEventType)
        {
            builder.AppendLine($"event_type,{Escape(total.Key)},{Escape(total.Currency)},{total.Count},{Format(total.Total)}");
        }

        foreach (var total in complianceReport.TotalsBySource)
        {
            builder.AppendLine($"source,{Escape(total.Key)},{Escape(total.Currency)},{total.Count},{Format(total.Total)}");
        }

        foreach (var total in complianceReport.TotalsByStatus)
        {
            builder.AppendLine($"status,{Escape(total.Key)},{Escape(total.Currency)},{total.Count},{Format(total.Total)}");
        }

        foreach (var point in trendReport.Points)
        {
            builder.AppendLine(
                $"daily,{point.DateUtc:yyyy-MM-dd},{Escape(point.Currency)},{point.EventCount},{Format(point.NetTotal)}");
        }

        return builder.ToString();
    }

    private static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Escape(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return field;
    }

    // ─────────────────── Base-currency report + conversion costs (issue #412 specs F/G) ───────────────────

    /// <summary>Resolves the tenant's base currency; fails closed when unscoped or unset.</summary>
    private async Task<string> RequireBaseCurrencyAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tenant || tenant == Guid.Empty)
        {
            throw new BusinessRuleViolationException(
                "BaseCurrencyReportRequiresTenantScope",
                "The base-currency revenue report requires a tenant scope; the base currency is a per-tenant setting.");
        }

        var baseCurrency = await context.Set<CurrencyConfiguration>()
            .AsNoTracking()
            .Where(configuration => configuration.TenantId == tenant && configuration.IsBaseCurrency)
            .Select(configuration => configuration.CurrencyCode)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(baseCurrency))
        {
            throw new BusinessRuleViolationException(
                "NoBaseCurrencyConfigured",
                $"Tenant {tenant} has no base currency configured; set one before requesting base-currency reports.");
        }

        return baseCurrency;
    }

    /// <summary>
    ///     Resolves the period-end quotation for <paramref name="baseCurrency"/> →
    /// <paramref name="currency"/>: newest non-override snapshot at or before
    /// <paramref name="toUtc"/> whose effective date qualifies, exact direction first then
    /// the inverse of the opposite pair. Never mixes providers or dates.
    /// </summary>
    private async Task<(decimal Rate, DateTime EffectiveDateUtc, string Provider)?> ResolvePeriodRateAsync(
        string baseCurrency,
        string currency,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var snapshots = context.Set<FxRateSnapshot>().AsNoTracking();

        var direct = await snapshots
            .Where(snapshot => !snapshot.IsOverride
                && snapshot.BaseCurrency == baseCurrency
                && snapshot.QuoteCurrency == currency
                && snapshot.EffectiveDateUtc <= toUtc.Date)
            .OrderByDescending(snapshot => snapshot.EffectiveDateUtc)
            .ThenByDescending(snapshot => snapshot.AsOfUtc)
            .Select(snapshot => new { snapshot.Rate, snapshot.EffectiveDateUtc, snapshot.ProviderName })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (direct is not null)
        {
            return (direct.Rate, direct.EffectiveDateUtc, direct.ProviderName);
        }

        var inverse = await snapshots
            .Where(snapshot => !snapshot.IsOverride
                && snapshot.BaseCurrency == currency
                && snapshot.QuoteCurrency == baseCurrency
                && snapshot.EffectiveDateUtc <= toUtc.Date)
            .OrderByDescending(snapshot => snapshot.EffectiveDateUtc)
            .ThenByDescending(snapshot => snapshot.AsOfUtc)
            .Select(snapshot => new { snapshot.Rate, snapshot.EffectiveDateUtc, snapshot.ProviderName })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (inverse is not null)
        {
            return (decimal.Round(1m / inverse.Rate, 12, MidpointRounding.AwayFromZero), inverse.EffectiveDateUtc, inverse.ProviderName);
        }

        return null;
    }

    /// <summary>Active (non-expired) operator override for base → currency, when any.</summary>
    private async Task<decimal?> ResolveActiveOverrideRateAsync(
        string baseCurrency,
        string currency,
        CancellationToken cancellationToken)
    {
        var now = SystemClock.UtcNow;
        var overrideRate = await context.Set<FxRateSnapshot>()
            .AsNoTracking()
            .Where(snapshot => snapshot.IsOverride
                && snapshot.BaseCurrency == baseCurrency
                && snapshot.QuoteCurrency == currency
                && (snapshot.ExpiresAtUtc == null || snapshot.ExpiresAtUtc > now))
            .OrderByDescending(snapshot => snapshot.AsOfUtc)
            .Select(snapshot => (decimal?)snapshot.Rate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return overrideRate;
    }

    /// <inheritdoc />
    public async Task<BaseCurrencyRevenueReport> GetBaseCurrencyRevenueReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var baseCurrency = await RequireBaseCurrencyAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var dailyTotals = await revenueEventRepository
            .GetDailyTotalsAsync(fromUtc, toUtc, tenantId, cancellationToken)
            .ConfigureAwait(false);

        var perCurrency = dailyTotals
            .GroupBy(total => total.Currency, StringComparer.Ordinal)
            .Select(group => new
            {
                Currency = group.Key,
                Credit = group.Sum(total => total.CreditTotal),
                Debit = group.Sum(total => total.DebitTotal),
                Net = group.Sum(total => total.NetTotal),
                Events = group.Sum(total => total.EventCount),
            })
            .OrderBy(entry => entry.Currency, StringComparer.Ordinal)
            .ToList();

        var entries = new List<BaseCurrencyRevenueEntry>();
        var baseCredit = 0m;
        var baseDebit = 0m;
        var baseNet = 0m;
        var unconverted = 0;

        foreach (var entry in perCurrency)
        {
            if (entry.Currency == baseCurrency)
            {
                // The base-currency row converts at identity (rate 1) by definition.
                entries.Add(new BaseCurrencyRevenueEntry(
                    entry.Currency, entry.Credit, entry.Debit, entry.Net, entry.Events,
                    Converted: true, RateUsed: 1m, RateEffectiveDateUtc: toUtc.Date, RateProvider: "identity",
                    BaseCreditTotal: entry.Credit, BaseDebitTotal: entry.Debit, BaseNetTotal: entry.Net, Note: null));
                baseCredit += entry.Credit;
                baseDebit += entry.Debit;
                baseNet += entry.Net;
                continue;
            }

            var rate = await ResolvePeriodRateAsync(baseCurrency, entry.Currency, toUtc, cancellationToken).ConfigureAwait(false);
            if (rate is null)
            {
                // Fail closed: listed but excluded from base totals — never silently merged.
                unconverted++;
                entries.Add(new BaseCurrencyRevenueEntry(
                    entry.Currency, entry.Credit, entry.Debit, entry.Net, entry.Events,
                    Converted: false, RateUsed: null, RateEffectiveDateUtc: null, RateProvider: null,
                    BaseCreditTotal: null, BaseDebitTotal: null, BaseNetTotal: null,
                    Note: "no-rate-at-period-end"));
                continue;
            }

            var credit = decimal.Round(entry.Credit / rate.Value.Rate, 2, MidpointRounding.AwayFromZero);
            var debit = decimal.Round(entry.Debit / rate.Value.Rate, 2, MidpointRounding.AwayFromZero);
            var net = decimal.Round(entry.Net / rate.Value.Rate, 2, MidpointRounding.AwayFromZero);
            baseCredit += credit;
            baseDebit += debit;
            baseNet += net;
            entries.Add(new BaseCurrencyRevenueEntry(
                entry.Currency, entry.Credit, entry.Debit, entry.Net, entry.Events,
                Converted: true, RateUsed: rate.Value.Rate, RateEffectiveDateUtc: rate.Value.EffectiveDateUtc, RateProvider: rate.Value.Provider,
                BaseCreditTotal: credit, BaseDebitTotal: debit, BaseNetTotal: net, Note: null));
        }

        var rateBasis = string.Join(
            ", ",
            entries.Where(item => item.Converted && item.Currency != baseCurrency)
                .Select(item => $"{item.Currency}@{item.RateUsed:0.####} ({item.RateEffectiveDateUtc:yyyy-MM-dd}, {item.RateProvider})"));
        rateBasis = rateBasis.Length > 0 ? rateBasis : "base-currency activity only";
        var netBaseText = baseNet.ToString("0.00", CultureInfo.InvariantCulture);
        var unconvertedText = unconverted > 0
            ? $" with {unconverted} currency(ies) unconverted (no quotation at period end) and excluded from the base totals"
            : string.Empty;
        var attestation =
            $"Base-currency revenue for {fromUtc:yyyy-MM-ddTHH:mm:ssZ} to {toUtc:yyyy-MM-ddTHH:mm:ssZ}, expressed in {baseCurrency}: "
            + $"per-currency totals converted at the period-end rate [{rateBasis}], "
            + $"net {netBaseText} {baseCurrency}"
            + unconvertedText
            + ".";

        return new BaseCurrencyRevenueReport(
            fromUtc,
            toUtc,
            baseCurrency,
            SystemClock.UtcNow,
            entries,
            decimal.Round(baseCredit, 2, MidpointRounding.AwayFromZero),
            decimal.Round(baseDebit, 2, MidpointRounding.AwayFromZero),
            decimal.Round(baseNet, 2, MidpointRounding.AwayFromZero),
            unconverted,
            attestation);
    }

    /// <inheritdoc />
    public async Task<ConversionCostReport> GetConversionCostReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var baseCurrency = await RequireBaseCurrencyAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var dailyTotals = await revenueEventRepository
            .GetDailyTotalsAsync(fromUtc, toUtc, tenantId, cancellationToken)
            .ConfigureAwait(false);

        var perCurrency = dailyTotals
            .Where(total => total.Currency != baseCurrency)
            .GroupBy(total => total.Currency, StringComparer.Ordinal)
            .Select(group => new { Currency = group.Key, Net = group.Sum(total => total.NetTotal) })
            .OrderBy(entry => entry.Currency, StringComparer.Ordinal)
            .ToList();

        var entries = new List<ConversionCostEntry>();
        var totalCost = 0m;

        foreach (var entry in perCurrency)
        {
            var market = await ResolvePeriodRateAsync(baseCurrency, entry.Currency, toUtc, cancellationToken).ConfigureAwait(false);
            var overrideRate = await ResolveActiveOverrideRateAsync(baseCurrency, entry.Currency, cancellationToken).ConfigureAwait(false);

            decimal? baseAtMarket = market is { Rate: > 0m }
                ? decimal.Round(entry.Net / market.Value.Rate, 2, MidpointRounding.AwayFromZero)
                : null;
            decimal? baseAtOverride = overrideRate is > 0m
                ? decimal.Round(entry.Net / overrideRate.Value, 2, MidpointRounding.AwayFromZero)
                : null;
            decimal? cost = baseAtMarket is not null && baseAtOverride is not null
                ? decimal.Round(Math.Abs(baseAtMarket.Value - baseAtOverride.Value), 2, MidpointRounding.AwayFromZero)
                : null;

            if (cost is not null)
            {
                totalCost += cost.Value;
            }

            entries.Add(new ConversionCostEntry(
                entry.Currency,
                entry.Net,
                market?.Rate,
                market?.EffectiveDateUtc,
                overrideRate,
                baseAtMarket,
                baseAtOverride,
                cost,
                Note: market is null ? "no-market-rate" : overrideRate is null ? "no-override" : null));
        }

        return new ConversionCostReport(
            fromUtc,
            toUtc,
            baseCurrency,
            SystemClock.UtcNow,
            entries,
            decimal.Round(totalCost, 2, MidpointRounding.AwayFromZero));
    }

    /// <inheritdoc />
    public async Task<RevenueReportExport> ExportBaseCurrencyReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var report = await GetBaseCurrencyRevenueReportAsync(fromUtc, toUtc, tenantId, cancellationToken).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine("currency,credit_total,debit_total,net_total,event_count,converted,rate_used,rate_effective_date,rate_provider,base_credit_total,base_debit_total,base_net_total,note");
        foreach (var entry in report.Entries)
        {
            builder.AppendLine(string.Join(',',
                Escape(entry.Currency),
                Format(entry.CreditTotal),
                Format(entry.DebitTotal),
                Format(entry.NetTotal),
                entry.EventCount,
                entry.Converted,
                entry.RateUsed is { } rate ? rate.ToString("0.############", CultureInfo.InvariantCulture) : string.Empty,
                entry.RateEffectiveDateUtc is { } effectiveDate ? effectiveDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty,
                entry.RateProvider is null ? string.Empty : Escape(entry.RateProvider),
                entry.BaseCreditTotal is { } baseCredit ? Format(baseCredit) : string.Empty,
                entry.BaseDebitTotal is { } baseDebit ? Format(baseDebit) : string.Empty,
                entry.BaseNetTotal is { } baseNet ? Format(baseNet) : string.Empty,
                entry.Note is null ? string.Empty : Escape(entry.Note)));
        }

        builder.AppendLine(string.Join(',',
            $"total_{Escape(report.BaseCurrency)}",
            Format(report.BaseCreditTotal),
            Format(report.BaseDebitTotal),
            Format(report.BaseNetTotal),
            string.Empty,
            report.UnconvertedCurrencies == 0 ? "true" : "false",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            report.UnconvertedCurrencies > 0 ? $"{report.UnconvertedCurrencies} unconverted" : string.Empty));

        var periodLabel = $"{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}";
        return new RevenueReportExport(
            $"revenue-base-currency-{periodLabel}.csv",
            "text/csv; charset=utf-8",
            builder.ToString());
    }
}
