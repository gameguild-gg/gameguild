using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     High-risk card BIN range fraud signal provider (issue #409: BIN range signal). Fires
///     when the Bank Identification Number prefix of the presented card matches a configured
///     high-risk entry — a 6-8 digit prefix or an inclusive numeric range. The list is
///     deployment-configured (empty by default). The BIN is the PCI-permitted non-sensitive
///     card prefix; the evidence records the matched entry only.
/// </summary>
public sealed class BinRangeFraudSignalProvider(
    ILogger<BinRangeFraudSignalProvider> logger) : IFraudSignalProvider
{
    private const string SignalType = "CardBin.HighRiskRange";

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
        if (bin is null || options.HighRiskCardBins.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        var matched = MatchEntry(bin, options.HighRiskCardBins);
        if (matched is null)
        {
            return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
        }

        signals.Add(new FraudSignal(
            SignalType,
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

        return Task.FromResult<IReadOnlyList<FraudSignal>>(signals);
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
