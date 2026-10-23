using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Fraud signal provider that evaluates the payment attempt's listable attributes — the
///     gateway payment-method token, the card fingerprint and the provider customer id —
///     against the tenant blacklist (issue #409). A manual whitelist override suppresses the
///     signal for that value; matched entries contribute the configurable blacklist weight.
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
    }
}
