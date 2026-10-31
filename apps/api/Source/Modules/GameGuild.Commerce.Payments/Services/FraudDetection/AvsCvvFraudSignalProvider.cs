using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     AVS/CVV verification-outcome fraud signal provider (issue #409: AVS/CVV mismatch).
///     Consumes the SERVER-KNOWN verification results threaded onto the attempt context —
///     gateway-reported outcomes from verified flows, never client self-reporting. A
///     mismatch contributes the strong weight; an explicit "not checked" contributes the
///     weak weight; absent outcomes (null) contribute nothing, so flows without
///     verification data stay unaffected.
/// </summary>
public sealed class AvsCvvFraudSignalProvider(
    ILogger<AvsCvvFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string AvsSignalType = "Avs.Outcome";
    private const string CvvSignalType = "Cvv.Outcome";

    /// <inheritdoc />
    public string Name => "AvsCvv";

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

        AddOutcomeSignal(signals, AvsSignalType, "AVS", signalContext.AvsOutcome, signalContext, options);
        AddOutcomeSignal(signals, CvvSignalType, "CVV", signalContext.CvvOutcome, signalContext, options);

        return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
    }

    private static void AddOutcomeSignal(
        List<FraudSignal> signals,
        string signalType,
        string checkName,
        FraudVerificationOutcome? outcome,
        FraudSignalContext signalContext,
        FraudAssessmentOptions options)
    {
        if (outcome is null)
        {
            return;
        }

        var (weight, reason) = outcome.Value switch
        {
            FraudVerificationOutcome.Mismatch => (options.AvsCvvMismatchWeight, "mismatch"),
            FraudVerificationOutcome.NotChecked => (options.AvsCvvUncheckedWeight, "not_checked"),
            _ => (0, "match")
        };

        if (weight <= 0)
        {
            return;
        }

        signals.Add(new FraudSignal(
            signalType,
            Math.Min(weight, options.MaxSignalScore),
            JsonSerializer.Serialize(new
            {
                check = checkName,
                outcome = outcome.Value.ToString(),
                reason = reason,
                paymentMethodId = signalContext.PaymentMethodId is null
                    ? null
                    : $"{signalContext.PaymentMethodId[..Math.Min(4, signalContext.PaymentMethodId.Length)]}…"
            })));
    }
}
