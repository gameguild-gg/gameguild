namespace GameGuild.Abstractions;

/// <summary>
///     Base record class for immutable domain entities.
///     Use this for entities that should be treated as immutable value objects with identity.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier</typeparam>
public abstract record EntityRecord<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    ///     Unique identifier for the entity.
    /// </summary>
    public virtual TKey Id { get; init; } = default!;

    /// <summary>
    ///     Unique identifier that associates the entity with a specific tenant in a multi-tenant system.
    /// </summary>
    public virtual Guid TenantId { get; init; }

    /// <summary>
    ///     Timestamp when the entity was created.
    /// </summary>
    public virtual DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    ///     Timestamp when the entity was last updated.
    /// </summary>
    public virtual DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    ///     Timestamp when the entity was soft-deleted (null if not deleted).
    /// </summary>
    public virtual DateTime? DeletedAt { get; init; }

    /// <summary>
    ///     Version number for optimistic concurrency control.
    /// </summary>
    public virtual int Version { get; init; }

    /// <summary>
    ///     Creates a new instance with updated timestamp.
    /// </summary>
    /// <returns>A new instance with UpdatedAt set to current UTC time</returns>
    public virtual EntityRecord<TKey> WithTouch() { return this with { UpdatedAt = DateTime.UtcNow }; }

    /// <summary>
    ///     Creates a new instance marked as soft-deleted.
    /// </summary>
    /// <returns>A new instance with DeletedAt set to current UTC time</returns>
    public virtual EntityRecord<TKey> WithSoftDelete() { return this with { DeletedAt = DateTime.UtcNow }; }

    /// <summary>
    ///     Creates a new instance with soft-delete restored.
    /// </summary>
    /// <returns>A new instance with DeletedAt set to null</returns>
    public virtual EntityRecord<TKey> WithRestore() { return this with { DeletedAt = null }; }
}
