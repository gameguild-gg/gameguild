using System.Net;
using System.Security.Claims;

namespace GameGuild.API;

/// <summary>
/// Configures identities that bypass rate limits and identities that are denied at the API boundary.
/// </summary>
public sealed class RateLimitAccessOptions
{
    private HashSet<string> _allowlistedUserIds = new(StringComparer.Ordinal);
    private HashSet<string> _denylistedUserIds = new(StringComparer.Ordinal);
    private HashSet<IPAddress> _allowlistedIpAddresses = [];
    private HashSet<IPAddress> _denylistedIpAddresses = [];

    public string[] AllowlistedUserIds { get; set; } = [];
    public string[] DenylistedUserIds { get; set; } = [];
    public string[] AllowlistedIpAddresses { get; set; } = [];
    public string[] DenylistedIpAddresses { get; set; } = [];

    public void Validate()
    {
        _allowlistedUserIds = ParseUserIds(AllowlistedUserIds, nameof(AllowlistedUserIds));
        _denylistedUserIds = ParseUserIds(DenylistedUserIds, nameof(DenylistedUserIds));
        _allowlistedIpAddresses = ParseIpAddresses(AllowlistedIpAddresses, nameof(AllowlistedIpAddresses));
        _denylistedIpAddresses = ParseIpAddresses(DenylistedIpAddresses, nameof(DenylistedIpAddresses));
    }

    internal bool IsAllowlisted(HttpContext context)
    {
        var userId = GetUserId(context);
        if (userId is not null && _allowlistedUserIds.Contains(userId))
        {
            return true;
        }

        var ipAddress = Normalize(context.Connection.RemoteIpAddress);
        return ipAddress is not null && _allowlistedIpAddresses.Contains(ipAddress);
    }

    internal bool IsDenylisted(HttpContext context)
    {
        var userId = GetUserId(context);
        if (userId is not null && _denylistedUserIds.Contains(userId))
        {
            return true;
        }

        var ipAddress = Normalize(context.Connection.RemoteIpAddress);
        return ipAddress is not null && _denylistedIpAddresses.Contains(ipAddress);
    }

    private static string? GetUserId(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }

    private static HashSet<string> ParseUserIds(IEnumerable<string> values, string optionName)
    {
        var userIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Rate limiting access option '{optionName}' cannot contain an empty user ID.");
            }

            userIds.Add(value.Trim());
        }

        return userIds;
    }

    private static HashSet<IPAddress> ParseIpAddresses(IEnumerable<string> values, string optionName)
    {
        var addresses = new HashSet<IPAddress>();
        foreach (var value in values)
        {
            if (!IPAddress.TryParse(value, out var address))
            {
                throw new InvalidOperationException(
                    $"Rate limiting access option '{optionName}' contains invalid IP address '{value}'.");
            }

            var normalized = Normalize(address);
            if (normalized is not null)
            {
                addresses.Add(normalized);
            }
        }

        return addresses;
    }

    private static IPAddress? Normalize(IPAddress? address)
    {
        return address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4() : address;
    }
}
