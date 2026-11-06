using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     <see cref="IGeoFencingService"/> over <see cref="IGeolocationProvider"/> and
///     <see cref="GeoFencingOptions"/>. The effective blocked set for a sign-in is the union
///     of the platform-wide list and the active tenant's additions. Geolocation failures and
///     unresolvable addresses evaluate per the configured unknown-country posture; the
///     provider contract already guarantees <c>null</c> instead of throwing.
/// </summary>
public sealed class GeoFencingService(
    IGeolocationProvider geolocationProvider,
    IOptions<GeoFencingOptions> options,
    ILogger<GeoFencingService> logger) : IGeoFencingService
{
    public async Task<GeoFenceDecision> EvaluateAsync(string? ipAddress, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return GeoFenceDecision.Allowed;
        }

        GeolocationResult? location = null;
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            location = await geolocationProvider.ResolveAsync(ipAddress, cancellationToken).ConfigureAwait(false);
        }

        var countryCode = location?.CountryCode?.Trim();
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            if (!settings.TreatUnknownCountryAsBlocked)
            {
                // Fail open on unknown geography so a geolocation outage cannot lock every
                // user out; operators in strict jurisdictions flip the posture.
                logger.LogInformation(
                    "Geo-fencing evaluation allowed a sign-in with an unresolved country for IP {IpAddress}",
                    LogRedaction.MaskIpAddress(ipAddress));
                return GeoFenceDecision.Allowed;
            }

            logger.LogWarning(
                "Geo-fencing evaluation blocked a sign-in with an unresolved country for IP {IpAddress}",
                LogRedaction.MaskIpAddress(ipAddress));
            return GeoFenceDecision.Blocked(matchedCountry: null, countryUnknown: true);
        }

        if (!IsBlocked(countryCode, tenantId, settings))
        {
            return GeoFenceDecision.Allowed;
        }

        logger.LogWarning(
            "Geo-fencing match: country {CountryCode} is blocked for tenant {TenantId}",
            countryCode,
            tenantId?.ToString() ?? "platform");
        return GeoFenceDecision.Blocked(countryCode.ToUpperInvariant(), countryUnknown: false);
    }

    private static bool IsBlocked(string countryCode, Guid? tenantId, GeoFencingOptions settings)
    {
        if (Contains(settings.BlockedCountries, countryCode))
        {
            return true;
        }

        return tenantId is { } activeTenant
            && settings.TenantBlockedCountries.TryGetValue(activeTenant.ToString(), out var tenantCountries)
            && Contains(tenantCountries, countryCode);
    }

    private static bool Contains(IEnumerable<string>? codes, string countryCode) =>
        codes?.Any(code => string.Equals(code.Trim(), countryCode, StringComparison.OrdinalIgnoreCase)) == true;
}
