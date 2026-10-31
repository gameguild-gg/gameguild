using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Fraud signal provider that evaluates the payment attempt's listable attributes — the
///     gateway payment-method token, the card fingerprint, the provider customer id, the
///     source IP address, the payer email (exact address and its domain) and the device
///     fingerprint — against the tenant blacklist (issue #409). A manual whitelist override
///     suppresses the signal for that value; matched entries contribute the configurable
///     blacklist weight.
/// </summary>
/// <remarks>
///     Signal details are bounded evidence: they name the matched attribute kind and the
///     bounded reason recorded with the entry, never the raw gateway token.
/// </remarks>
public sealed class BlacklistFraudSignalProvider(
    IApplicationDbContext context,
    IFraudBlacklistService blacklistService,
    ILogger<BlacklistFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string SignalTypePrefix = "Blacklist";

    /// <inheritdoc />
    public string Name => "Blacklist";

    /// <inheritdoc />
    public async Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
        FraudSignalContext signalContext,
        FraudAssessmentOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalContext);
        ArgumentNullException.ThrowIfNull(options);

        var signals = new List<FraudSignal>();

        // FraudSignalContext.PaymentMethodId carries the gateway payment-method token.
        string? cardFingerprint = null;
        if (signalContext.PaymentMethodId is { } paymentMethodToken)
        {
            cardFingerprint = await context.Set<PaymentMethod>()
                .AsNoTracking()
                .Where(candidate => candidate.TenantId == signalContext.TenantId
                                    && candidate.GatewayPaymentMethodId == paymentMethodToken)
                .Select(candidate => candidate.CardFingerprint)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var weight = Math.Min(options.BlacklistWeight, options.MaxSignalScore);

        await AddSignalForValueAsync(signals, signalContext, FraudBlacklistEntryType.PaymentMethodToken, signalContext.PaymentMethodId, weight, cancellationToken).ConfigureAwait(false);
        await AddSignalForValueAsync(signals, signalContext, FraudBlacklistEntryType.CardFingerprint, cardFingerprint, weight, cancellationToken).ConfigureAwait(false);
        await AddSignalForValueAsync(signals, signalContext, FraudBlacklistEntryType.ExternalCustomerId, signalContext.ExternalCustomerId, weight, cancellationToken).ConfigureAwait(false);

        // IP blacklist (issue #409: block known malicious IP ranges — CIDR-style wildcards
        // are matched by the blacklist pattern evaluator, e.g. 203.0.113.*).
        await AddSignalForValueAsync(signals, signalContext, FraudBlacklistEntryType.IpAddress, signalContext.SourceIpAddress, weight, cancellationToken).ConfigureAwait(false);

        // Device-fingerprint blacklist (issue #409).
        await AddSignalForValueAsync(signals, signalContext, FraudBlacklistEntryType.DeviceFingerprint, signalContext.DeviceFingerprint, weight, cancellationToken).ConfigureAwait(false);

        // Payer email blacklist (issue #409): exact address first, then the domain, so an
        // entry for either shape fires exactly once (the domain check only runs when the
        // exact-address check did not already match).
        if (signalContext.PayerEmail is { } payerEmail
            && EmailDomainOf(payerEmail) is { } payerDomain)
        {
            var exactMatch = await blacklistService
                .EvaluateAsync(signalContext.TenantId, FraudBlacklistEntryType.Email, payerEmail, cancellationToken)
                .ConfigureAwait(false);
            if (exactMatch.IsBlacklisted)
            {
                AddSignalForMatch(signals, FraudBlacklistEntryType.Email, weight, exactMatch);
            }
            else
            {
                await AddSignalForValueAsync(signals, signalContext, FraudBlacklistEntryType.EmailDomain, payerDomain, weight, cancellationToken).ConfigureAwait(false);
            }
        }

        if (signals.Count > 0)
        {
            logger.LogDebug(
                "Fraud blacklist evaluation for tenant {TenantId} payment {PaymentId}: {SignalCount} blacklist hit(s)",
                signalContext.TenantId,
                signalContext.PaymentId,
                signals.Count);
        }

        return signals;
    }

    /// <summary>Extracts the lowercased domain of an email address, or null when it has none.</summary>
    internal static string? EmailDomainOf(string email)
    {
        var separatorIndex = email.LastIndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == email.Length - 1)
        {
            return null;
        }

        var domain = email[(separatorIndex + 1)..].Trim().ToLowerInvariant();
        return domain.Length == 0 ? null : domain;
    }

    private static void AddSignalForMatch(
        List<FraudSignal> signals,
        FraudBlacklistEntryType entryType,
        int weight,
        FraudBlacklistMatch match) =>
        signals.Add(new FraudSignal(
            $"{SignalTypePrefix}.{entryType}",
            weight,
            JsonSerializer.Serialize(new
            {
                entryType = entryType.ToString(),
                matchedPattern = match.MatchedPattern,
                reason = match.Reason,
                source = match.Source?.ToString()
            })));

    private async Task AddSignalForValueAsync(
        List<FraudSignal> signals,
        FraudSignalContext signalContext,
        FraudBlacklistEntryType entryType,
        string? value,
        int weight,
        CancellationToken cancellationToken)
    {
        if (value is null)
        {
            return;
        }

        var match = await blacklistService
            .EvaluateAsync(signalContext.TenantId, entryType, value, cancellationToken)
            .ConfigureAwait(false);

        if (!match.IsBlacklisted)
        {
            return;
        }

        AddSignalForMatch(signals, entryType, weight, match);
    }
}
