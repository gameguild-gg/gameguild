using GameGuild.CQRS;
using GameGuild.Modules.Tenants;


namespace GameGuild;

/// <summary>
///   Generic base entity class that provides common properties and functionality for all domain entities. Supports different ID types while maintaining the same base functionality. Implements separation of concerns through IAuditable
///   and IConcurrencyControlled interfaces.
/// </summary>
/// <typeparam name="TKey"> The type of the entity's identifier </typeparam>
public abstract class EntityBase<TKey> : IEntity<TKey>, IHasDomainEvents where TKey : IEquatable<TKey> {
  private readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();

  /// <summary> Default constructor </summary>
  protected EntityBase() { }

  /// <summary> Constructor for partial initialization (useful for updates) Mirrors the NestJS EntityDto constructor pattern </summary>
  /// <param name="partial"> Partial entity data to initialize with </param>
  protected EntityBase(object partial) : this() { SetPropertiesFromObject(partial); }

  /// <summary> The tenant this entity belongs to (null if global) </summary>
  public virtual Tenant? Tenant { get; set; }

  /// <summary> Whether the entity is global (not tenant-specific) </summary>
  public virtual bool IsGlobal { get => Tenant == null; }

  /// <summary> Checks if this entity is newly created (not yet persisted to a database) </summary>
  public virtual bool IsNew { get => Version == 0; }

  /// <summary> Checks if this entity is soft-deleted </summary>
  public virtual bool IsDeleted { get => DeletedAt.HasValue; }

  /// <summary> Unique identifier for the entity </summary>
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public virtual TKey Id { get; set; } = default!;

  /// <summary> Version number for optimistic concurrency control Uses ConcurrencyCheck for cross-database compatibility (PostgreSQL, SQLite, SQL Server) </summary>
  [ConcurrencyCheck]
  public virtual int Version { get; set; } = 0;

  /// <summary> Timestamp when the entity was created </summary>
  [Required]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary> Timestamp when the entity was last updated </summary>
  [Required]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public virtual DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  /// <summary> Timestamp when the entity was soft-deleted (null if not deleted) </summary>
  public virtual DateTime? DeletedAt { get; set; }

  /// <summary> Updates the UpdatedAt timestamp to the current UTC time </summary>
  public virtual void Touch() { UpdatedAt = DateTime.UtcNow; }

  /// <summary> Soft-delete the entity by setting DeletedAt timestamp </summary>
  public virtual void SoftDelete() {
    if (IsDeleted) return;

    DeletedAt = DateTime.UtcNow;
    Touch();
  }

  /// <summary> Restore a soft-deleted entity by clearing DeletedAt timestamp </summary>
  public virtual void Restore() {
    if (!IsDeleted) return;

    DeletedAt = null;
    Touch();
  }

  /// <summary> Domain events raised by this entity </summary>
  public IReadOnlyList<IDomainEvent> DomainEvents { get => _domainEvents.AsReadOnly(); }

  /// <summary> Adds a domain event to the entity's event collection </summary>
  /// <param name="domainEvent"> The domain event to add </param>
  public void AddDomainEvent(IDomainEvent domainEvent) { _domainEvents.Add(domainEvent); }

  /// <summary> Removes a specific domain event from the entity's event collection </summary>
  /// <param name="domainEvent"> The domain event to remove </param>
  public void RemoveDomainEvent(IDomainEvent domainEvent) { _domainEvents.Remove(domainEvent); }

  /// <summary> Clears all domain events from the entity's event collection </summary>
  public void ClearDomainEvents() { _domainEvents.Clear(); }

  /// <summary> Sets multiple properties from a dictionary (useful for partial updates) </summary>
  /// <param name="properties"> Dictionary of property names and values </param>
  public virtual void SetProperties(Dictionary<string, object?> properties) {
    var entityType = GetType();

    foreach (var property in properties) {
      var propertyInfo = entityType.GetProperty(property.Key);

      if (propertyInfo == null || !propertyInfo.CanWrite) continue;

      try {
        var value = property.Value;

        if (value != null && value.GetType() != propertyInfo.PropertyType) {
          var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
          value = Convert.ChangeType(value, targetType);
        }

        propertyInfo.SetValue(this, value);

        if (property.Key != nameof(CreatedAt)) { Touch(); }
      }
      catch (Exception) {
        // Silently ignore conversion errors
      }
    }
  }

  /// <summary> Gets a dictionary representation of the entity's current state </summary>
  /// <returns> Dictionary with property names and values </returns>
  public virtual Dictionary<string, object?> ToDictionary() {
    var result = new Dictionary<string, object?>();
    var properties = GetType().GetProperties();

    foreach (var property in properties) {
      if (property.CanRead) { result[property.Name] = property.GetValue(this); }
    }

    return result;
  }

  /// <summary> Override for better debugging and logging </summary>
  public override string ToString() {
    var deletedStatus = IsDeleted ? " (DELETED)" : "";

    return $"{GetType().Name} {{ Id = {Id}, Version = {Version}, CreatedAt = {CreatedAt:yyyy-MM-dd HH:mm:ss}, UpdatedAt = {UpdatedAt:yyyy-MM-dd HH:mm:ss}{deletedStatus} }}";
  }

  private void SetPropertiesFromObject(object partial) {
    var properties = partial.GetType().GetProperties();
    var entityType = GetType();

    foreach (var sourceProperty in properties) {
      var targetProperty = entityType.GetProperty(sourceProperty.Name);

      if (targetProperty == null || !targetProperty.CanWrite) continue;

      try {
        var value = sourceProperty.GetValue(partial);

        if (value != null && value.GetType() != targetProperty.PropertyType) {
          var targetType = Nullable.GetUnderlyingType(targetProperty.PropertyType) ?? targetProperty.PropertyType;
          value = Convert.ChangeType(value, targetType);
        }

        targetProperty.SetValue(this, value);
      }
      catch (Exception) {
        // Silently ignore conversion errors
      }
    }
  }
}

/// <summary> Base entity class that provides common properties and functionality for all domain entities. Uses Guid as the default identifier type. </summary>
public class EntityBase : EntityBase<Guid> {
  /// <summary> Default constructor </summary>
  protected EntityBase() {
    if (Id == Guid.Empty) { Id = Guid.NewGuid(); }
  }

  /// <summary> Constructor for partial initialization (useful for updates) </summary>
  /// <param name="partial"> Partial entity data to initialize with </param>
  protected EntityBase(object partial) : base(partial) {
    if (Id == Guid.Empty) { Id = Guid.NewGuid(); }
  }

  /// <summary> Static factory method to create an entity with initial properties </summary>
  /// <typeparam name="T"> The entity type </typeparam>
  /// <param name="partial"> Initial properties </param>
  /// <returns> New instance of the entity </returns>
  public static T Create<T>(object partial) where T : EntityBase, new() {
    var instance = new T();

    switch (partial) {
      case null: break;
      case Dictionary<string, object?> dict: instance.SetProperties(dict); break;

      default: {
          var properties = partial.GetType().GetProperties();
          var propDict = new Dictionary<string, object?>();

          foreach (var prop in properties) { propDict[prop.Name] = prop.GetValue(partial); }

          instance.SetProperties(propDict);

          break;
        }
    }

    return instance;
  }

  /// <summary> Static factory method to create an entity (parameterless) </summary>
  /// <typeparam name="T"> The entity type </typeparam>
  /// <returns> New instance of the entity </returns>
  public static T Create<T>() where T : EntityBase, new() { return new T(); }
}
