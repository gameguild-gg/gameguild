using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     High-risk-country fraud signal provider (issue #409: flag transactions from countries
///     with elevated fraud rates). Compares the attempt's ISO 3166-1 alpha-2 billing country
///     against the configured high-risk list. The list is empty by default: the capability is
///     opt-in per deployment so no geography is silently blocked.
/// </summary>
public sealed class HighRiskCountryFraudSignalProvider(
    ILogger<HighRiskCountryFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string SignalType = "Geography.HighRiskCountry";

    /// <inheritdoc />
    public string Name => "HighRiskCountry";

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

        var country = signalContext.Country?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(country) || options.HighRiskCountries.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        var matched = options.HighRiskCountries
            .Select(candidate => candidate?.Trim().ToUpperInvariant())
            .FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate) && string.Equals(candidate, country, StringComparison.Ordinal));

        if (matched is null)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        signals.Add(new FraudSignal(
            SignalType,
            Math.Min(options.HighRiskCountryWeight, options.MaxSignalScore),
            JsonSerializer.Serialize(new
            {
                billingCountry = matched,
                amount = signalContext.Amount,
                currency = signalContext.Currency
            })));

        logger.LogDebug(
            "Fraud high-risk-country evaluation for tenant {TenantId} payment {PaymentId}: billing country {Country} is on the high-risk list",
            signalContext.TenantId,
            signalContext.PaymentId,
            matched);

        return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
    }
}
