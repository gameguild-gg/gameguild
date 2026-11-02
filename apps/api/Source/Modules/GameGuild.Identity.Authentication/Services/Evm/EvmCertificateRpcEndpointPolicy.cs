using System.Buffers.Binary;
using System.Net;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Server-side request rules for the configured certificate RPC endpoint. Production service
///     registration rejects loopback, private, link-local, shared, reserved and multicast targets
///     before any RPC call is made.
/// </summary>
/// <remarks>
///     <para>
///         The endpoint is operator configuration, not user input; these rules are defense in depth so
///         a compromised or mistyped configuration cannot turn the API host into a proxy aimed at
///         loopback services or internal networks (including cloud instance-metadata endpoints). Only
///         literal addresses and the RFC 6761 <c>localhost</c> names are classified: this policy is
///         enforced once at registration and performs no DNS resolution, keeping registration free of
///         network side effects. DNS names that later resolve into private space are not detected here;
///         the transport's fixed-method, bounded, error-sanitized requests remain the runtime boundary.
///     </para>
///     <para>
///         The isolated loopback HTTPS/Anvil interop fixtures construct their transports directly and
///         never register through this policy, so controlled-boundary testing is unaffected.
///     </para>
///     <para>Violations never echo the endpoint, its host, user information or credentials.</para>
/// </remarks>
public static class EvmCertificateRpcEndpointPolicy
{
    /// <summary>Returns policy violations for an RPC endpoint without echoing the endpoint itself.</summary>
    public static IReadOnlyList<string> Validate(Uri? endpoint)
    {
        var errors = new List<string>();
        if (endpoint is null || !endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("The certificate RPC endpoint must be an absolute HTTPS URI.");
            return errors;
        }

        var host = endpoint.Host;
        if (string.IsNullOrEmpty(host))
        {
            errors.Add("The certificate RPC endpoint must name a host.");
            return errors;
        }

        if (IPAddress.TryParse(host, out var address))
        {
            if (!IsPublicLiteralAddress(address))
            {
                errors.Add(
                    "The certificate RPC endpoint must not target a loopback, private, link-local, shared, reserved or multicast address.");
            }

            return errors;
        }

        var name = host.TrimEnd('.').ToLowerInvariant();
        if (name == "localhost" || name.EndsWith(".localhost", StringComparison.Ordinal))
        {
            errors.Add("The certificate RPC endpoint must not target the reserved localhost name.");
        }

        return errors;
    }

    private static bool IsPublicLiteralAddress(IPAddress address)
    {
        var bytes = address.GetAddressBytes();

        // Unwrap IPv4-mapped IPv6 (for example ::ffff:127.0.0.1) so mapped literals obey IPv4 rules.
        if (bytes.Length == 16 && bytes[10] == 0xFF && bytes[11] == 0xFF && IsZero(bytes, 0, 10))
        {
            return IsPublicIPv4(bytes[12..]);
        }

        return bytes.Length switch
        {
            4 => IsPublicIPv4(bytes),
            16 => IsPublicIPv6(bytes),
            _ => false
        };
    }

    private static bool IsPublicIPv4(byte[] address) =>
        !MatchesPrefix(address, 0x00000000, 8) &&   // "this network"/unspecified source, 0.0.0.0/8
        !MatchesPrefix(address, 0x0A000000, 8) &&   // RFC 1918 private, 10.0.0.0/8
        !MatchesPrefix(address, 0x64400000, 10) &&  // RFC 6598 shared (CGNAT), 100.64.0.0/10
        !MatchesPrefix(address, 0x7F000000, 8) &&   // loopback, 127.0.0.0/8
        !MatchesPrefix(address, 0xA9FE0000, 16) &&  // link-local incl. cloud metadata, 169.254.0.0/16
        !MatchesPrefix(address, 0xAC100000, 12) &&  // RFC 1918 private, 172.16.0.0/12
        !MatchesPrefix(address, 0xC0000000, 24) &&  // IETF protocol assignments, 192.0.0.0/24
        !MatchesPrefix(address, 0xC0000200, 24) &&  // TEST-NET-1 documentation, 192.0.2.0/24
        !MatchesPrefix(address, 0xC0586300, 24) &&  // 6to4 relay (deprecated), 192.88.99.0/24
        !MatchesPrefix(address, 0xC0A80000, 16) &&  // RFC 1918 private, 192.168.0.0/16
        !MatchesPrefix(address, 0xC6120000, 15) &&  // benchmarking, 198.18.0.0/15
        !MatchesPrefix(address, 0xC6336400, 24) &&  // TEST-NET-2 documentation, 198.51.100.0/24
        !MatchesPrefix(address, 0xCB007100, 24) &&  // TEST-NET-3 documentation, 203.0.113.0/24
        !MatchesPrefix(address, 0xE0000000, 4) &&   // multicast, 224.0.0.0/4
        !MatchesPrefix(address, 0xF0000000, 4);     // reserved incl. broadcast, 240.0.0.0/4

    private static bool IsPublicIPv6(byte[] address) =>
        !IsZero(address, 0, 12) &&                  // unspecified ::, loopback ::1 and deprecated IPv4-compatible form
        !MatchesPrefix(address, 0x20010DB8, 32) &&  // RFC 3849 documentation, 2001:db8::/32
        !MatchesPrefix(address, 0x20020000, 16) &&  // 6to4 (deprecated), 2002::/16
        !IsWellKnownNat64(address) &&               // well-known NAT64 translation, 64:ff9b::/96
        !MatchesPrefix(address, 0xFC000000, 7) &&   // unique local, fc00::/7
        !MatchesPrefix(address, 0xFEC00000, 10) &&  // site-local (deprecated), fec0::/10
        !MatchesPrefix(address, 0xFE800000, 10) &&  // link-local, fe80::/10
        !MatchesPrefix(address, 0xFF000000, 8);     // multicast, ff00::/8

    private static bool IsWellKnownNat64(byte[] address) =>
        BinaryPrimitives.ReadUInt32BigEndian(address) == 0x0064FF9B && IsZero(address, 4, 8);

    private static bool MatchesPrefix(byte[] address, uint prefix, int prefixLength)
    {
        var mask = prefixLength == 0 ? 0 : uint.MaxValue << (32 - prefixLength);
        var value = BinaryPrimitives.ReadUInt32BigEndian(address);
        return (value & mask) == (prefix & mask);
    }

    private static bool IsZero(byte[] address, int offset, int count)
    {
        for (var i = offset; i < offset + count; i++)
        {
            if (address[i] != 0)
            {
                return false;
            }
        }

        return true;
    }
}
