namespace GameGuild.Commerce.Payments;

/// <summary>Grouped totals for one key of a grouping dimension (event type, source or status), in one currency.</summary>
/// <param name="Key">Grouping key (enum name).</param>
/// <param name="Currency">ISO-4217 currency code the totals are denominated in. Amounts are never consolidated across currencies.</param>
/// <param name="Count">Number of revenue events in the group.</param>
/// <param name="Total">Sum of event amounts in the group, in <paramref name="Currency" />.</param>
public sealed record RevenueEventGroupTotal(string Key, string Currency, int Count, decimal Total);

/// <summary>Dimension used to group revenue totals in reports.</summary>
public enum RevenueEventTotalGrouping
{
    /// <summary>Group totals by <see cref="RevenueEventType" />.</summary>
    EventType = 0,

    /// <summary>Group totals by <see cref="RevenueSource" />.</summary>
    Source = 1,

    /// <summary>Group totals by <see cref="RevenueEventStatus" />.</summary>
    Status = 2
}

/// <summary>Net revenue total for one UTC day and one currency.</summary>
/// <param name="DateUtc">UTC day (midnight) the totals belong to.</param>
/// <param name="Currency">ISO-4217 currency code the totals are denominated in. Amounts are never consolidated across currencies.</param>
/// <param name="CreditTotal">Sum of credit-side revenue event amounts, in <paramref name="Currency" />.</param>
/// <param name="DebitTotal">Sum of debit-side revenue event amounts (refunds, chargebacks, fees), in <paramref name="Currency" />.</param>
/// <param name="NetTotal">CreditTotal minus DebitTotal.</param>
/// <param name="EventCount">Number of revenue events counted for the day in this currency.</param>
public sealed record RevenueDailyTotal(
    DateTime DateUtc,
    string Currency,
    decimal CreditTotal,
    decimal DebitTotal,
    decimal NetTotal,
    int EventCount);

/// <summary>One point of a historical revenue trend, for one currency.</summary>
/// <param name="DateUtc">UTC day (midnight) the point covers.</param>
/// <param name="Currency">ISO-4217 currency code the point is denominated in.</param>
/// <param name="CreditTotal">Credit-side total for the day, in <paramref name="Currency" />.</param>
/// <param name="DebitTotal">Debit-side total for the day, in <paramref name="Currency" />.</param>
/// <param name="NetTotal">Net total for the day, in <paramref name="Currency" />.</param>
/// <param name="EventCount">Revenue events counted for the day in this currency.</param>
public sealed record RevenueTrendPoint(
    DateTime DateUtc,
    string Currency,
    decimal CreditTotal,
    decimal DebitTotal,
    decimal NetTotal,
    int EventCount);

/// <summary>Range totals of a historical revenue trend for one currency.</summary>
/// <param name="Currency">ISO-4217 currency code the totals are denominated in.</param>
/// <param name="TotalCredit">Credit total across the range, in <paramref name="Currency" />.</param>
/// <param name="TotalDebit">Debit total across the range, in <paramref name="Currency" />.</param>
/// <param name="TotalNet">Net total across the range, in <paramref name="Currency" />.</param>
public sealed record RevenueTrendCurrencyTotal(
    string Currency,
    decimal TotalCredit,
    decimal TotalDebit,
    decimal TotalNet);

/// <summary>Historical revenue trend over an inclusive date range, broken down by currency.</summary>
/// <param name="FromUtc">Start of the range (inclusive).</param>
/// <param name="ToUtc">End of the range (inclusive).</param>
/// <param name="Points">One point per (UTC day, currency) pair for every currency observed in the range, including zero-activity days for those currencies. Empty when the range has no revenue events, since zero amounts cannot be attributed to a currency.</param>
/// <param name="TotalsByCurrency">Range totals per currency observed in the period. Amounts are never consolidated across currencies.</param>
public sealed record RevenueTrendReport(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<RevenueTrendPoint> Points,
    IReadOnlyList<RevenueTrendCurrencyTotal> TotalsByCurrency);

/// <summary>One ranked currency in the popularity report (issue #412 spec G).</summary>
/// <param name="Rank">Rank position (1 = most transacted).</param>
/// <param name="Currency">ISO-4217 currency code the entry is denominated in.</param>
/// <param name="TransactionCount">Revenue events counted in this currency over the period.</param>
/// <param name="TotalCredit">Credit-side total in <paramref name="Currency" />.</param>
/// <param name="TotalDebit">Debit-side total in <paramref name="Currency" />.</param>
/// <param name="TotalNet">Net total in <paramref name="Currency" />; never consolidated across currencies.</param>
public sealed record CurrencyPopularityEntry(
    int Rank,
    string Currency,
    int TransactionCount,
    decimal TotalCredit,
    decimal TotalDebit,
    decimal TotalNet);

/// <summary>Most-transacted currencies over an inclusive period (issue #412 spec G).</summary>
/// <param name="FromUtc">Start of the period (inclusive).</param>
/// <param name="ToUtc">End of the period (inclusive).</param>
/// <param name="GeneratedAtUtc">Moment the report was produced.</param>
/// <param name="Currencies">Currencies ranked by transaction count (then net volume, then code), limited to the requested size.</param>
public sealed record CurrencyPopularityReport(
    DateTime FromUtc,
    DateTime ToUtc,
    DateTime GeneratedAtUtc,
    IReadOnlyList<CurrencyPopularityEntry> Currencies);

/// <summary>Reconciliation coverage summary included in compliance reports.</summary>
/// <param name="ReconciliationRuns">Number of reconciliation runs overlapping the period.</param>
/// <param name="MatchedLines">Total statement lines matched by those runs.</param>
/// <param name="Discrepancies">Total discrepancies recorded by those runs.</param>
/// <param name="LastRunCompletedAtUtc">Completion timestamp of the most recent run, if any.</param>
public sealed record RevenueReconciliationCoverage(
    int ReconciliationRuns,
    int MatchedLines,
    int Discrepancies,
    DateTime? LastRunCompletedAtUtc);

/// <summary>Compliance-grade revenue summary for an inclusive period, broken down by currency.</summary>
/// <param name="FromUtc">Start of the period (inclusive).</param>
/// <param name="ToUtc">End of the period (inclusive).</param>
/// <param name="GeneratedAtUtc">Moment the report was produced.</param>
/// <param name="TotalsByEventType">Revenue totals grouped by (event type, currency).</param>
/// <param name="TotalsBySource">Revenue totals grouped by (source, currency).</param>
/// <param name="TotalsByStatus">Revenue totals grouped by (processing status, currency).</param>
/// <param name="UncountedEventCount">Revenue events still pending processing (not counted as recognized revenue).</param>
/// <param name="Reconciliation">Reconciliation coverage across the period.</param>
/// <param name="Attestation">Human-readable attestation statement for compliance filings. States gross revenue per currency; no cross-currency consolidation is performed.</param>
public sealed record RevenueComplianceReport(
    DateTime FromUtc,
    DateTime ToUtc,
    DateTime GeneratedAtUtc,
    IReadOnlyList<RevenueEventGroupTotal> TotalsByEventType,
    IReadOnlyList<RevenueEventGroupTotal> TotalsBySource,
    IReadOnlyList<RevenueEventGroupTotal> TotalsByStatus,
    int UncountedEventCount,
    RevenueReconciliationCoverage Reconciliation,
    string Attestation);

/// <summary>Serialized revenue report ready for delivery to an accounting/ERP system.</summary>
/// <param name="FileName">Suggested file name for the export.</param>
/// <param name="ContentType">MIME type of the payload (text/csv or application/json).</param>
/// <param name="Content">Serialized report content.</param>
public sealed record RevenueReportExport(string FileName, string ContentType, string Content);

/// <summary>One per-currency row of the base-currency revenue report (issue #412 spec G).</summary>
/// <param name="Currency">Transaction currency of the row.</param>
/// <param name="CreditTotal">Gross credits in the transaction currency.</param>
/// <param name="DebitTotal">Gross debits (refunds) in the transaction currency.</param>
/// <param name="NetTotal">Net revenue in the transaction currency.</param>
/// <param name="EventCount">Revenue events behind the row.</param>
/// <param name="Converted">Whether the base-currency figures could be computed (a quotation existed at the period-end rate).</param>
/// <param name="RateUsed">Quotation applied (transaction-currency units per one base unit) when converted.</param>
/// <param name="RateEffectiveDateUtc">Effective date of the applied quotation.</param>
/// <param name="RateProvider">Provider of the applied quotation.</param>
/// <param name="BaseCreditTotal">Credit total expressed in the base currency.</param>
/// <param name="BaseDebitTotal">Debit total expressed in the base currency.</param>
/// <param name="BaseNetTotal">Net revenue expressed in the base currency.</param>
/// <param name="Note">Why the row is unconverted, when applicable.</param>
public sealed record BaseCurrencyRevenueEntry(
    string Currency,
    decimal CreditTotal,
    decimal DebitTotal,
    decimal NetTotal,
    int EventCount,
    bool Converted,
    decimal? RateUsed,
    DateTime? RateEffectiveDateUtc,
    string? RateProvider,
    decimal? BaseCreditTotal,
    decimal? BaseDebitTotal,
    decimal? BaseNetTotal,
    string? Note);

/// <summary>
///     Base-currency revenue report + reconciliation (issue #412 specs F/G): every transacted
///     currency of the period with its per-currency totals and the base-currency equivalent
///     computed at the period-end rate, plus the reconciled grand totals in base currency.
///     Unconvertible currencies are listed fail-closed (never silently merged) and excluded
///     from the base totals.
/// </summary>
/// <param name="FromUtc">Inclusive period start (UTC).</param>
/// <param name="ToUtc">Inclusive period end (UTC).</param>
/// <param name="BaseCurrency">Tenant base (accounting) currency the report is expressed in.</param>
/// <param name="GeneratedAtUtc">Report generation instant.</param>
/// <param name="Entries">One row per transacted currency.</param>
/// <param name="BaseCreditTotal">Grand credit total in base currency (converted rows only).</param>
/// <param name="BaseDebitTotal">Grand debit total in base currency (converted rows only).</param>
/// <param name="BaseNetTotal">Grand net total in base currency (converted rows only).</param>
/// <param name="UnconvertedCurrencies">Currencies that could not be converted (no quotation at period end).</param>
/// <param name="Attestation">Human-readable attestation naming the rate basis per currency.</param>
public sealed record BaseCurrencyRevenueReport(
    DateTime FromUtc,
    DateTime ToUtc,
    string BaseCurrency,
    DateTime GeneratedAtUtc,
    IReadOnlyList<BaseCurrencyRevenueEntry> Entries,
    decimal BaseCreditTotal,
    decimal BaseDebitTotal,
    decimal BaseNetTotal,
    int UnconvertedCurrencies,
    string Attestation);

/// <summary>One per-currency row of the conversion-cost analysis (issue #412 spec G).</summary>
/// <param name="Currency">Transaction currency of the row.</param>
/// <param name="NetVolume">Net revenue volume in the transaction currency over the period.</param>
/// <param name="MarketRate">Market quotation at period end (transaction-currency units per one base unit).</param>
/// <param name="MarketRateEffectiveDateUtc">Effective date of the market quotation.</param>
/// <param name="OverrideRate">Active operator override quotation for the same pair, when any.</param>
/// <param name="BaseAtMarketRate">Net volume converted to base at the market rate.</param>
/// <param name="BaseAtOverrideRate">Net volume converted to base at the override rate.</param>
/// <param name="CostInBase">Absolute base-currency difference between the two conversion legs — the pricing decision's conversion cost.</param>
/// <param name="Note">Why parts of the row are unavailable (e.g. "no-market-rate").</param>
public sealed record ConversionCostEntry(
    string Currency,
    decimal NetVolume,
    decimal? MarketRate,
    DateTime? MarketRateEffectiveDateUtc,
    decimal? OverrideRate,
    decimal? BaseAtMarketRate,
    decimal? BaseAtOverrideRate,
    decimal? CostInBase,
    string? Note);

/// <summary>
///     Currency conversion costs analysis (issue #412 spec G): for every non-base transacted
///     currency, the base-currency value of its net volume at the market rate versus at the
///     operator override rate — quantifying what pinned rates cost (or save) per market.
/// </summary>
/// <param name="FromUtc">Inclusive period start (UTC).</param>
/// <param name="ToUtc">Inclusive period end (UTC).</param>
/// <param name="BaseCurrency">Tenant base currency.</param>
/// <param name="GeneratedAtUtc">Report generation instant.</param>
/// <param name="Entries">One row per transacted non-base currency.</param>
/// <param name="TotalCostInBase">Sum of per-currency costs in base currency.</param>
public sealed record ConversionCostReport(
    DateTime FromUtc,
    DateTime ToUtc,
    string BaseCurrency,
    DateTime GeneratedAtUtc,
    IReadOnlyList<ConversionCostEntry> Entries,
    decimal TotalCostInBase);
