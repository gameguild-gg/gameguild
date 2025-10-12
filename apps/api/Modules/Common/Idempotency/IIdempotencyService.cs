namespace GameGuild.Modules.Common.Idempotency;

/// <summary>
/// Service for managing idempotent request processing
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Checks if a request with the given idempotency key has already been processed
    /// </summary>
    Task<IdempotencyResult?> GetResultAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the result of a processed request with its idempotency key
    /// </summary>
    Task StoreResultAsync(string idempotencyKey, object result, int statusCode, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired idempotency records
    /// </summary>
    Task CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a stored idempotent request result
/// </summary>
public sealed class IdempotencyResult
{
    public required string IdempotencyKey { get; init; }
    public required string ResultJson { get; init; }
    public required int StatusCode { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
