using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Trend bucket granularity of the fraud statistics report (issue #409: weekly and
///     monthly trend views alongside the daily default).
/// </summary>
public enum FraudTrendGranularity
{
    /// <summary>One point per UTC day (the original default).</summary>
    Daily = 0,

    /// <summary>One point per ISO week (bucketed at Monday 00:00 UTC).</summary>
    Weekly = 1,

    /// <summary>One point per calendar month (bucketed at the first of the month, UTC).</summary>
    Monthly = 2
}

/// <summary>
///     Fraud-program statistics over a date window (issue #409 reporting): assessed,
///     flagged and blocked payment counts with rates, the chargeback rate among succeeded
///     payments, the false-positive proxy (held reviews later approved), the manual-review
///     backlog with average time-to-review, and the top fraud signals by frequency.
///     Requires the <c>payments:admin</c> permission; the tenant scope is enforced at the
///     API boundary from the actor context.
/// </summary>
/// <param name="TenantId">Optional tenant scope; enforced at the API boundary.</param>
/// <param name="FromUtc">Inclusive window start; defaults to 30 days ago.</param>
/// <param name="ToUtc">Inclusive window end; defaults to now.</param>
/// <param name="TrendGranularity">Bucket size of the trend series (issue #409: weekly/monthly).</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record GetFraudStatisticsQuery(
    Guid? TenantId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    FraudTrendGranularity TrendGranularity = FraudTrendGranularity.Daily) : IQuery<FraudStatisticsReport>;

/// <summary>Combined read model of one fraud-statistics reporting pass.</summary>
public sealed record FraudStatisticsReport
{
    /// <summary>UTC instant the report was generated.</summary>
    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>Inclusive window start used for the aggregates.</summary>
    public required DateTime FromUtc { get; init; }

    /// <summary>Inclusive window end used for the aggregates.</summary>
    public required DateTime ToUtc { get; init; }

    /// <summary>Payment totals inside the window.</summary>
    public required FraudStatisticsTotals Totals { get; init; }

    /// <summary>Derived rates, each null when its denominator is zero.</summary>
    public required FraudStatisticsRates Rates { get; init; }

    /// <summary>Manual-review queue statistics.</summary>
    public required FraudStatisticsReviews Reviews { get; init; }

    /// <summary>Top fraud signal types by frequency inside the window (top 10).</summary>
    public required IReadOnlyList<FraudStatisticsSignal> TopSignals { get; init; }

    /// <summary>Fraud outcomes by billing country inside the window (issue #409: fraud by country).</summary>
    public required IReadOnlyList<FraudStatisticsCountryRow> ByCountry { get; init; }

    /// <summary>Fraud outcomes by gateway payment provider inside the window (issue #409: fraud by payment method).</summary>
    public required IReadOnlyList<FraudStatisticsMethodRow> ByPaymentMethod { get; init; }

    /// <summary>Fraud outcomes by subscription plan inside the window (issue #409: fraud by plan).</summary>
    public required IReadOnlyList<FraudStatisticsPlanRow> ByPlan { get; init; }

    /// <summary>Bucket granularity of <see cref="Trend"/> (issue #409: weekly/monthly views).</summary>
    public required FraudTrendGranularity TrendGranularity { get; init; }

    /// <summary>Trend of created/held/blocked payments inside the window, bucketed by <see cref="TrendGranularity"/> (issue #409: trend analysis).</summary>
    public required IReadOnlyList<FraudStatisticsTrendPoint> Trend { get; init; }
}

/// <summary>Fraud outcomes for one billing country inside the statistics window (issue #409).</summary>
public record FraudStatisticsCountryRow
{
    /// <summary>ISO 3166-1 alpha-2 billing country; "unknown" when the attempt carried none.</summary>
    public required string Country { get; init; }

    /// <summary>Payments created for the country in the window.</summary>
    public required int Created { get; init; }

    /// <summary>Assessed payments held for review in the window.</summary>
    public required int ReviewHeld { get; init; }

    /// <summary>Assessed payments blocked in the window.</summary>
    public required int Blocked { get; init; }
}

/// <summary>Fraud outcomes for one gateway payment provider inside the statistics window (issue #409).</summary>
public record FraudStatisticsMethodRow
{
    /// <summary>Gateway provider that processed the attempts (stripe, paypal, ...).</summary>
    public required string Provider { get; init; }

    /// <summary>Payments created through the provider in the window.</summary>
    public required int Created { get; init; }

    /// <summary>Assessed payments held for review in the window.</summary>
    public required int ReviewHeld { get; init; }

    /// <summary>Assessed payments blocked in the window.</summary>
    public required int Blocked { get; init; }
}

/// <summary>Fraud outcomes for one subscription plan inside the statistics window (issue #409: fraud by plan).</summary>
public record FraudStatisticsPlanRow
{
    /// <summary>Plan the charged subscriptions are on; null when the attempt could not be attributed to a plan.</summary>
    public required Guid? PlanId { get; init; }

    /// <summary>Display name of the plan; "unknown" when unattributed or the plan no longer exists.</summary>
    public required string PlanName { get; init; }

    /// <summary>Payments created for the plan in the window.</summary>
    public required int Created { get; init; }

    /// <summary>Assessed payments held for review in the window.</summary>
    public required int ReviewHeld { get; init; }

    /// <summary>Assessed payments blocked in the window.</summary>
    public required int Blocked { get; init; }
}

/// <summary>One bucket of the fraud-outcome trend inside the statistics window (issue #409).</summary>
public record FraudStatisticsTrendPoint
{
    /// <summary>UTC start of the bucket (day, ISO week or month, per the report granularity).</summary>
    public required DateTime Day { get; init; }

    /// <summary>Payments created in the bucket.</summary>
    public required int Created { get; init; }

    /// <summary>Assessed payments held for review in the bucket.</summary>
    public required int ReviewHeld { get; init; }

    /// <summary>Assessed payments blocked in the bucket.</summary>
    public required int Blocked { get; init; }
}

/// <summary>Payment counts inside the statistics window.</summary>
public record FraudStatisticsTotals
{
    /// <summary>Payments created in the window.</summary>
    public required int PaymentsCreated { get; init; }

    /// <summary>Payments that went through the fraud gate in the window.</summary>
    public required int Assessed { get; init; }

    /// <summary>Assessed payments the gate allowed.</summary>
    public required int Allowed { get; init; }

    /// <summary>Assessed payments the gate held for manual review.</summary>
    public required int ReviewHeld { get; init; }

    /// <summary>Assessed payments the gate blocked.</summary>
    public required int Blocked { get; init; }

    /// <summary>Payments that succeeded in the window.</summary>
    public required int Succeeded { get; init; }

    /// <summary>Payments that ended disputed (chargebacks) in the window.</summary>
    public required int Disputed { get; init; }
}

/// <summary>Derived fraud-program rates; null when the denominator is zero.</summary>
public record FraudStatisticsRates
{
    /// <summary>Flagged rate: (held + blocked) / assessed, 0-100.</summary>
    public double? FlaggedPercent { get; init; }

    /// <summary>Blocked rate: blocked / assessed, 0-100.</summary>
    public double? BlockedPercent { get; init; }

    /// <summary>Chargeback rate: disputed / succeeded, 0-100.</summary>
    public double? ChargebackPercent { get; init; }

    /// <summary>False-positive proxy: held reviews later approved / resolved reviews, 0-100.</summary>
    public double? FalsePositivePercent { get; init; }
}

/// <summary>Manual-review queue statistics (issue #409: backlog and average time to review).</summary>
public record FraudStatisticsReviews
{
    /// <summary>Reviews currently awaiting a decision (all time; the live queue).</summary>
    public required int PendingBacklog { get; init; }

    /// <summary>Reviews resolved in the window.</summary>
    public required int ResolvedInWindow { get; init; }

    /// <summary>Resolved reviews that were approved (held transactions that were legitimate).</summary>
    public required int ApprovedInWindow { get; init; }

    /// <summary>Resolved reviews that were declined (confirmed fraud).</summary>
    public required int DeclinedInWindow { get; init; }

    /// <summary>Average hours from payment creation to review resolution in the window; null when none.</summary>
    public double? AverageReviewHours { get; init; }
}

/// <summary>One fraud signal type with its frequency inside the window.</summary>
public record FraudStatisticsSignal
{
    /// <summary>Stable signal identifier, e.g. <c>Velocity.PaymentFrequency</c>.</summary>
    public required string SignalType { get; init; }

    /// <summary>How often the signal fired in the window.</summary>
    public required int Count { get; init; }
}

/// <summary>Handler for <see cref="GetFraudStatisticsQuery"/>.</summary>
public sealed class GetFraudStatisticsQueryHandler(
    IApplicationDbContext context,
    GameGuild.Commerce.ISubscriptionPaymentContextService? subscriptionContextService = null)
    : IQueryHandler<GetFraudStatisticsQuery, FraudStatisticsReport>
{
    private const int MaxEvidenceRows = 5000;
    private const int TopSignalCount = 10;

    /// <summary>Bound on the distinct subscriptions resolved to plans for the by-plan view.</summary>
    private const int MaxPlanSubscriptionResolutions = 2000;

    /// <inheritdoc />
    public async Task<FraudStatisticsReport> Handle(GetFraudStatisticsQuery request, CancellationToken cancellationToken)
    {
        var now = SystemClock.UtcNow;
        var toUtc = request.ToUtc ?? now;
        var fromUtc = request.FromUtc ?? toUtc.AddDays(-30);
        if (fromUtc > toUtc)
        {
            (fromUtc, toUtc) = (toUtc, fromUtc);
        }

        var payments = context.Set<Payment>().AsNoTracking();

        if (request.TenantId.HasValue)
        {
            var tenantId = request.TenantId.Value;
            payments = payments.Where(payment => payment.TenantId == tenantId);
        }

        payments = payments.Where(payment => payment.CreatedAt >= fromUtc && payment.CreatedAt <= toUtc);

        var totalsProjection = await payments
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Created = group.Count(),
                Assessed = group.Count(payment => payment.FraudDecision != null),
                Allowed = group.Count(payment => payment.FraudDecision == FraudDecision.Allow),
                ReviewHeld = group.Count(payment => payment.FraudDecision == FraudDecision.Review),
                Blocked = group.Count(payment => payment.FraudDecision == FraudDecision.Block),
                Succeeded = group.Count(payment => payment.Status == PaymentStatus.Succeeded),
                Disputed = group.Count(payment => payment.Status == PaymentStatus.Disputed),
                ReviewsApproved = group.Count(payment => payment.FraudReviewStatus == FraudReviewStatus.Approved),
                ReviewsDeclined = group.Count(payment => payment.FraudReviewStatus == FraudReviewStatus.Declined),
                ReviewSecondsTotal = group
                    .Where(payment => payment.FraudReviewedAt != null)
                    .Sum(payment => (double?)(payment.FraudReviewedAt!.Value - payment.CreatedAt).TotalSeconds)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? new { Created = 0, Assessed = 0, Allowed = 0, ReviewHeld = 0, Blocked = 0, Succeeded = 0, Disputed = 0, ReviewsApproved = 0, ReviewsDeclined = 0, ReviewSecondsTotal = (double?)0 };

        // The live queue backlog is deliberately window-independent: it answers "how much
        // review work is waiting right now".
        var backlogQuery = context.Set<Payment>().AsNoTracking()
            .Where(payment => payment.FraudReviewStatus == FraudReviewStatus.PendingReview);
        if (request.TenantId.HasValue)
        {
            var tenantId = request.TenantId.Value;
            backlogQuery = backlogQuery.Where(payment => payment.TenantId == tenantId);
        }

        var pendingBacklog = await backlogQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var resolvedInWindow = totalsProjection.ReviewsApproved + totalsProjection.ReviewsDeclined;

        var evidenceRows = await payments
            .Where(payment => payment.FraudDecision != null && payment.FraudEvidence != null)
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => payment.FraudEvidence!)
            .Take(MaxEvidenceRows)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var signalCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var evidence in evidenceRows)
        {
            foreach (var signalType in ExtractSignalTypes(evidence))
            {
                signalCounts[signalType] = signalCounts.TryGetValue(signalType, out var current)
                    ? current + 1
                    : 1;
            }
        }

        var topSignals = signalCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(TopSignalCount)
            .Select(pair => new FraudStatisticsSignal { SignalType = pair.Key, Count = pair.Value })
            .ToList();

        var resolvedReviewCount = totalsProjection.ReviewsApproved + totalsProjection.ReviewsDeclined;

        // Fraud by billing country (issue #409: fraud by country).
        var byCountry = await payments
            .GroupBy(payment => payment.BillingCountry ?? "unknown")
            .Select(group => new FraudStatisticsCountryRow
            {
                Country = group.Key,
                Created = group.Count(),
                ReviewHeld = group.Count(payment => payment.FraudDecision == FraudDecision.Review),
                Blocked = group.Count(payment => payment.FraudDecision == FraudDecision.Block)
            })
            .OrderByDescending(row => row.Blocked)
            .ThenByDescending(row => row.ReviewHeld)
            .ThenBy(row => row.Country)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Fraud by payment method (issue #409): the gateway provider is the payment-method
        // breakdown available on every attempt.
        var byPaymentMethod = await payments
            .GroupBy(payment => payment.Provider)
            .Select(group => new FraudStatisticsMethodRow
            {
                Provider = group.Key,
                Created = group.Count(),
                ReviewHeld = group.Count(payment => payment.FraudDecision == FraudDecision.Review),
                Blocked = group.Count(payment => payment.FraudDecision == FraudDecision.Block)
            })
            .OrderByDescending(row => row.Blocked)
            .ThenByDescending(row => row.ReviewHeld)
            .ThenBy(row => row.Provider)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Trend (issue #409: daily, weekly and monthly granularity). The bucketing is
        // computed over a bounded projection so one code path serves every granularity —
        // EF cannot translate ISO-week math portably to SQL.
        var trendProjection = await payments
            .Select(payment => new { payment.CreatedAt, payment.FraudDecision })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var trend = trendProjection
            .GroupBy(row => BucketStart(row.CreatedAt, request.TrendGranularity))
            .Select(group => new FraudStatisticsTrendPoint
            {
                Day = group.Key,
                Created = group.Count(),
                ReviewHeld = group.Count(row => row.FraudDecision == FraudDecision.Review),
                Blocked = group.Count(row => row.FraudDecision == FraudDecision.Block)
            })
            .OrderBy(point => point.Day)
            .ToList();

        // Fraud by subscription plan (issue #409): payments carry the subscription id; the
        // bulk payment-context resolution maps subscriptions to their plans in one round
        // trip. Without the subscription service (unit hosts) the view degrades to the
        // unattributed bucket instead of failing the report.
        var byPlan = await BuildByPlanAsync(payments, cancellationToken).ConfigureAwait(false);

        return new FraudStatisticsReport
        {
            GeneratedAtUtc = now,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Totals = new FraudStatisticsTotals
            {
                PaymentsCreated = totalsProjection.Created,
                Assessed = totalsProjection.Assessed,
                Allowed = totalsProjection.Allowed,
                ReviewHeld = totalsProjection.ReviewHeld,
                Blocked = totalsProjection.Blocked,
                Succeeded = totalsProjection.Succeeded,
                Disputed = totalsProjection.Disputed
            },
            Rates = new FraudStatisticsRates
            {
                FlaggedPercent = Percent(totalsProjection.ReviewHeld + totalsProjection.Blocked, totalsProjection.Assessed),
                BlockedPercent = Percent(totalsProjection.Blocked, totalsProjection.Assessed),
                ChargebackPercent = Percent(totalsProjection.Disputed, totalsProjection.Succeeded),
                FalsePositivePercent = Percent(totalsProjection.ReviewsApproved, resolvedReviewCount)
            },
            Reviews = new FraudStatisticsReviews
            {
                PendingBacklog = pendingBacklog,
                ResolvedInWindow = resolvedInWindow,
                ApprovedInWindow = totalsProjection.ReviewsApproved,
                DeclinedInWindow = totalsProjection.ReviewsDeclined,
                AverageReviewHours = resolvedInWindow > 0
                    ? totalsProjection.ReviewSecondsTotal / resolvedInWindow / 3600.0
                    : null
            },
            TopSignals = topSignals,
            ByCountry = byCountry,
            ByPaymentMethod = byPaymentMethod,
            ByPlan = byPlan,
            TrendGranularity = request.TrendGranularity,
            Trend = trend
        };
    }

    /// <summary>
    ///     Builds the by-plan breakdown (issue #409): groups window payments by subscription,
    ///     resolves the distinct subscriptions to their plans through the bulk payment-context
    ///     service and re-aggregates by plan. Payments without a subscription (or beyond the
    ///     resolution bound) land in the unattributed bucket.
    /// </summary>
    private async Task<IReadOnlyList<FraudStatisticsPlanRow>> BuildByPlanAsync(
        IQueryable<Payment> payments,
        CancellationToken cancellationToken)
    {
        var subscriptionGroups = await payments
            .GroupBy(payment => payment.SubscriptionId)
            .Select(group => new
            {
                SubscriptionId = group.Key,
                Created = group.Count(),
                ReviewHeld = group.Count(payment => payment.FraudDecision == FraudDecision.Review),
                Blocked = group.Count(payment => payment.FraudDecision == FraudDecision.Block)
            })
            .OrderByDescending(group => group.Created)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var planKeys = new Dictionary<Guid, (Guid? PlanId, string PlanName)>();
        if (subscriptionContextService is not null)
        {
            var subscriptionIds = subscriptionGroups
                .Where(group => group.SubscriptionId.HasValue)
                .Take(MaxPlanSubscriptionResolutions)
                .Select(group => group.SubscriptionId!.Value)
                .ToList();

            if (subscriptionIds.Count > 0)
            {
                var contexts = await subscriptionContextService
                    .GetPaymentContextsAsync(subscriptionIds, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var paymentContext in contexts)
                {
                    planKeys[paymentContext.SubscriptionId] = (
                        paymentContext.PlanId,
                        string.IsNullOrWhiteSpace(paymentContext.PlanName) ? "unknown" : paymentContext.PlanName);
                }
            }
        }

        var planRows = subscriptionGroups
            .Select(group => (Group: group, Plan: ResolvePlanKey(group.SubscriptionId, planKeys)))
            .GroupBy(entry => entry.Plan)
            .Select(group => new FraudStatisticsPlanRow
            {
                PlanId = group.Key.PlanId,
                PlanName = group.Key.PlanName,
                Created = group.Sum(entry => entry.Group.Created),
                ReviewHeld = group.Sum(entry => entry.Group.ReviewHeld),
                Blocked = group.Sum(entry => entry.Group.Blocked)
            })
            .OrderByDescending(row => row.Blocked)
            .ThenByDescending(row => row.ReviewHeld)
            .ThenByDescending(row => row.Created)
            .ThenBy(row => row.PlanName, StringComparer.Ordinal)
            .ToList();

        return planRows;
    }

    private static (Guid? PlanId, string PlanName) ResolvePlanKey(
        Guid? subscriptionId,
        IReadOnlyDictionary<Guid, (Guid? PlanId, string PlanName)> planKeys) =>
        subscriptionId.HasValue && planKeys.TryGetValue(subscriptionId.Value, out var plan)
            ? plan
            : (PlanId: null, PlanName: "unknown");

    /// <summary>
    ///     Start of the trend bucket containing the instant: the day itself, the ISO week
    ///     (Monday 00:00) or the calendar month, per the requested granularity. ISO-8601
    ///     week math without calendar-culture variance.
    /// </summary>
    internal static DateTime BucketStart(DateTime instantUtc, FraudTrendGranularity granularity)
    {
        var date = instantUtc.Date;
        return granularity switch
        {
            FraudTrendGranularity.Weekly => date.AddDays(-IsoDayIndex(date)),
            FraudTrendGranularity.Monthly => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => date
        };
    }

    /// <summary>Monday=0 .. Sunday=6 (ISO-8601 day index).</summary>
    private static int IsoDayIndex(DateTime date) =>
        date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;

    private static double? Percent(int numerator, int denominator) =>
        denominator > 0 ? Math.Round(numerator * 100.0 / denominator, 2) : null;

    private static IEnumerable<string> ExtractSignalTypes(string evidenceJson)
    {
        try
        {
            using var document = JsonDocument.Parse(evidenceJson);
            if (!document.RootElement.TryGetProperty("signals", out var signals))
            {
                return [];
            }

            if (signals.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            List<string> signalTypes = [];
            foreach (var signal in signals.EnumerateArray())
            {
                if (signal.ValueKind == JsonValueKind.Object
                    && signal.TryGetProperty("type", out var type)
                    && type.ValueKind == JsonValueKind.String)
                {
                    var value = type.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        signalTypes.Add(value);
                    }
                }
            }

            return signalTypes;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
