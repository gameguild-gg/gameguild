using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Billing webhook delivery and processing statistics over a date window
///     (issue #408: webhook processing success rate, average processing time, failed
///     webhook count and the dashboard activity view). Served from the durable webhook
///     inbox; the window defaults to the last 24 hours.
/// </summary>
/// <param name="FromUtc">Inclusive window start; defaults to 24 hours ago.</param>
/// <param name="ToUtc">Inclusive window end; defaults to now.</param>
public sealed record GetBillingWebhookStatsQuery(DateTime? FromUtc = null, DateTime? ToUtc = null)
    : IQuery<BillingWebhookStatsDto>;

/// <summary>Read model of one webhook-statistics reporting pass.</summary>
public sealed record BillingWebhookStatsDto
{
    /// <summary>UTC instant the report was generated.</summary>
    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>Inclusive window start used for the aggregates.</summary>
    public required DateTime FromUtc { get; init; }

    /// <summary>Inclusive window end used for the aggregates.</summary>
    public required DateTime ToUtc { get; init; }

    /// <summary>Events received into the inbox in the window.</summary>
    public required int ReceivedInWindow { get; init; }

    /// <summary>Events processed successfully in the window.</summary>
    public required int ProcessedInWindow { get; init; }

    /// <summary>Events currently failed and awaiting retry (not dead-lettered).</summary>
    public required int FailedPendingRetry { get; init; }

    /// <summary>Events dead-lettered in the window after exhausting the retry budget.</summary>
    public required int DeadLetteredInWindow { get; init; }

    /// <summary>Events received in the window whose provider signature did not verify.</summary>
    public required int UnverifiedInWindow { get; init; }

    /// <summary>Processing success rate: processed / received, 0-100; null when nothing was received.</summary>
    public double? SuccessRatePercent { get; init; }

    /// <summary>
    ///     Average processing latency (received -> processed) in seconds over the events
    ///     processed in the window; null when none.
    /// </summary>
    public double? AverageProcessingSeconds { get; init; }

    /// <summary>Per-provider breakdown of the window aggregates.</summary>
    public required IReadOnlyList<BillingWebhookProviderStatsDto> PerProvider { get; init; }
}

/// <summary>Per-provider webhook activity inside the statistics window.</summary>
public sealed record BillingWebhookProviderStatsDto
{
    /// <summary>Provider that sent the events (stripe, paypal, ...).</summary>
    public required string Provider { get; init; }

    /// <summary>Events received from the provider in the window.</summary>
    public required int Received { get; init; }

    /// <summary>Events processed successfully in the window.</summary>
    public required int Processed { get; init; }

    /// <summary>Events currently failed and awaiting retry.</summary>
    public required int FailedPendingRetry { get; init; }

    /// <summary>Events dead-lettered in the window.</summary>
    public required int DeadLettered { get; init; }

    /// <summary>Events received in the window whose signature did not verify.</summary>
    public required int Unverified { get; init; }
}

/// <summary>Handler for <see cref="GetBillingWebhookStatsQuery"/>.</summary>
public sealed class GetBillingWebhookStatsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetBillingWebhookStatsQuery, BillingWebhookStatsDto>
{
    private const int MaxLatencyRows = 5000;

    /// <inheritdoc />
    public async Task<BillingWebhookStatsDto> Handle(GetBillingWebhookStatsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = SystemClock.UtcNow;
        var toUtc = request.ToUtc ?? now;
        var fromUtc = request.FromUtc ?? toUtc.AddHours(-24);
        if (fromUtc > toUtc)
        {
            (fromUtc, toUtc) = (toUtc, fromUtc);
        }

        var eventsInWindow = context.Set<BillingWebhookEvent>()
            .AsNoTracking()
            .Where(webhookEvent => webhookEvent.CreatedAt >= fromUtc && webhookEvent.CreatedAt <= toUtc);

        var providerAggregates = await eventsInWindow
            .GroupBy(webhookEvent => webhookEvent.Provider)
            .Select(group => new
            {
                Provider = group.Key,
                Received = group.Count(),
                Processed = group.Count(webhookEvent => webhookEvent.IsProcessed),
                DeadLettered = group.Count(webhookEvent => webhookEvent.DeadLetteredAt != null),
                Unverified = group.Count(webhookEvent => webhookEvent.IsSignatureVerified == false)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Latency (received -> processed) is averaged in memory over a bounded sample:
        // the row projection is minimal and the dashboard tolerates a bounded sample on
        // very high-traffic windows.
        var latencyRows = await context.Set<BillingWebhookEvent>()
            .AsNoTracking()
            .Where(webhookEvent => webhookEvent.IsProcessed
                                    && webhookEvent.ProcessedAt != null
                                    && webhookEvent.CreatedAt >= fromUtc
                                    && webhookEvent.CreatedAt <= toUtc)
            .OrderByDescending(webhookEvent => webhookEvent.ProcessedAt)
            .Select(webhookEvent => new { webhookEvent.CreatedAt, ProcessedAt = webhookEvent.ProcessedAt!.Value })
            .Take(MaxLatencyRows)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var failedByProvider = await context.Set<BillingWebhookEvent>()
            .AsNoTracking()
            .Where(webhookEvent => webhookEvent.IsFailed
                                    && webhookEvent.DeadLetteredAt == null
                                    && !webhookEvent.IsProcessed)
            .GroupBy(webhookEvent => webhookEvent.Provider)
            .Select(group => new { Provider = group.Key, Failed = group.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var failedByProviderLookup = failedByProvider.ToDictionary(item => item.Provider, item => item.Failed);
        var failedPendingRetry = failedByProviderLookup.Values.Sum();

        var receivedTotal = providerAggregates.Sum(item => item.Received);
        var processedTotal = providerAggregates.Sum(item => item.Processed);
        var deadLetteredTotal = providerAggregates.Sum(item => item.DeadLettered);
        var unverifiedTotal = providerAggregates.Sum(item => item.Unverified);

        return new BillingWebhookStatsDto
        {
            GeneratedAtUtc = now,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            ReceivedInWindow = receivedTotal,
            ProcessedInWindow = processedTotal,
            FailedPendingRetry = failedPendingRetry,
            DeadLetteredInWindow = deadLetteredTotal,
            UnverifiedInWindow = unverifiedTotal,
            SuccessRatePercent = receivedTotal > 0
                ? Math.Round(processedTotal * 100.0 / receivedTotal, 2)
                : null,
            AverageProcessingSeconds = latencyRows.Count > 0
                ? Math.Round(
                    latencyRows.Average(row => (row.ProcessedAt - row.CreatedAt).TotalSeconds),
                    2)
                : null,
            PerProvider = providerAggregates
                .OrderByDescending(item => item.Received)
                .ThenBy(item => item.Provider, StringComparer.Ordinal)
                .Select(item => new BillingWebhookProviderStatsDto
                {
                    Provider = item.Provider,
                    Received = item.Received,
                    Processed = item.Processed,
                    FailedPendingRetry = failedByProviderLookup.TryGetValue(item.Provider, out var failed) ? failed : 0,
                    DeadLettered = item.DeadLettered,
                    Unverified = item.Unverified
                })
                .ToList()
        };
    }
}
