using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     GeoIP-based fraud signal provider (issue #409: VPN/proxy detection and
///     timezone-vs-IP inconsistency). Reuses the platform's <see cref="IGeolocationProvider"/>
///     (offline MaxMind adapter including the Anonymous-IP anonymizer traits):
/// </summary>
/// <list type="bullet">
///     <item>
///         <see cref="Signals.GeoIpAnonymizer"/> fires when the source IP resolves to a known
///         anonymizing network — VPN endpoint, public proxy, Tor exit node or hosting
///         provider — via the MaxMind anonymizer traits already exposed by the provider.
///     </item>
///     <item>
///         <see cref="Signals.GeoIpTimezoneMismatch"/> fires when the IANA timezone the
///         client claims differs from the timezone GeoIP resolves for the source IP.
///     </item>
/// </list>
/// <remarks>
///     Degrades silently: a null resolution (no database configured, private/loopback IP,
///     lookup miss) contributes no signals — the provider contract never throws for
///     unknown addresses. The claimed timezone is evidence, not fact: the signal weight
///     reflects that a spoofed header alone is weak.
/// </remarks>
public sealed class GeoIpFraudSignalProvider(
    IGeolocationProvider geolocationProvider,
    ILogger<GeoIpFraudSignalProvider> logger) : IFraudSignalProvider
{
    /// <summary>Stable signal identifiers emitted by this provider.</summary>
    public static class Signals
    {
        /// <summary>Source IP resolves to an anonymizing network (VPN/proxy/Tor/hosting).</summary>
        public const string GeoIpAnonymizer = "GeoIp.Anonymizer";

        /// <summary>Client-claimed timezone disagrees with the GeoIP timezone of the source IP.</summary>
        public const string GeoIpTimezoneMismatch = "GeoIp.TimezoneMismatch";
    }

    /// <inheritdoc />
    public string Name => "GeoIp";

    /// <inheritdoc />
    public async Task<IReadOnlyList<FraudSignal>> EvaluateAsync(
        FraudSignalContext signalContext,
        FraudAssessmentOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signalContext);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var signals = new List<FraudSignal>();

        if (string.IsNullOrWhiteSpace(signalContext.SourceIpAddress))
        {
            return signals;
        }

        GameGuild.GeolocationResult? geolocation;
        try
        {
            geolocation = await geolocationProvider
                .ResolveAsync(signalContext.SourceIpAddress, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Provider contract is non-throwing, but a misbehaving implementation must
            // never break the fraud gate.
            logger.LogWarning(
                exception,
                "GeoIP resolution failed for the fraud evaluation of payment {PaymentId}; skipping GeoIP signals",
                signalContext.PaymentId);
            return signals;
        }

        if (geolocation is null)
        {
            return signals;
        }

        var anonymizerTraits = new[]
        {
            geolocation.IsAnonymousVpn,
            geolocation.IsPublicProxy,
            geolocation.IsTorExitNode,
            geolocation.IsHostingProvider
        };

        if (anonymizerTraits.Any(trait => trait == true))
        {
            signals.Add(new FraudSignal(
                Signals.GeoIpAnonymizer,
                Math.Min(options.AnonymizerWeight, options.MaxSignalScore),
                JsonSerializer.Serialize(new
                {
                    isAnonymousVpn = geolocation.IsAnonymousVpn == true,
                    isPublicProxy = geolocation.IsPublicProxy == true,
                    isTorExitNode = geolocation.IsTorExitNode == true,
                    isHostingProvider = geolocation.IsHostingProvider == true
                })));

            logger.LogDebug(
                "Fraud GeoIP evaluation for tenant {TenantId} payment {PaymentId}: source IP is a known anonymizing network",
                signalContext.TenantId,
                signalContext.PaymentId);
        }

        if (!string.IsNullOrWhiteSpace(signalContext.ClientTimezone)
            && !string.IsNullOrWhiteSpace(geolocation.Timezone)
            && !TimezonesEquivalent(signalContext.ClientTimezone, geolocation.Timezone))
        {
            signals.Add(new FraudSignal(
                Signals.GeoIpTimezoneMismatch,
                Math.Min(options.TimezoneMismatchWeight, options.MaxSignalScore),
                JsonSerializer.Serialize(new
                {
                    claimedTimezone = signalContext.ClientTimezone,
                    resolvedTimezone = geolocation.Timezone
                })));

            logger.LogDebug(
                "Fraud GeoIP evaluation for tenant {TenantId} payment {PaymentId}: claimed timezone {Claimed} disagrees with resolved {Resolved}",
                signalContext.TenantId,
                signalContext.PaymentId,
                signalContext.ClientTimezone,
                geolocation.Timezone);
        }

        return signals;
    }

    /// <summary>
    ///     Compares IANA timezone identifiers; fixed-offset identifiers (UTC+01:00) match the
    ///     corresponding Etc/GMT zone so a client reporting its offset does not false-positive
    ///     against a named zone at the same offset.
    /// </summary>
    internal static bool TimezonesEquivalent(string claimed, string resolved)
    {
        var left = Normalize(claimed);
        var right = Normalize(resolved);
        if (left == right)
        {
            return true;
        }

        return TryNormalizeFixedOffset(left, out var leftOffset) && leftOffset == right;
    }

    private static string Normalize(string timezone) => timezone.Trim();

    /// <summary>Maps a fixed-offset identifier (UTC+02:00) to its Etc/GMT equivalent.</summary>
    internal static bool TryNormalizeFixedOffset(string timezone, out string etcGmt)
    {
        etcGmt = string.Empty;
        if (!timezone.StartsWith("UTC", StringComparison.OrdinalIgnoreCase)
            || timezone.Length < 5)
        {
            return false;
        }

        var body = timezone[3..];
        var negative = body.StartsWith("-");
        var magnitude = body.TrimStart('+', '-');
        var parts = magnitude.Split(':');
        if (parts.Length is < 1 or > 2
            || !int.TryParse(parts[0], out var hours)
            || hours is < 0 or > 14)
        {
            return false;
        }

        if (parts.Length == 2)
        {
            if (parts[1].Length != 2
                || !int.TryParse(parts[1], out var minutes)
                || minutes is < 0 or > 59)
            {
                return false;
            }
        }

        // IANA's Etc/GMT zones are sign-inverted relative to UTC+offset notation.
        etcGmt = hours == 0
            ? "Etc/GMT"
            : $"Etc/GMT{(negative ? "+" : "-")}{hours}";
        return true;
    }
}
