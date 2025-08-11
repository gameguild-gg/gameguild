using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Tenants;


namespace GameGuild.Common;

/// <summary>
/// Generic base entity class that provides common properties and functionality for all domain entities.
/// Supports different ID types while maintaining the same base functionality.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier</typeparam>
public abstract class Entity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey> {
  /// <summary>
  /// Unique identifier for the entity
  /// </summary>
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public virtual TKey Id { get; set; } = default!;

  /// <summary>
  /// Unique identifier for the entity (IEntity implementation)
  /// </summary>
  Guid IEntity.Id {
    get => Id is Guid guid ? guid : throw new InvalidOperationException("Entity ID is not a GUID");
    set => Id = (TKey)(object)value;
  }

  /// <summary>
  /// Version number for optimistic concurrency control
  /// Uses ConcurrencyCheck for cross-database compatibility (PostgreSQL, SQLite, SQL Server)
  /// </summary>
  [ConcurrencyCheck]
  public virtual int Version { get; set; } = 0;

  /// <summary>
  /// Timestamp when the entity was created
  /// </summary>
  [Required]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Timestamp when the entity was last updated
  /// </summary>
  [Required]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public virtual DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Timestamp when the entity was soft-deleted (null if not deleted)
  /// </summary>
  public virtual DateTime? DeletedAt { get; set; }

  /// <summary>
  /// Default constructor
  /// </summary>
  protected Entity() { }

  /// <summary>
  /// Constructor for partial initialization (useful for updates)
  /// Mirrors the NestJS EntityDto constructor pattern
  /// </summary>
  /// <param name="partial">Partial entity data to initialize with</param>
  protected Entity(object partial) : this() {
    var properties = partial.GetType().GetProperties();
    var entityType = GetType();

    foreach (var sourceProperty in properties) {
      var targetProperty = entityType.GetProperty(sourceProperty.Name);

      if (targetProperty == null || !targetProperty.CanWrite) continue;

      try {
        var value = sourceProperty.GetValue(partial);

        if (value != null) {
          // Handle type conversion if necessary
          if (value.GetType() != targetProperty.PropertyType) {
            var targetType = Nullable.GetUnderlyingType(targetProperty.PropertyType) ?? targetProperty.PropertyType;
            value = Convert.ChangeType(value, targetType);
          }

          targetProperty.SetValue(this, value);
        }
      }
      catch (Exception) {
        // Silently ignore conversion errors
      }
    }
  }

  /// <summary>
  /// Updates the UpdatedAt timestamp to the current UTC time
  /// </summary>
  public virtual void Touch() { UpdatedAt = DateTime.UtcNow; }

  /// <summary>
  /// Sets multiple properties from a dictionary (useful for partial updates)
  /// </summary>
  /// <param name="properties">Dictionary of property names and values</param>
  public virtual void SetProperties(Dictionary<string, object?> properties) {
    var entityType = GetType();

    foreach (var property in properties) {
      var propertyInfo = entityType.GetProperty(property.Key);

      if (propertyInfo == null || !propertyInfo.CanWrite) continue;

      try {
        // Handle type conversion if necessary
        var value = property.Value;

        if (value != null && value.GetType() != propertyInfo.PropertyType) {
          // Handle nullable types
          var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
          value = Convert.ChangeType(value, targetType);
        }

        propertyInfo.SetValue(this, value);

        // Don't auto-update UpdatedAt for CreatedAt changes
        if (property.Key != nameof(CreatedAt)) Touch();
      }
      catch (Exception) {
        // Silently ignore conversion errors for now
        // In a production app, you might want to log these or handle them differently
      }
    }
  }

  /// <summary>
  /// Checks if this entity is newly created (not yet persisted to a database)
  /// </summary>
  public virtual bool IsNew {
    get => Version == 0;
  }

  /// <summary>
  /// Checks if this entity is soft-deleted
  /// </summary>
  public virtual bool IsDeleted {
    get => DeletedAt.HasValue;
  }

  /// <summary>
  /// Soft-delete the entity by setting DeletedAt timestamp
  /// </summary>
  public virtual void SoftDelete() {
    if (IsDeleted) return;

    DeletedAt = DateTime.UtcNow;
    Touch();
  }

  /// <summary>
  /// Restore a soft-deleted entity by clearing DeletedAt timestamp
  /// </summary>
  public virtual void Restore() {
    if (!IsDeleted) return;

    DeletedAt = null;
    Touch();
  }

  /// <summary>
  /// Gets a dictionary representation of the entity's current state
  /// </summary>
  /// <returns>Dictionary with property names and values</returns>
  public virtual Dictionary<string, object?> ToDictionary() {
    var result = new Dictionary<string, object?>();
    var properties = GetType().GetProperties();

    foreach (var property in properties) {
      if (property.CanRead) result[property.Name] = property.GetValue(this);
    }

    return result;
  }

  /// <summary>
  /// Override for better debugging and logging
  /// </summary>
  public override string ToString() {
    var deletedStatus = IsDeleted ? " (DELETED)" : "";

    return
      $"{GetType().Name} {{ Id = {Id}, Version = {Version}, CreatedAt = {CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt = {UpdatedAt:yyyy-MM-dd HH:mm:ss}{deletedStatus} }}";
  }

  // Tenant logic for ITenantable
  public virtual Tenant? Tenant { get; set; }

  public virtual bool IsGlobal {
    get => Tenant == null;
  }

  private readonly List<IDomainEvent> _domainEvents = [];

  public List<IDomainEvent> DomainEvents {
    get => [.. _domainEvents];
  }

  public void ClearDomainEvents() { _domainEvents.Clear(); }

  public void Raise(IDomainEvent domainEvent) { _domainEvents.Add(domainEvent); }
}

/// <summary>
/// Base entity class that provides common properties and functionality for all domain entities.
/// </summary>
public class Entity : Entity<Guid> {
  /// <summary>
  /// Unique identifier for the entity
  /// </summary>
  [GraphQLType(typeof(NonNullType<UuidType>))]
  [GraphQLDescription("The unique identifier for the entity (UUID).")]
  public sealed override Guid Id { get; set; }

  /// <summary>
  /// Version number for optimistic concurrency control
  /// </summary>
  [GraphQLType(typeof(NonNullType<IntType>))]
  [GraphQLDescription("Version number for optimistic concurrency control.")]
  public override int Version { get; set; }

  /// <summary>
  /// Timestamp when the entity was created
  /// </summary>
  [GraphQLType(typeof(NonNullType<DateTimeType>))]
  [GraphQLDescription("The date and time when the entity was created.")]
  public sealed override DateTime CreatedAt { get; set; }

  /// <summary>
  /// Timestamp when the entity was last updated
  /// </summary>
  [GraphQLType(typeof(NonNullType<DateTimeType>))]
  [GraphQLDescription("The date and time when the entity was last updated.")]
  public sealed override DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Timestamp when the entity was soft-deleted (null if not deleted)
  /// </summary>
  [GraphQLType(typeof(DateTimeType))]
  [GraphQLDescription("The date and time when the entity was soft deleted (null if not deleted).")]
  public sealed override DateTime? DeletedAt { get; set; }

  /// <summary>
  /// Whether the entity is soft-deleted
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>))]
  [GraphQLDescription("Indicates whether the entity has been soft deleted.")]
  public override bool IsDeleted {
    get => DeletedAt.HasValue;
  }

  /// <summary>
  /// The tenant this entity belongs to (null if global)
  /// </summary>
  public override Tenant? Tenant { get; set; }

  /// <summary>
  /// Default constructor
  /// </summary>
  protected Entity() {
    // Generate new GUID for new entities
    if (Id == Guid.Empty) Id = Guid.NewGuid();
  }

  /// <summary>
  /// Constructor for partial initialization (useful for updates)
  /// </summary>
  /// <param name="partial">Partial entity data to initialize with</param>
  protected Entity(object partial) : base(partial) {
    // Generate new GUID for new entities if not provided in partial
    if (Id == Guid.Empty) Id = Guid.NewGuid();
  }

  /// <summary>
  /// Static factory method to create an entity with initial properties
  /// </summary>
  /// <typeparam name="T">The entity type</typeparam>
  /// <param name="partial">Initial properties</param>
  /// <returns>New instance of the entity</returns>
  public static T Create<T>(object partial) where T : Entity, new() {
    // Create an instance and set properties
    var instance = new T();

    switch (partial) {
      case null: break;
      // Handle Dictionary<string, object?> case
      case Dictionary<string, object?> dict: instance.SetProperties(dict); break;

      default: {
        // Handle an anonymous object case
        var properties = partial.GetType().GetProperties();
        var propDict = new Dictionary<string, object?>();

        foreach (var prop in properties) propDict[prop.Name] = prop.GetValue(partial);

        instance.SetProperties(propDict);

        break;
      }
    }

    return instance;
  }

  /// <summary>
  /// Static factory method to create an entity (parameterless)
  /// </summary>
  /// <typeparam name="T">The entity type</typeparam>
  /// <returns>New instance of the entity</returns>
  public static T Create<T>() where T : Entity, new() { return new T(); }
}
