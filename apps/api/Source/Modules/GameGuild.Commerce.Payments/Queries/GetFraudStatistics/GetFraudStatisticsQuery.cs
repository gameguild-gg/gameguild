using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

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
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record GetFraudStatisticsQuery(
    Guid? TenantId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IQuery<FraudStatisticsReport>;

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
public sealed class GetFraudStatisticsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetFraudStatisticsQuery, FraudStatisticsReport>
{
    private const int MaxEvidenceRows = 5000;
    private const int TopSignalCount = 10;

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
            TopSignals = topSignals
        };
    }

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
