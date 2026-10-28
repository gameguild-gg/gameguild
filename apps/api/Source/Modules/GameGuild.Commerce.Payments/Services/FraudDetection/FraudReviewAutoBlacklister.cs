using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Best-effort automatic blacklisting of confirmed-fraud payment attempts (issue #409:
///     automatic addition on confirmed fraud). When a manual fraud review is declined, the
///     attempt's listable attributes — the gateway payment-method token, the card
///     fingerprint and the provider customer id — are added to the tenant blacklist with a
///     bounded expiry so future attempts with the same attributes are scored by the
///     blacklist signal provider. Failures are logged and swallowed: the review resolution
///     itself has already succeeded and is never rolled back.
/// </summary>
public sealed class FraudReviewAutoBlacklister(
    IFraudBlacklistService blacklistService,
    IApplicationDbContext context,
    IOptionsMonitor<FraudAssessmentOptions> optionsMonitor,
    ILogger<FraudReviewAutoBlacklister> logger)
{
    /// <summary>
    ///     Blacklists the confirmed-fraud attempt's attributes when the tenant's
    ///     <c>AutoBlacklistOnFraudDecline</c> option is enabled (default true).
    /// </summary>
    /// <param name="payment">Payment whose fraud review was declined.</param>
    /// <param name="reviewedByUserId">Administrator who declined the review.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of blacklist entries added.</returns>
    public async Task<int> RunAsync(Payment payment, Guid reviewedByUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);

        try
        {
            var options = optionsMonitor.CurrentValue.ResolveFor(payment.TenantId);
            if (!options.AutoBlacklistOnFraudDecline)
            {
                return 0;
            }

            var expiresAt = SystemClock.UtcNow.AddDays(options.AutoBlacklistExpiryDays);
            var reason = $"Auto: fraud review declined for payment {payment.Id}";
            var added = 0;

            // Payment.PaymentMethodId carries the gateway payment-method token; the card
            // fingerprint (when bound) is resolved through the payment-method registry.
            string? cardFingerprint = null;
            if (payment.PaymentMethodId is { } paymentMethodToken)
            {
                cardFingerprint = await context.Set<PaymentMethod>()
                    .AsNoTracking()
                    .Where(candidate => candidate.TenantId == payment.TenantId
                                        && candidate.GatewayPaymentMethodId == paymentMethodToken)
                    .Select(candidate => candidate.CardFingerprint)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            added += await AddAsync(payment.TenantId, FraudBlacklistEntryType.PaymentMethodToken, payment.PaymentMethodId, reason, expiresAt, reviewedByUserId, cancellationToken).ConfigureAwait(false);
            added += await AddAsync(payment.TenantId, FraudBlacklistEntryType.CardFingerprint, cardFingerprint, reason, expiresAt, reviewedByUserId, cancellationToken).ConfigureAwait(false);
            added += await AddAsync(payment.TenantId, FraudBlacklistEntryType.ExternalCustomerId, payment.ExternalCustomerId, reason, expiresAt, reviewedByUserId, cancellationToken).ConfigureAwait(false);

            if (added > 0)
            {
                logger.LogInformation(
                    "Automatically blacklisted {EntryCount} attribute(s) of confirmed-fraud payment {PaymentId} for tenant {TenantId}",
                    added,
                    payment.Id,
                    payment.TenantId);
            }

            return added;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Automatic blacklisting of confirmed-fraud payment {PaymentId} failed; the review resolution stands",
                payment.Id);
            return 0;
        }
    }

    private async Task<int> AddAsync(
        Guid tenantId,
        FraudBlacklistEntryType entryType,
        string? value,
        string reason,
        DateTime expiresAt,
        Guid reviewedByUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var entry = FraudBlacklistEntry.Create(
            tenantId,
            entryType,
            value,
            reason,
            isWhitelist: false,
            FraudBlacklistSource.AutoFraudDecline,
            expiresAt,
            reviewedByUserId);

        if (entry is null)
        {
            return 0;
        }

        await blacklistService.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return 1;
    }
}
