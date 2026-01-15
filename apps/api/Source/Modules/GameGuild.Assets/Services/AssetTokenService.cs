using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GameGuild.Assets;

/// <summary>
/// Options for asset token configuration.
/// </summary>
public class AssetTokenOptions
{
    public const string SectionName = "Assets:Token";

    /// <summary>
    /// Secret key for HMAC signing (base64 encoded).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Default token validity in hours.
    /// </summary>
    public int DefaultExpiryHours { get; set; } = 24;

    /// <summary>
    /// Time window size in hours (for rotation).
    /// </summary>
    public int TimeWindowHours { get; set; } = 8;
}

/// <summary>
/// Implementation of asset token generation and validation.
/// Uses HMAC-SHA256 with movable time windows for anti-stampede.
/// </summary>
public class AssetTokenService : IAssetTokenService
{
    private readonly byte[] _secretKey;
    private readonly int _defaultExpiryHours;
    private readonly int _timeWindowHours;

    public AssetTokenService(IOptions<AssetTokenOptions> options)
    {
        var opts = options.Value;
        
        // Generate a key if not provided (development only)
        if (string.IsNullOrEmpty(opts.SecretKey))
        {
            _secretKey = RandomNumberGenerator.GetBytes(32);
        }
        else
        {
            _secretKey = Convert.FromBase64String(opts.SecretKey);
        }
        
        _defaultExpiryHours = opts.DefaultExpiryHours > 0 ? opts.DefaultExpiryHours : 24;
        _timeWindowHours = opts.TimeWindowHours > 0 ? opts.TimeWindowHours : 8;
    }

    /// <summary>
    /// Generates a signed access token for an asset.
    /// </summary>
    public string GenerateToken(
        Guid assetReferenceId,
        Guid tenantId,
        AssetAccessPolicy accessPolicy,
        TransformationSpec? transformation = null,
        TimeSpan? customExpiry = null)
    {
        var timeWindow = GetCurrentTimeWindow();
        var expiry = DateTimeOffset.UtcNow.Add(customExpiry ?? TimeSpan.FromHours(_defaultExpiryHours));
        var expiryTimestamp = expiry.ToUnixTimeSeconds();
        var transformSpec = transformation?.ToCanonicalString() ?? string.Empty;

        var payload = BuildPayload(assetReferenceId, timeWindow, expiryTimestamp, accessPolicy, transformSpec, tenantId);
        var signature = ComputeSignature(payload);

        // Encode: timeWindow (2 bytes) + expiry (4 bytes) + signature (16 bytes) = 22 bytes base64
        var tokenBytes = new byte[22];
        BitConverter.GetBytes((short)timeWindow).CopyTo(tokenBytes, 0);
        BitConverter.GetBytes((int)(expiryTimestamp - GetBaseTimestamp())).CopyTo(tokenBytes, 2);
        signature.AsSpan(0, 16).CopyTo(tokenBytes.AsSpan(6));

        return Base64UrlEncode(tokenBytes);
    }

    /// <summary>
    /// Validates a token and returns the decoded payload if valid.
    /// </summary>
    public AssetTokenPayload? ValidateToken(string token, Guid assetReferenceId, Guid tenantId)
    {
        try
        {
            var tokenBytes = Base64UrlDecode(token);
            if (tokenBytes.Length < 22)
                return null;

            var timeWindow = BitConverter.ToInt16(tokenBytes, 0);
            var expiryOffset = BitConverter.ToInt32(tokenBytes, 2);
            var expiryTimestamp = GetBaseTimestamp() + expiryOffset;
            var providedSignature = tokenBytes.AsSpan(6, 16);

            // Check time window (current or previous)
            var currentWindow = GetCurrentTimeWindow();
            if (timeWindow != currentWindow && timeWindow != currentWindow - 1)
                return null;

            // Check expiry
            if (expiryTimestamp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return null;

            // Verify signature for all possible access policies
            foreach (var accessPolicy in Enum.GetValues<AssetAccessPolicy>())
            {
                var payload = BuildPayload(assetReferenceId, timeWindow, expiryTimestamp, accessPolicy, string.Empty, tenantId);
                var expectedSignature = ComputeSignature(payload);

                if (providedSignature.SequenceEqual(expectedSignature.AsSpan(0, 16)))
                {
                    return new AssetTokenPayload(
                        assetReferenceId,
                        timeWindow,
                        expiryTimestamp,
                        accessPolicy,
                        string.Empty,
                        tenantId);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the current time window index.
    /// </summary>
    public int GetCurrentTimeWindow()
    {
        var totalHours = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600);
        return totalHours / _timeWindowHours;
    }

    private string BuildPayload(
        Guid assetReferenceId,
        int timeWindow,
        long expiryTimestamp,
        AssetAccessPolicy accessPolicy,
        string transformSpec,
        Guid tenantId)
    {
        return $"{assetReferenceId}|{timeWindow}|{expiryTimestamp}|{(int)accessPolicy}|{transformSpec}|{tenantId}";
    }

    private byte[] ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_secretKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static long GetBaseTimestamp()
    {
        // Use Jan 1, 2024 as base to save bytes
        return new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
