using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Disposable-email fraud signal provider (issue #409: flag disposable email providers).
///     Fires when the payer email presented with the payment attempt uses a domain on the
///     configured disposable-provider list (mailinator, guerrillamail, 10minutemail, ...).
///     The list is deployment-configurable; the defaults cover the widely abused providers.
/// </summary>
/// <remarks>
///     Signal evidence records only the matched domain, never the full payer address.
/// </remarks>
public sealed class DisposableEmailFraudSignalProvider(
    ILogger<DisposableEmailFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string SignalType = "Email.DisposableDomain";

    /// <inheritdoc />
    public string Name => "DisposableEmail";

    /// <inheritdoc />
    public Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
        FraudSignalContext signalContext,
        FraudAssessmentOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalContext);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        var signals = new List<FraudSignal>();

        if (signalContext.PayerEmail is null
            || BlacklistFraudSignalProvider.EmailDomainOf(signalContext.PayerEmail) is not { } domain)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        var disposableDomains = options.DisposableEmailDomains;
        if (disposableDomains.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        var matched = disposableDomains
            .Select(candidate => candidate?.Trim().ToLowerInvariant())
            .FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate) && string.Equals(candidate, domain, StringComparison.Ordinal));

        if (matched is null)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        signals.Add(new FraudSignal(
            SignalType,
            Math.Min(options.DisposableEmailWeight, options.MaxSignalScore),
            JsonSerializer.Serialize(new
            {
                domain = matched,
                amount = signalContext.Amount,
                currency = signalContext.Currency
            })));

        logger.LogDebug(
            "Fraud disposable-email evaluation for tenant {TenantId} payment {PaymentId}: payer email uses disposable domain {Domain}",
            signalContext.TenantId,
            signalContext.PaymentId,
            matched);

        return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
    }
}
