using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query for one fraud review case's full detail (issue #409: manual review interface
///     enrichment — user history, payment details, device fingerprint and session data).
///     Requires the <c>payments:admin</c> permission. Extends the review-queue item with
///     the reviewer context: the customer's prior transaction count and volume with the
///     account-tenure proxy (days since the customer's earliest recorded payment), the
///     payment's method/IP/location, and the device/session signals the gate assessed.
/// </summary>
/// <param name="PaymentId">The held payment whose review detail is requested.</param>
/// <param name="TenantId">Optional tenant scope; enforced at the API boundary from the actor context.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record GetFraudReviewDetailQuery(
    Guid PaymentId,
    Guid? TenantId = null) : IQuery<FraudReviewDetail?>;

/// <summary>Full manual-review read model for one held payment (issue #409).</summary>
public sealed record FraudReviewDetail
{
    /// <summary>The review-queue item this detail extends.</summary>
    public required FraudReviewListItem Case { get; init; }

    /// <summary>Customer's prior payment attempts (all time, same account scope).</summary>
    public required FraudReviewCustomerHistory History { get; init; }

    /// <summary>Payment details as the gate saw them.</summary>
    public required FraudReviewPaymentContext PaymentContext { get; init; }

    /// <summary>Device and session data captured with the attempt.</summary>
    public required FraudReviewDeviceContext DeviceContext { get; init; }
}

/// <summary>
///     Customer history for a review case (issue #409: show user history — previous
///     transactions, account age). Account age is proxied by the customer's earliest
///     recorded payment in the tenant: the payments module has no identity-module access,
///     and for fraud purposes the charging history IS the account's relevant tenure.
/// </summary>
public sealed record FraudReviewCustomerHistory
{
    /// <summary>Prior payment attempts by this customer (excluding the held one).</summary>
    public required int PriorAttemptCount { get; init; }

    /// <summary>Count of the customer's prior attempts that succeeded.</summary>
    public required int PriorSucceededCount { get; init; }

    /// <summary>Cumulative volume of the customer's prior non-cancelled attempts.</summary>
    public required decimal PriorVolume { get; init; }

    /// <summary>Days between the customer's earliest recorded payment and the held attempt; null when this is the first.</summary>
    public required int? AccountAgeDays { get; init; }
}

/// <summary>Payment details of the held attempt (issue #409: amount, method, IP, location).</summary>
public sealed record FraudReviewPaymentContext
{
    /// <summary>Payment-method token presented for the attempt.</summary>
    public required string? PaymentMethodId { get; init; }

    /// <summary>Gateway provider the attempt was routed to.</summary>
    public required string Provider { get; init; }

    /// <summary>Client IP the attempt originated from, when captured.</summary>
    public required string? SourceIpAddress { get; init; }

    /// <summary>ISO 3166-1 alpha-2 billing country of the attempt, when captured.</summary>
    public required string? BillingCountry { get; init; }

    /// <summary>External (provider) customer reference, when bound.</summary>
    public required string? ExternalCustomerId { get; init; }
}

/// <summary>Device fingerprint and session data of the held attempt (issue #409).</summary>
public sealed record FraudReviewDeviceContext
{
    /// <summary>Device fingerprint presented with the attempt, when captured.</summary>
    public required string? DeviceFingerprint { get; init; }

    /// <summary>Client-claimed IANA timezone, recovered from the assessed signal evidence.</summary>
    public required string? ClientTimezone { get; init; }

    /// <summary>GeoIP-resolved timezone of the source IP, recovered from the assessed signal evidence.</summary>
    public required string? ResolvedTimezone { get; init; }

    /// <summary>Whether the source IP resolved to a known anonymizer (VPN/proxy/Tor/hosting).</summary>
    public required bool? AnonymizerDetected { get; init; }
}

/// <summary>
///     Handler for <see cref="GetFraudReviewDetailQuery"/>. Loads the held payment, the
///     customer's payment-history aggregates (same account scoping as the velocity
///     signal), and recovers the device/session facts from the persisted attempt context
///     and the bounded signal evidence bundle. Returns null when the payment does not
///     exist, is outside the caller's tenant scope, or was never held for review.
/// </summary>
public sealed class GetFraudReviewDetailQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetFraudReviewDetailQuery, FraudReviewDetail?>
{
    /// <inheritdoc />
    public async Task<FraudReviewDetail?> Handle(GetFraudReviewDetailQuery request, CancellationToken cancellationToken)
    {
        var payment = await context.Set<Payment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            .ConfigureAwait(false);

        if (payment is null
            || (request.TenantId.HasValue && payment.TenantId != request.TenantId.Value)
            || payment.FraudDecision is null)
        {
            return null;
        }

        var accountKey = payment.ExternalCustomerId ?? payment.SubscriptionId?.ToString();
        var priorQuery = context.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.TenantId == payment.TenantId
                        && p.Id != payment.Id
                        && p.Status != PaymentStatus.Cancelled
                        && (p.ExternalCustomerId == payment.ExternalCustomerId
                            || (payment.SubscriptionId != null && p.SubscriptionId == payment.SubscriptionId)));

        var priorStats = await priorQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                AttemptCount = group.Count(),
                SucceededCount = group.Count(p => p.Status == PaymentStatus.Succeeded),
                Volume = group.Sum(p => p.Amount),
                FirstSeenAt = group.Min(p => p.CreatedAt)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var accountAgeDays = priorStats is null
            ? (int?)null
            : Math.Max(0, (int)(payment.CreatedAt - priorStats.FirstSeenAt).TotalDays);

        var (clientTimezone, resolvedTimezone, anonymizer) = ExtractSessionFacts(payment.FraudEvidence);

        return new FraudReviewDetail
        {
            Case = new FraudReviewListItem
            {
                TenantId = payment.TenantId,
                PaymentId = payment.Id,
                SubscriptionId = payment.SubscriptionId,
                ExternalCustomerId = payment.ExternalCustomerId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Score = payment.FraudScore ?? 0,
                RiskLevel = FraudAssessment.RiskLevelForScore(payment.FraudScore ?? 0),
                Reason = payment.FraudDecisionReason ?? string.Empty,
                Evidence = payment.FraudEvidence,
                CreatedAt = payment.CreatedAt
            },
            History = new FraudReviewCustomerHistory
            {
                PriorAttemptCount = priorStats?.AttemptCount ?? 0,
                PriorSucceededCount = priorStats?.SucceededCount ?? 0,
                PriorVolume = priorStats?.Volume ?? 0m,
                AccountAgeDays = accountAgeDays
            },
            PaymentContext = new FraudReviewPaymentContext
            {
                PaymentMethodId = payment.PaymentMethodId,
                Provider = payment.Provider,
                SourceIpAddress = payment.FraudSourceIpAddress,
                BillingCountry = payment.BillingCountry,
                ExternalCustomerId = payment.ExternalCustomerId
            },
            DeviceContext = new FraudReviewDeviceContext
            {
                DeviceFingerprint = payment.FraudDeviceFingerprint,
                ClientTimezone = clientTimezone,
                ResolvedTimezone = resolvedTimezone,
                AnonymizerDetected = anonymizer
            }
        };
    }

    /// <summary>
    ///     Recovers the session facts (claimed/resolved timezone, anonymizer detection)
    ///     from the persisted signal evidence bundle. Defensive by design: a missing,
    ///     truncated or malformed bundle yields nulls, never a failed detail read.
    /// </summary>
    private static (string? ClientTimezone, string? ResolvedTimezone, bool? Anonymizer) ExtractSessionFacts(string? evidenceJson)
    {
        if (string.IsNullOrWhiteSpace(evidenceJson))
        {
            return (null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(evidenceJson);
            if (!document.RootElement.TryGetProperty("signals", out var signals)
                || signals.ValueKind != JsonValueKind.Array)
            {
                return (null, null, null);
            }

            string? clientTimezone = null;
            string? resolvedTimezone = null;
            bool? anonymizer = null;

            foreach (var signal in signals.EnumerateArray())
            {
                if (!signal.TryGetProperty("type", out var typeElement)
                    || typeElement.Stringify() is not { } signalType)
                {
                    continue;
                }

                var details = signal.TryGetProperty("details", out var detailsElement)
                    && detailsElement.ValueKind == JsonValueKind.String
                    ? detailsElement.GetString()
                    : null;

                if (details is null)
                {
                    continue;
                }

                if (signalType.StartsWith("GeoIp.TimezoneMismatch", StringComparison.Ordinal))
                {
                    using var detailsDocument = JsonDocument.Parse(details);
                    if (detailsDocument.RootElement.TryGetProperty("claimedTimezone", out var claimed))
                    {
                        clientTimezone = claimed.GetString();
                    }

                    if (detailsDocument.RootElement.TryGetProperty("resolvedTimezone", out var resolved))
                    {
                        resolvedTimezone = resolved.GetString();
                    }
                }
                else if (signalType.StartsWith("GeoIp.Anonymizer", StringComparison.Ordinal))
                {
                    // The anonymizer signal only fires when at least one anonymizer trait
                    // (VPN, public proxy, Tor exit, hosting provider) resolved to true.
                    anonymizer = true;
                }
            }

            return (clientTimezone, resolvedTimezone, anonymizer);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }
}

/// <summary>Small JSON helper: reads a string element regardless of value kind.</summary>
file static class JsonElementStringExtensions
{
    public static string? Stringify(this JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
}
