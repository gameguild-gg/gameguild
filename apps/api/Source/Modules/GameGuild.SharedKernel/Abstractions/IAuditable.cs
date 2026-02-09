namespace GameGuild;

/// <summary>
///     Base interface for entities that track their lifecycle.
///     This provides a cleaner separation of auditing concerns.
///     Properties are read-only in the interface — mutations go through <see cref="Touch"/>,
///     <see cref="SoftDelete"/>, and <see cref="Restore"/> methods.
/// </summary>
public interface IAuditable
{
    /// <summary>
    ///     Timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    ///     Timestamp when the entity was last updated.
    /// </summary>
    DateTime UpdatedAt { get; }

    /// <summary>
    ///     Timestamp when the entity was soft-deleted (null if not deleted).
    /// </summary>
    DateTime? DeletedAt { get; }

    /// <summary>
    ///     Updates the UpdatedAt timestamp to the current UTC time.
    /// </summary>
    void Touch();

    /// <summary>
    ///     Marks the entity as soft-deleted by setting DeletedAt to the current UTC time.
    /// </summary>
    void SoftDelete();

    /// <summary>
    ///     Restores a soft-deleted entity by clearing the DeletedAt timestamp.
    /// </summary>
    void Restore();
}
