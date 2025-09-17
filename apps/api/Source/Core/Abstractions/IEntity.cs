namespace GameGuild;

/// <summary> Generic interface for entities with typed ID (for backward compatibility) </summary>
/// <typeparam name="TKey"> The type of the entity's identifier </typeparam>
public interface IEntity<TKey> : IAuditable, IConcurrencyControlled where TKey : IEquatable<TKey> {
  /// <summary> Unique identifier for the entity with a specific type </summary>
  TKey Id { get; set; }
}

/// <summary> Interface that defines the contract for all entities in the system. Provides the basic structure that all domain entities should implement. </summary>
public interface IEntity : IEntity<Guid> {
  /// <summary> Checks if this entity is newly created (not persisted to the database) </summary>
  bool IsNew { get; }

  /// <summary> Checks if this entity is soft-deleted </summary>
  bool IsDeleted { get; }
}
