namespace GameGuild;

/// <summary>
///     Base interface for entities with optimistic concurrency control.
///     Version is read-only in the interface; EF Core manages it via the backing field.
/// </summary>
public interface IConcurrencyControlled
{
    /// <summary>
    ///     Version number for optimistic concurrency control.
    /// </summary>
    int Version { get; }
}
