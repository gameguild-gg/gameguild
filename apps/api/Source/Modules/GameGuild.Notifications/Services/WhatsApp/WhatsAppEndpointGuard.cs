using System.Net;
using System.Net.Sockets;

namespace GameGuild.Notifications.Services.WhatsApp;

/// <summary>
///     Server-side request guard for the WhatsApp Cloud API endpoint (issues #54/#55).
///     Before the provider opens any connection it validates that the base URL uses
///     http/https only, that the host is not localhost/loopback, and that every resolved
///     address is a public unicast address — private, link-local, loopback, reserved and
///     multicast targets are rejected so a tampered base URL cannot pivot the integration
///     into an SSRF probe of internal infrastructure.
/// </summary>
public sealed class WhatsAppEndpointGuard(Func<string, IPAddress[]>? dnsResolver = null)
{
    private readonly Func<string, IPAddress[]> _dnsResolver =
        dnsResolver ?? (host => Dns.GetHostAddresses(host));

    /// <summary>
    ///     Validates an absolute base URL against the server-side request rules. Throws
    ///     <see cref="InvalidOperationException"/> when the target is not an allowed
    ///     public http(s) endpoint.
    /// </summary>
    public void ValidateBaseUri(Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        if (!baseUri.IsAbsoluteUri)
        {
            throw new InvalidOperationException("The WhatsApp API base URL must be absolute.");
        }

        if (baseUri.Scheme is not "http" and not "https")
        {
            throw new InvalidOperationException(
                $"The WhatsApp API base URL must use http or https, not '{baseUri.Scheme}'.");
        }

        var host = baseUri.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("The WhatsApp API base URL must include a host.");
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("ip6-localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("ip6-loopback", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The WhatsApp API base URL host '{host}' is a local address and is not allowed.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = _dnsResolver(host);
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException(
                $"The WhatsApp API base URL host '{host}' could not be resolved.", ex);
        }

        if (addresses.Length == 0)
        {
            throw new InvalidOperationException(
                $"The WhatsApp API base URL host '{host}' did not resolve to any address.");
        }

        foreach (var address in addresses)
        {
            if (!IsPublicUnicast(address))
            {
                throw new InvalidOperationException(
                    $"The WhatsApp API base URL host '{host}' resolves to the non-public address " +
                    $"'{address}' (loopback/private/link-local/reserved/multicast targets are rejected).");
            }
        }
    }

    /// <summary>
    ///     A target address is allowed only when it is neither loopback, link-local,
    ///     private, reserved nor multicast. IPv4-mapped IPv6 addresses are unwrapped so
    ///     ::ffff:10.0.0.1 cannot sneak through.
    /// </summary>
    public static bool IsPublicUnicast(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily == AddressFamily.InterNetwork
            ? IsPublicIPv4(bytes)
            : IsPublicIPv6(bytes);
    }

    private static bool IsPublicIPv4(byte[] b)
    {
        var first = b[0];
        var second = b[1];

        // 0.0.0.0/8 "this network", 10/8 private, 127/8 loopback, 240/4 reserved,
        // 255.255.255.255 broadcast.
        if (first is 0 or 10 or 127 or >= 240)
        {
            return false;
        }

        // 100.64/10 carrier-grade NAT shared address space.
        if (first == 100 && second >= 64 && second <= 127)
        {
            return false;
        }

        // 169.254/16 link-local (including 169.254.169.254 metadata endpoints).
        if (first == 169 && second == 254)
        {
            return false;
        }

        // 172.16/12 private.
        if (first == 172 && second >= 16 && second <= 31)
        {
            return false;
        }

        // 192.0.0/24 special purpose, 192.0.2/24 TEST-NET-1.
        if (first == 192 && second == 0 && b[2] is 0 or 2)
        {
            return false;
        }

        // 192.88.99/24 6to4 relay anycast (deprecated).
        if (first == 192 && second == 88 && b[2] == 99)
        {
            return false;
        }

        // 192.168/16 private.
        if (first == 192 && second == 168)
        {
            return false;
        }

        // 198.18/15 benchmarking, 198.51.100/24 TEST-NET-2.
        if (first == 198 && ((second == 18 || second == 19) || (second == 51 && b[2] == 100)))
        {
            return false;
        }

        // 203.0.113/24 TEST-NET-3.
        if (first == 203 && second == 0 && b[2] == 113)
        {
            return false;
        }

        return true;
    }

    private static bool IsPublicIPv6(byte[] b)
    {
        // Unspecified (::) and loopback (::1).
        if (b.All(static octet => octet == 0))
        {
            return false;
        }

        if (b[..15].All(static octet => octet == 0) && b[15] == 1)
        {
            return false;
        }

        // IPv4-mapped targets were unwrapped earlier; other embedded forms (::a.b.c.d,
        // ::ffff:a.b.c.d) are rejected by the prefix checks above plus the IPv4 check on
        // the tail when it maps.
        if (b[..10].All(static octet => octet == 0) && b[10] == 0xff && b[11] == 0xff)
        {
            return IsPublicIPv4(b[12..]);
        }

        // Link-local fe80::/10.
        if (b[0] == 0xfe && (b[1] & 0xC0) == 0x80)
        {
            return false;
        }

        // Unique-local fc00::/7.
        if ((b[0] & 0xFE) == 0xFC)
        {
            return false;
        }

        // Multicast ff00::/8 (includes solicited-node multicast).
        if (b[0] == 0xff)
        {
            return false;
        }

        // Site-local fec0::/10 (deprecated).
        if (b[0] == 0xfe && (b[1] & 0xC0) == 0xC0)
        {
            return false;
        }

        // Documentation 2001:db8::/32.
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8)
        {
            return false;
        }

        return true;
    }
}
