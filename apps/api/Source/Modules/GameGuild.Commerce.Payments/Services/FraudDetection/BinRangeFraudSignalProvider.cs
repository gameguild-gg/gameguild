using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Card BIN fraud signal provider (issue #409: BIN range + BIN-country checks). Fires on
///     two independent conditions over the PCI-permitted 6-8 digit Bank Identification
///     Number prefix of the presented card:
///     <list type="bullet">
///         <item><c>CardBin.HighRiskRange</c> — the BIN matches a configured high-risk
///         prefix or inclusive numeric range.</item>
///         <item><c>CardBin.CountryMismatch</c> — the BIN's mapped issuer country differs
///         from the attempt's billing country (the "card country vs user country" check;
///         the issuer-country dataset is a deployment-configured map).</item>
///     </list>
///     Both lists are deployment-configured and empty by default. The evidence records the
///     matched entry and the country pair only.
/// </summary>
public sealed class BinRangeFraudSignalProvider(
    ILogger<BinRangeFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string HighRiskRangeSignalType = "CardBin.HighRiskRange";
    private const string CountryMismatchSignalType = "CardBin.CountryMismatch";

    /// <inheritdoc />
    public string Name => "BinRange";

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

        var bin = NormalizeBin(signalContext.CardBin);
        if (bin is null)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        EvaluateHighRiskRange(bin, signalContext, options, signals);
        EvaluateCountryMismatch(bin, signalContext, options, signals);

        return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
    }

    private void EvaluateHighRiskRange(
        string bin,
        FraudSignalContext signalContext,
        FraudAssessmentOptions options,
        List<FraudSignal> signals)
    {
        if (options.HighRiskCardBins.Length == 0)
        {
            return;
        }

        var matched = MatchEntry(bin, options.HighRiskCardBins);
        if (matched is null)
        {
            return;
        }

        signals.Add(new FraudSignal(
            HighRiskRangeSignalType,
            Math.Min(options.CardBinWeight, options.MaxSignalScore),
            JsonSerializer.Serialize(new
            {
                bin = bin,
                matchedEntry = matched
            })));

        logger.LogDebug(
            "Fraud BIN-range evaluation for tenant {TenantId} payment {PaymentId}: card BIN {Bin} matched high-risk entry {Entry}",
            signalContext.TenantId,
            signalContext.PaymentId,
            bin,
            matched);
    }

    private void EvaluateCountryMismatch(
        string bin,
        FraudSignalContext signalContext,
        FraudAssessmentOptions options,
        List<FraudSignal> signals)
    {
        if (options.CardBinCountryMap.Length == 0 || string.IsNullOrWhiteSpace(signalContext.Country))
        {
            return;
        }

        var issuerCountry = ResolveIssuerCountry(bin, options.CardBinCountryMap);
        var billingCountry = signalContext.Country.Trim().ToUpperInvariant();
        if (issuerCountry is null || string.Equals(issuerCountry, billingCountry, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        signals.Add(new FraudSignal(
            CountryMismatchSignalType,
            Math.Min(options.CardBinCountryMismatchWeight, options.MaxSignalScore),
            JsonSerializer.Serialize(new
            {
                bin = bin,
                cardCountry = issuerCountry,
                userCountry = billingCountry
            })));

        logger.LogDebug(
            "Fraud BIN-country evaluation for tenant {TenantId} payment {PaymentId}: card BIN {Bin} issued in {CardCountry} but billing country is {UserCountry}",
            signalContext.TenantId,
            signalContext.PaymentId,
            bin,
            issuerCountry,
            billingCountry);
    }

    /// <summary>
    ///     Resolves the issuer country of a normalized BIN from <c>prefix-or-range=CC</c>
    ///     entries. Malformed entries never match (fail-closed to no-signal).
    /// </summary>
    internal static string? ResolveIssuerCountry(string bin, IReadOnlyList<string?> entries)
    {
        foreach (var rawEntry in entries)
        {
            if (string.IsNullOrWhiteSpace(rawEntry))
            {
                continue;
            }

            var entry = rawEntry.Trim();
            var separator = entry.IndexOf('=');
            if (separator <= 0 || separator == entry.Length - 1)
            {
                continue;
            }

            var pattern = entry[..separator].Trim();
            var country = entry[(separator + 1)..].Trim();
            if (country.Length != 2 || !country.All(char.IsLetter))
            {
                continue;
            }

            if (MatchEntry(bin, [pattern]) is not null)
            {
                return country.ToUpperInvariant();
            }
        }

        return null;
    }

    /// <summary>Normalizes a candidate BIN to its 6-8 digit form; null when unusable.</summary>
    internal static string? NormalizeBin(string? cardBin)
    {
        if (string.IsNullOrWhiteSpace(cardBin))
        {
            return null;
        }

        var trimmed = cardBin.Trim();
        return trimmed.Length is >= 6 and <= 8 && trimmed.All(char.IsDigit)
            ? trimmed
            : null;
    }

    /// <summary>
    ///     Matches a normalized BIN against the configured entries. An entry is a prefix
    ///     (<c>123456</c>, matches any BIN starting with it) or an inclusive numeric range
    ///     (<c>400000-400050</c>, compared on the shared length). Malformed entries never
    ///     match (fail-closed to no-signal rather than blocking every card).
    /// </summary>
    internal static string? MatchEntry(string bin, IReadOnlyList<string?> entries)
    {
        foreach (var rawEntry in entries)
        {
            if (string.IsNullOrWhiteSpace(rawEntry))
            {
                continue;
            }

            var entry = rawEntry.Trim();
            var separator = entry.IndexOf('-');
            if (separator < 0)
            {
                if (entry.Length is >= 6 and <= 8
                    && entry.All(char.IsDigit)
                    && bin.StartsWith(entry, StringComparison.Ordinal))
                {
                    return entry;
                }

                continue;
            }

            var low = entry[..separator].Trim();
            var high = entry[(separator + 1)..].Trim();
            if (low.Length != high.Length
                || low.Length is < 6 or > 8
                || !low.All(char.IsDigit)
                || !high.All(char.IsDigit)
                || !long.TryParse(low, out var lowValue)
                || !long.TryParse(high, out var highValue)
                || lowValue > highValue)
            {
                continue;
            }

            // Compare on the entry's length so a 6-digit range matches the first 6 BIN digits.
            var candidate = bin.Length > low.Length ? bin[..low.Length] : bin;
            if (long.TryParse(candidate, out var binValue) && binValue >= lowValue && binValue <= highValue)
            {
                return entry;
            }
        }

        return null;
    }
}
