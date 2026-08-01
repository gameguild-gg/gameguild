using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Features;

/// <summary>
///     Deterministic hash-based rollout calculator for percentage-based feature releases
///     Uses SHA256 hashing to ensure consistent and fair distribution
/// </summary>
public static class RolloutHashCalculator
{
    private static readonly ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Shared;

    /// <summary>
    ///     Determines if a user/tenant should be included in a percentage rollout
    /// </summary>
    /// <param name="identifier">Unique identifier (userId, tenantId, etc.)</param>
    /// <param name="percentage">Rollout percentage (0-100)</param>
    /// <param name="salt">Optional salt for consistent but different bucketing</param>
    /// <returns>True if the identifier falls within the rollout percentage</returns>
    /// <exception cref="ArgumentException">Thrown when identifier is null or empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when percentage is not between 0 and 100</exception>
    public static bool IsInRollout(string identifier, int percentage, string? salt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (percentage >= FeatureFlagConstants.MaxRolloutPercentage) return true;

        if (percentage <= FeatureFlagConstants.MinRolloutPercentage) return false;

        var hashInput = $"{identifier}:{salt ?? FeatureFlagConstants.DefaultRolloutSalt}";
        var bucketValue = CalculateBucket(hashInput);

        return bucketValue < percentage;
    }

    /// <summary>
    ///     Creates an identifier from a feature context with fallback chain
    ///     Priority: TenantId -> UserId -> IpAddress -> "anonymous"
    /// </summary>
    /// <param name="context">Feature evaluation context</param>
    /// <returns>Best available identifier from context</returns>
    public static string CreateIdentifier(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TenantId?.ToString() ?? context.UserId?.ToString() ?? context.IpAddress ?? FeatureFlagConstants.AnonymousIdentifier;
    }

    /// <summary>
    ///     Calculates a deterministic bucket value (0-99) for an identifier
    ///     Uses SHA256 for consistent hashing across different runs
    /// </summary>
    /// <param name="hashInput">The input string to hash</param>
    /// <returns>Bucket value between 0 and 99</returns>
    private static uint CalculateBucket(string hashInput)
    {
        var buffer = ByteArrayPool.Rent(32);

        try
        {
            var inputBytes = Encoding.UTF8.GetBytes(hashInput);
            _ = SHA256.HashData(inputBytes, buffer.AsSpan(0, 32));

            // Use first 4 bytes to create a uint
            return BitConverter.ToUInt32(buffer, 0) % 100;
        }
        finally { ByteArrayPool.Return(buffer); }
    }

    /// <summary>
    ///     Validates a rollout percentage value
    /// </summary>
    /// <param name="percentage">Percentage to validate</param>
    /// <returns>True if percentage is valid (0-100)</returns>
    public static bool IsValidPercentage(int percentage) { return percentage is >= FeatureFlagConstants.MinRolloutPercentage and <= FeatureFlagConstants.MaxRolloutPercentage; }

    /// <summary>
    ///     Calculates the bucket value for an identifier (useful for testing/debugging)
    /// </summary>
    /// <param name="identifier">Unique identifier</param>
    /// <param name="salt">Optional salt</param>
    /// <returns>Bucket value (0-99)</returns>
    public static uint GetBucketValue(string identifier, string? salt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var hashInput = $"{identifier}:{salt ?? FeatureFlagConstants.DefaultRolloutSalt}";

        return CalculateBucket(hashInput);
    }
}
