using GameGuild;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Shared base record for all CQRS requests (commands and queries),
///     providing a unique identifier and creation timestamp.
/// </summary>
public abstract record RequestBase
{
    /// <summary>
    ///     Unique identifier for this request.
    /// </summary>
    public Guid RequestId { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     When the request was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = SystemClock.UtcNow;
}
