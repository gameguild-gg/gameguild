namespace GameGuild.Commerce.Payments;

/// <summary>
///     Input for a reconciliation run (issue #404). When <see cref="Lines" /> is null the
///     lines are pulled from the configured <see cref="IExternalRevenueStatementSource" />.
/// </summary>
/// <param name="TenantId">Optional tenant scope; null reconciles the global aggregate.</param>
/// <param name="Source">External system the statement came from.</param>
/// <param name="PeriodStartUtc">Inclusive period start (UTC).</param>
/// <param name="PeriodEndUtc">Inclusive period end (UTC).</param>
/// <param name="Lines">Inline statement lines, or null to use the configured external source.</param>
/// <param name="ExternalStatementId">Optional statement/batch identifier for traceability.</param>
/// <param name="InitiatedByUserId">Optional actor that initiated the run.</param>
public sealed record RevenueReconciliationRequest(
    Guid? TenantId,
    string Source,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    IReadOnlyList<ExternalRevenueStatementLine>? Lines,
    string? ExternalStatementId = null,
    Guid? InitiatedByUserId = null);

/// <summary>
///     External reconciliation and discrepancy detection (issue #404): compares external
///     accounting/ERP statements against internally recorded revenue events and records a
///     durable, immutable run with every mismatch.
/// </summary>
public interface IRevenueReconciliationService
{
    /// <summary>Run a reconciliation for the requested period and return the committed run.</summary>
    /// <param name="request">Reconciliation input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The committed reconciliation run with discrepancies persisted.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resolved statement exceeds the configured line limit.</exception>
    Task<RevenueReconciliationRun> ReconcileAsync(RevenueReconciliationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>An anomaly candidate computed by the detector before persistence, for one currency.</summary>
/// <param name="Kind">Kind of anomaly.</param>
/// <param name="DetectedForDateUtc">UTC day (midnight) the anomaly belongs to.</param>
/// <param name="Currency">ISO-4217 currency code of the daily net revenue series the candidate was computed from. Baselines and z-scores are never mixed across currencies.</param>
/// <param name="ObservedNetRevenue">Net revenue observed for the day, in <paramref name="Currency" />.</param>
/// <param name="ExpectedNetRevenue">Baseline mean net revenue, in <paramref name="Currency" />.</param>
/// <param name="ZScore">Standard score of the observation (rounded to four decimals).</param>
/// <param name="BaselineDays">Number of baseline days used.</param>
public sealed record RevenueAnomalyCandidate(
    RevenueAnomalyKind Kind,
    DateTime DetectedForDateUtc,
    string Currency,
    decimal ObservedNetRevenue,
    decimal ExpectedNetRevenue,
    decimal ZScore,
    int BaselineDays);

/// <summary>
///     Statistical anomaly detection over daily net revenue (issue #404): flags days whose
///     net revenue deviates from the trailing baseline by at least the configured z-score.
///     Baselines and z-scores are computed per currency so amounts are never mixed across
///     units.
/// </summary>
public interface IRevenueAnomalyService
{
    /// <summary>Evaluate the most recent days (per options) per currency and return anomaly candidates without persisting them.</summary>
    /// <param name="evaluationDateUtc">Reference "today"; evaluation covers the preceding days per options.</param>
    /// <param name="tenantId">Optional tenant scope; null evaluates the global aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Anomaly candidates, oldest evaluated day first, one series per observed currency.</returns>
    Task<IReadOnlyList<RevenueAnomalyCandidate>> DetectAsync(
        DateTime evaluationDateUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Evaluate the most recent days and persist alerts, skipping (kind, day, currency) pairs already alerted.</summary>
    /// <param name="evaluationDateUtc">Reference "today"; evaluation covers the preceding days per options.</param>
    /// <param name="tenantId">Optional tenant scope; null evaluates the global aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of new alerts created.</returns>
    Task<int> DetectAndPersistAsync(
        DateTime evaluationDateUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Compliance reporting, historical trend analysis and export serialization for
///     revenue auditing (issue #404).
/// </summary>
public interface IRevenueReportService
{
    /// <summary>Build the compliance report for an inclusive period.</summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="tenantId">Optional tenant scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compliance report.</returns>
    Task<RevenueComplianceReport> GetComplianceReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Build the daily net-revenue trend for an inclusive period, per currency.</summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="tenantId">Optional tenant scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trend report with one point per (day, currency) for every observed currency, including zero-activity days, and range totals per currency.</returns>
    Task<RevenueTrendReport> GetTrendReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rank the most-transacted currencies over an inclusive period (issue #412 spec G):
    /// per-currency transaction counts and credit/debit/net totals, ordered by transaction
    /// count (then net volume, then code). Amounts are never consolidated across currencies.
    /// </summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="tenantId">Optional tenant scope.</param>
    /// <param name="limit">Maximum ranked entries (1..50, default 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ranked popularity report.</returns>
    Task<CurrencyPopularityReport> GetPopularCurrenciesAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>Serialize the compliance and trend reports for external delivery.</summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="format">Export format: "csv" or "json".</param>
    /// <param name="tenantId">Optional tenant scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The serialized export with file name and content type.</returns>
    /// <exception cref="ArgumentException">Thrown when the format is not csv or json.</exception>
    Task<RevenueReportExport> ExportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        string format,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Base-currency revenue report + reconciliation (issue #412 specs F/G): per-currency
    /// totals converted to the tenant base currency at the period-end rate, with reconciled
    /// grand totals. Unconvertible currencies stay listed but excluded from base totals.
    /// </summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="tenantId">Tenant scope (required: the base currency is per tenant).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The base-currency revenue report.</returns>
    /// <exception cref="BusinessRuleViolationException">
    /// Thrown when the tenant has no base currency configured or no tenant scope is given.
    /// </exception>
    Task<BaseCurrencyRevenueReport> GetBaseCurrencyRevenueReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Currency conversion costs analysis (issue #412 spec G): per non-base transacted
    /// currency, the base value of its net volume at the market rate versus at the active
    /// operator override rate, quantifying the cost of pinned rates per market.
    /// </summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="tenantId">Tenant scope (required: the base currency is per tenant).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion-cost report.</returns>
    /// <exception cref="BusinessRuleViolationException">
    /// Thrown when the tenant has no base currency configured or no tenant scope is given.
    /// </exception>
    Task<ConversionCostReport> GetConversionCostReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Serializes the base-currency revenue report as RFC 4180 CSV.</summary>
    /// <param name="fromUtc">Inclusive period start (UTC).</param>
    /// <param name="toUtc">Inclusive period end (UTC).</param>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CSV export with file name and content type.</returns>
    Task<RevenueReportExport> ExportBaseCurrencyReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
