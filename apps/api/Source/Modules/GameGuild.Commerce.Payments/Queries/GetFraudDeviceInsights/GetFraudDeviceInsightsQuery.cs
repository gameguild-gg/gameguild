using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query for the cross-account device insight listing (issue #409: same device across
///     multiple accounts — signal plus listing surface). Requires the
///     <c>payments:admin</c> permission. Aggregates the tenant's payment attempts by the
///     persisted device fingerprint and returns the fingerprints shared by multiple
///     customer accounts, with the per-account breakdown reviewers use to judge whether
///     the sharing is a household or a fraud ring.
/// </summary>
/// <param name="TenantId">Optional tenant scope; enforced at the API boundary from the actor context.</param>
/// <param name="MinAccounts">Minimum distinct accounts for a device to be listed; defaults to the assessment option (2).</param>
/// <param name="LookbackDays">Look-back window in days; defaults to the assessment option (30).</param>
/// <param name="Page">Page number for pagination (1-based).</param>
/// <param name="PageSize">Page size (1-100).</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record GetFraudDeviceInsightsQuery(
    Guid? TenantId = null,
    int? MinAccounts = null,
    int? LookbackDays = null,
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<FraudDeviceInsight>>;

/// <summary>One device fingerprint seen across multiple customer accounts (issue #409).</summary>
public sealed record FraudDeviceInsight
{
    /// <summary>The shared device fingerprint (opaque client identifier, not PII).</summary>
    public required string DeviceFingerprint { get; init; }

    /// <summary>Distinct customer accounts that presented this device in the window.</summary>
    public required int AccountCount { get; init; }

    /// <summary>Payment attempts that presented this device in the window.</summary>
    public required int AttemptCount { get; init; }

    /// <summary>Earliest attempt carrying this device in the window.</summary>
    public required DateTime FirstSeenAt { get; init; }

    /// <summary>Latest attempt carrying this device in the window.</summary>
    public required DateTime LastSeenAt { get; init; }

    /// <summary>Per-account breakdown: account key, its attempts and latest activity.</summary>
    public required IReadOnlyList<FraudDeviceAccountUsage> Accounts { get; init; }
}

/// <summary>Usage of one shared device by one customer account (issue #409).</summary>
/// <param name="AccountKey">External customer id, or the subscription id for unbound attempts.</param>
/// <param name="AttemptCount">Payment attempts this account made with the device.</param>
/// <param name="LastSeenAt">Latest of those attempts.</param>
public sealed record FraudDeviceAccountUsage(
    string AccountKey,
    int AttemptCount,
    DateTime LastSeenAt);

/// <summary>
///     Handler for <see cref="GetFraudDeviceInsightsQuery"/>. Loads a bounded projection
///     of the tenant's fingerprinted attempts over the look-back window (backed by
///     <c>ix_payments_fraud_device_lookup</c>) and aggregates the cross-account groups
///     client-side: the per-tenant, per-window volume is review-queue scale, while the
///     account-key coalescing (external customer id, else subscription) stays plain CLR.
/// </summary>
public sealed class GetFraudDeviceInsightsQueryHandler(
    IApplicationDbContext context,
    Microsoft.Extensions.Options.IOptionsMonitor<FraudAssessmentOptions> optionsMonitor)
    : IQueryHandler<GetFraudDeviceInsightsQuery, IReadOnlyList<FraudDeviceInsight>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FraudDeviceInsight>> Handle(
        GetFraudDeviceInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        var minAccounts = Math.Max(2, request.MinAccounts ?? options.CrossAccountDeviceMinAccounts);
        var lookbackDays = Math.Clamp(request.LookbackDays ?? options.CrossAccountDeviceLookbackDays, 1, 365);
        var windowStart = SystemClock.UtcNow - TimeSpan.FromDays(lookbackDays);

        var query = context.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.FraudDeviceFingerprint != null
                              && payment.CreatedAt >= windowStart
                              && payment.Status != PaymentStatus.Cancelled);

        if (request.TenantId.HasValue)
        {
            var tenantId = request.TenantId.Value;
            query = query.Where(payment => payment.TenantId == tenantId);
        }

        var attempts = await query
            .Select(payment => new
            {
                payment.FraudDeviceFingerprint,
                AccountKey = payment.ExternalCustomerId ?? (payment.SubscriptionId ?? payment.Id).ToString(),
                payment.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return attempts
            .GroupBy(attempt => attempt.FraudDeviceFingerprint!)
            .Select(deviceGroup => new FraudDeviceInsight
            {
                DeviceFingerprint = deviceGroup.Key,
                AccountCount = deviceGroup.Select(attempt => attempt.AccountKey).Distinct().Count(),
                AttemptCount = deviceGroup.Count(),
                FirstSeenAt = deviceGroup.Min(attempt => attempt.CreatedAt),
                LastSeenAt = deviceGroup.Max(attempt => attempt.CreatedAt),
                Accounts = deviceGroup
                    .GroupBy(attempt => attempt.AccountKey)
                   .Select(accountGroup => new FraudDeviceAccountUsage(
                        accountGroup.Key,
                        accountGroup.Count(),
                        accountGroup.Max(attempt => attempt.CreatedAt)))
                    .OrderByDescending(account => account.LastSeenAt)
                    .ToList()
            })
            .Where(insight => insight.AccountCount >= minAccounts)
            .OrderByDescending(insight => insight.AccountCount)
            .ThenByDescending(insight => insight.LastSeenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
