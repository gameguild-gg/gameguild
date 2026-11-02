namespace GameGuild.Commerce.Payments;

/// <summary>
///     Aggregated performance of one gateway over one rolling window, computed from
///     persisted payment records (issue #413).
/// </summary>
/// <param name="ProviderId">Provider identifier of the gateway.</param>
/// <param name="Window">Window label (24h, 7d or 30d).</param>
/// <param name="SinceUtc">Inclusive window start (UTC).</param>
/// <param name="Attempts">Payments recorded for the gateway inside the window.</param>
/// <param name="Succeeded">Payments that reached <see cref="PaymentStatus.Succeeded" />.</param>
/// <param name="Failed">Payments that reached <see cref="PaymentStatus.Failed" />.</param>
/// <param name="Unresolved">Payments still pending/processing/requiring action inside the window.</param>
/// <param name="SuccessRate">Succeeded / Attempts; 0 when no attempts.</param>
/// <param name="AverageProcessingLatencyMs">
///     Mean of <c>ProcessedAt - CreatedAt</c> over succeeded payments, milliseconds; null
///     when no succeeded payment has both timestamps.
/// </param>
public sealed record GatewayWindowMetrics(
    string ProviderId,
    string Window,
    DateTime SinceUtc,
    int Attempts,
    int Succeeded,
    int Failed,
    int Unresolved,
    double SuccessRate,
    double? AverageProcessingLatencyMs);

/// <summary>
///     Per-gateway success-rate and latency aggregates over rolling 24h/7d/30d windows,
///     computed from payment records (issue #413). Together with the provider health surface
///     from the #780 external-providers API and <see cref="IGatewayCircuitBreaker" />
///     snapshots this forms the per-gateway health and performance picture.
/// </summary>
public interface IGatewayMetricsService
{
    /// <summary>
    ///     Computes the 24h/7d/30d window aggregates for every provider found in the
    ///     payment records. Providers without payments in a window are omitted from that
    ///     window's list (no zero-inflation). An optional tenant scope restricts the
    ///     aggregated payment records to one tenant.
    /// </summary>
    Task<IReadOnlyList<GatewayWindowMetrics>> GetWindowMetricsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Computes the expanded gateway analytics (issue #413 analytics remainders): the
    ///     rolling-window aggregates plus the error-rate-by-type breakdown (failed payments
    ///     grouped by gateway error code), the per-gateway cost analysis (succeeded volume
    ///     priced through the configured fee schedule) and the geographic performance
    ///     breakdown (attempts and success rate per billing country).
    /// </summary>
    Task<GatewayAnalyticsReport> GetAnalyticsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Expanded per-gateway analytics over the rolling windows (issue #413 analytics
///     remainders): window aggregates, error-type breakdown, cost analysis and geographic
///     breakdown in one report.
/// </summary>
/// <param name="GeneratedAtUtc">UTC instant the report was generated.</param>
/// <param name="Windows">Rolling-window aggregates per provider with payments in the window.</param>
/// <param name="ErrorBreakdown">Failed-payment counts per provider, window and gateway error code.</param>
/// <param name="CostAnalysis">Per-provider succeeded volume priced through the configured fee schedule (30d window).</param>
/// <param name="GeographicBreakdown">Attempts and success rate per provider, window and billing country.</param>
public sealed record GatewayAnalyticsReport(
    DateTime GeneratedAtUtc,
    IReadOnlyList<GatewayWindowMetrics> Windows,
    IReadOnlyList<GatewayErrorTypeMetrics> ErrorBreakdown,
    IReadOnlyList<GatewayCostMetrics> CostAnalysis,
    IReadOnlyList<GatewayGeoMetrics> GeographicBreakdown);

/// <summary>
///     Error-rate-by-type row (issue #413): failed payments of one provider in one window,
///     grouped by the gateway error code recorded on the payment.
/// </summary>
/// <param name="ProviderId">Provider identifier of the gateway.</param>
/// <param name="Window">Window label (24h, 7d or 30d).</param>
/// <param name="ErrorCode">Gateway error code; <c>(unclassified)</c> when none was recorded.</param>
/// <param name="Count">Failed payments with that error code inside the window.</param>
/// <param name="ShareOfFailures">Count divided by the provider's failed payments in the window.</param>
public sealed record GatewayErrorTypeMetrics(
    string ProviderId,
    string Window,
    string ErrorCode,
    int Count,
    double ShareOfFailures);

/// <summary>
///     Cost-analysis row (issue #413): one provider's succeeded payment volume in the 30d
///     window priced through its configured fee schedule. Providers without a complete fee
///     schedule report the raw volume with a null estimate (unknown cost is never zero).
/// </summary>
/// <param name="ProviderId">Provider identifier of the gateway.</param>
/// <param name="SucceededCount">Succeeded payments in the 30d window.</param>
/// <param name="SucceededVolume">Sum of the succeeded payment amounts (major units) in the 30d window.</param>
/// <param name="FeeFixedAmount">Configured per-transaction fixed fee; null when no schedule is configured.</param>
/// <param name="FeePercentBasisPoints">Configured percentage fee in basis points (100 bp = 1%); null when no schedule is configured.</param>
/// <param name="EstimatedFee">Fixed fee x succeeded count + percentage fee x volume; null without a complete schedule.</param>
public sealed record GatewayCostMetrics(
    string ProviderId,
    int SucceededCount,
    decimal SucceededVolume,
    decimal? FeeFixedAmount,
    int? FeePercentBasisPoints,
    decimal? EstimatedFee);

/// <summary>
///     Geographic performance row (issue #413): attempts and success rate of one provider in
///     one window, grouped by the billing country recorded on the payment.
/// </summary>
/// <param name="ProviderId">Provider identifier of the gateway.</param>
/// <param name="Window">Window label (24h, 7d or 30d).</param>
/// <param name="Country">ISO 3166-1 alpha-2 billing country; <c>(unknown)</c> when the payment recorded none.</param>
/// <param name="Attempts">Payments of that country inside the window.</param>
/// <param name="Succeeded">Succeeded payments of that country inside the window.</param>
/// <param name="SuccessRate">Succeeded / Attempts; 0 when no attempts.</param>
public sealed record GatewayGeoMetrics(
    string ProviderId,
    string Window,
    string Country,
    int Attempts,
    int Succeeded,
    double SuccessRate);

/// <summary>
///     Default <see cref="IGatewayMetricsService" />: reads payment records through
/// <see cref="IPaymentRepository" /> and aggregates them in memory.
/// </summary>
public sealed class GatewayMetricsService(
    IPaymentRepository paymentRepository,
    Microsoft.Extensions.Options.IOptions<GatewayRoutingOptions> routingOptions) : IGatewayMetricsService
{
    private static readonly (string Label, TimeSpan Duration)[] Windows =
    [
        ("24h", TimeSpan.FromHours(24)),
        ("7d", TimeSpan.FromDays(7)),
        ("30d", TimeSpan.FromDays(30))
    ];

    private const string ThirtyDayWindowLabel = "30d";

    /// <inheritdoc />
    public async Task<IReadOnlyList<GatewayWindowMetrics>> GetWindowMetricsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var metrics = new List<GatewayWindowMetrics>();

        foreach (var (label, duration) in Windows)
        {
            var since = now.Subtract(duration);
            var payments = await paymentRepository
                .GetByDateRangeAsync(since, now, tenantId, cancellationToken)
                .ConfigureAwait(false);

            metrics.AddRange(
                payments
                    .GroupBy(payment => payment.Provider, StringComparer.OrdinalIgnoreCase)
                    .Select(group => Aggregate(group.Key, group.ToList(), label, since)));
        }

        return metrics;
    }

    /// <inheritdoc />
    public async Task<GatewayAnalyticsReport> GetAnalyticsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var windowMetrics = new List<GatewayWindowMetrics>();
        var errorBreakdown = new List<GatewayErrorTypeMetrics>();
        var geoBreakdown = new List<GatewayGeoMetrics>();
        IEnumerable<Payment>? thirtyDayPayments = null;

        foreach (var (label, duration) in Windows)
        {
            var since = now.Subtract(duration);
            var payments = await paymentRepository
                .GetByDateRangeAsync(since, now, tenantId, cancellationToken)
                .ConfigureAwait(false);

            if (label == ThirtyDayWindowLabel)
            {
                thirtyDayPayments = payments;
            }

            foreach (var providerGroup in payments.GroupBy(payment => payment.Provider, StringComparer.OrdinalIgnoreCase))
            {
                var providerPayments = providerGroup.ToList();
                var providerId = providerGroup.Key.ToLowerInvariant();
                windowMetrics.Add(Aggregate(providerGroup.Key, providerPayments, label, since));

                errorBreakdown.AddRange(BuildErrorBreakdown(providerId, providerPayments, label));
                geoBreakdown.AddRange(BuildGeoBreakdown(providerId, providerPayments, label));
            }
        }

        return new GatewayAnalyticsReport(
            GeneratedAtUtc: now,
            Windows: windowMetrics,
            ErrorBreakdown: errorBreakdown,
            CostAnalysis: BuildCostAnalysis(thirtyDayPayments ?? []),
            GeographicBreakdown: geoBreakdown);
    }

    private static IEnumerable<GatewayErrorTypeMetrics> BuildErrorBreakdown(
        string providerId,
        IReadOnlyList<Payment> payments,
        string window)
    {
        var failed = payments.Where(payment => payment.Status == PaymentStatus.Failed).ToList();

        return failed
            .GroupBy(payment => payment.ErrorCode ?? "(unclassified)", StringComparer.OrdinalIgnoreCase)
            .Select(group => new GatewayErrorTypeMetrics(
                providerId,
                window,
                group.Key,
                group.Count(),
                failed.Count == 0 ? 0.0 : (double)group.Count() / failed.Count));
    }

    private static IEnumerable<GatewayGeoMetrics> BuildGeoBreakdown(
        string providerId,
        IReadOnlyList<Payment> payments,
        string window) =>
        payments
            .GroupBy(payment => payment.BillingCountry ?? "(unknown)", StringComparer.OrdinalIgnoreCase)
            .Select(group => new GatewayGeoMetrics(
                providerId,
                window,
                group.Key.ToUpperInvariant(),
                group.Count(),
                group.Count(payment => payment.Status == PaymentStatus.Succeeded),
                !group.Any()
                    ? 0.0
                    : (double)group.Count(payment => payment.Status == PaymentStatus.Succeeded) / group.Count()));

    private List<GatewayCostMetrics> BuildCostAnalysis(IEnumerable<Payment> payments) =>
        payments
            .GroupBy(payment => payment.Provider, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var providerId = group.Key.ToLowerInvariant();
                var succeeded = group.Where(payment => payment.Status == PaymentStatus.Succeeded).ToList();
                var volume = succeeded.Sum(payment => payment.Amount);
                var entry = routingOptions.Value.FindEntry(providerId);

                return new GatewayCostMetrics(
                    providerId,
                    succeeded.Count,
                    volume,
                    entry?.FeeFixedAmount,
                    entry?.FeePercentBasisPoints,
                    // Per-transaction fixed fee applies to every succeeded charge, the
                    // percentage fee to the volume; both only exist with a complete schedule.
                    entry?.HasFeeSchedule == true
                        ? entry.FeeFixedAmount!.Value * succeeded.Count + volume * entry.FeePercentBasisPoints!.Value / 10000m
                        : null);
            })
            .ToList();

    private static GatewayWindowMetrics Aggregate(
        string providerId,
        IReadOnlyList<Payment> payments,
        string window,
        DateTime sinceUtc)
    {
        var succeeded = payments.Count(payment => payment.Status == PaymentStatus.Succeeded);
        var failed = payments.Count(payment => payment.Status == PaymentStatus.Failed);
        var unresolved = payments.Count(payment =>
            payment.Status is PaymentStatus.Pending
                or PaymentStatus.Processing
                or PaymentStatus.RequiresAction);

        var latencySamples = payments
            .Where(payment => payment.Status == PaymentStatus.Succeeded && payment.ProcessedAt.HasValue)
            .Select(payment => (payment.ProcessedAt!.Value - payment.CreatedAt).TotalMilliseconds)
            .Where(milliseconds => milliseconds >= 0)
            .ToList();

        return new GatewayWindowMetrics(
            ProviderId: providerId.ToLowerInvariant(),
            Window: window,
            SinceUtc: sinceUtc,
            Attempts: payments.Count,
            Succeeded: succeeded,
            Failed: failed,
            Unresolved: unresolved,
            SuccessRate: payments.Count == 0 ? 0.0 : (double)succeeded / payments.Count,
            AverageProcessingLatencyMs: latencySamples.Count == 0
                ? null
                : Math.Round(latencySamples.Average(), 2, MidpointRounding.AwayFromZero));
    }
}
