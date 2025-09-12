namespace GameGuild;

/// <summary>
///     Base interface for entities with optimistic concurrency control.
/// </summary>
public interface IConcurrencyControlled
{
    /// <summary>
    ///     Version number for optimistic concurrency control.
    /// </summary>
    int Version { get; set; }
}
