namespace GameGuild;

/// <summary>
///     Base interface for entities that track their lifecycle.
///     This provides a cleaner separation of auditing concerns.
/// </summary>
public interface IAuditable
{
    /// <summary>
    ///     Timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    ///     Timestamp when the entity was last updated.
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    ///     Timestamp when the entity was soft-deleted (null if not deleted).
    /// </summary>
    DateTime? DeletedAt { get; set; }

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
