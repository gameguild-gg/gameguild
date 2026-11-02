using System.Net;
using System.Net.Sockets;
using GameGuild;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Geolocation;

/// <summary>
///     Offline MaxMind GeoLite2 adapter for <see cref="IGeolocationProvider"/>. The city
///     database reader is opened once from <c>Geolocation:MaxMind:DatabasePath</c> and is
///     thread-safe for concurrent lookups; an optional GeoIP2 Anonymous IP database
///     (<c>Geolocation:MaxMind:AnonymousDatabasePath</c>) additionally resolves
///     VPN/proxy/Tor/hosting traits (issue #278). Fail-closed contract: missing/unreadable
///     databases, private/loopback addresses and lookup misses resolve to <c>null</c>
///     (Unknown) — enrichment degrades gracefully and never fails the login pipeline.
/// </summary>
public sealed class MaxMindGeolocationProvider : IGeolocationProvider, IDisposable
{
    private readonly DatabaseReader? reader;
    private readonly DatabaseReader? anonymousReader;
    private readonly ILogger<MaxMindGeolocationProvider> logger;
    private bool disposed;

    public MaxMindGeolocationProvider(IOptions<MaxMindGeolocationOptions> options, ILogger<MaxMindGeolocationProvider> logger)
        : this(options.Value.DatabasePath, logger, options.Value.AnonymousDatabasePath)
    {
    }

    internal MaxMindGeolocationProvider(string? databasePath, ILogger<MaxMindGeolocationProvider> logger, string? anonymousDatabasePath = null)
    {
        this.logger = logger;
        reader = OpenDatabase(databasePath, "geolocation");
        anonymousReader = OpenDatabase(anonymousDatabasePath, "anonymous-IP");
    }

    private DatabaseReader? OpenDatabase(string? databasePath, string purpose)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            logger.LogInformation("MaxMind {Purpose} database is not configured; enrichment stays disabled", purpose);
            return null;
        }

        if (!File.Exists(databasePath))
        {
            logger.LogWarning("MaxMind {Purpose} database not found at {DatabasePath}; enrichment stays disabled", purpose, databasePath);
            return null;
        }

        try
        {
            var opened = new DatabaseReader(databasePath);
            logger.LogInformation("MaxMind {Purpose} database loaded from {DatabasePath}", purpose, databasePath);
            return opened;
        }
        catch (Exception ex)
        {
            // Fail closed: an unreadable database must disable enrichment, not startup.
            logger.LogError(ex, "MaxMind {Purpose} database at {DatabasePath} could not be opened; enrichment stays disabled", purpose, databasePath);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<GeolocationResult?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if ((reader is null && anonymousReader is null) || string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out var address))
        {
            return Task.FromResult<GeolocationResult?>(null);
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.IsPrivate())
        {
            return Task.FromResult<GeolocationResult?>(null);
        }

        var result = ResolveCity(address);
        var anonymous = ResolveAnonymousTraits(address);

        // An address with no city data and no anonymous-network match is a miss, not a
        // partial result. A known-non-anonymous address without city data is still a miss:
        // there is nothing to enrich.
        var isAnonymousNetwork = anonymous is { } traits &&
            (traits.IsAnonymousVpn == true || traits.IsPublicProxy == true || traits.IsTorExitNode == true || traits.IsHostingProvider == true);
        if (result is null && !isAnonymousNetwork)
        {
            return Task.FromResult<GeolocationResult?>(null);
        }

        if (anonymous is { } resolvedTraits)
        {
            result ??= new GeolocationResult();
            result = result with
            {
                IsAnonymousVpn = resolvedTraits.IsAnonymousVpn,
                IsPublicProxy = resolvedTraits.IsPublicProxy,
                IsTorExitNode = resolvedTraits.IsTorExitNode,
                IsHostingProvider = resolvedTraits.IsHostingProvider
            };
        }

        return Task.FromResult<GeolocationResult?>(result);
    }

    private GeolocationResult? ResolveCity(IPAddress address)
    {
        if (reader is null)
        {
            return null;
        }

        try
        {
            var response = reader.City(address);
            if (response is null)
            {
                return null;
            }

            var result = new GeolocationResult
            {
                Country = NullIfEmpty(response.Country?.Name),
                CountryCode = NullIfEmpty(response.Country?.IsoCode),
                Region = NullIfEmpty(response.MostSpecificSubdivision?.Name),
                City = NullIfEmpty(response.City?.Name),
                PostalCode = NullIfEmpty(response.Postal?.Code),
                Latitude = response.Location?.Latitude,
                Longitude = response.Location?.Longitude,
                Timezone = NullIfEmpty(response.Location?.TimeZone)
            };

            // An address with no country data at all is a miss, not a partial result.
            return result.CountryCode is null ? null : result;
        }
        catch (Exception ex) when (ex is AddressNotFoundException or GeoIP2Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     Resolves anonymous-network traits. <c>null</c> = no data (database absent or lookup
    ///     failed); an all-<c>false</c> result means the database is available and the address
    ///     is not listed as anonymous.
    /// </summary>
    private GeolocationResult? ResolveAnonymousTraits(IPAddress address)
    {
        if (anonymousReader is null)
        {
            return null;
        }

        try
        {
            var response = anonymousReader.AnonymousIP(address);
            if (response is null)
            {
                return null;
            }

            return new GeolocationResult
            {
                IsAnonymousVpn = response.IsAnonymousVpn,
                IsPublicProxy = response.IsPublicProxy,
                IsTorExitNode = response.IsTorExitNode,
                IsHostingProvider = response.IsHostingProvider
            };
        }
        catch (AddressNotFoundException)
        {
            // Not listed in the anonymous database: known non-anonymous, not unknown.
            return new GeolocationResult
            {
                IsAnonymousVpn = false,
                IsPublicProxy = false,
                IsTorExitNode = false,
                IsHostingProvider = false
            };
        }
        catch (GeoIP2Exception)
        {
            // Unreadable answer: unknown rather than known-clean.
            return null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        reader?.Dispose();
        anonymousReader?.Dispose();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>IPAddress extension kept next to its only consumer.</summary>
file static class IPAddressExtensions
{
    public static bool IsPrivate(this IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)
                || bytes[0] == 127;
        }

        // IPv6: unique-local (fc00::/7) and link-local (fe80::/10).
        var ipv6 = address.GetAddressBytes();
        return (ipv6[0] & 0xFE) == 0xFC || (ipv6[0] == 0xFE && (ipv6[1] & 0xC0) == 0x80);
    }
}
