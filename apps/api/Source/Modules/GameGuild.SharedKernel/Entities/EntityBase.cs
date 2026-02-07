using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Entities;

/// <summary>
///     Generic base entity class that provides common properties and functionality for all domain entities.
///     Supports different ID types while maintaining the same base functionality.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier</typeparam>
public abstract class EntityBase<TKey> : IEntity<TKey>, ITenantScoped where TKey : IEquatable<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    ///     Default constructor
    /// </summary>
    protected EntityBase() { }

    /// <summary>
    ///     Constructor for partial initialization (useful for updates).
    ///     Uses a non-virtual initialization path to avoid virtual member calls in constructors.
    /// </summary>
    /// <param name="partial">Partial entity data to initialize with</param>
    protected EntityBase(object partial) : this() { InitializeFromPartial(partial); }

    public virtual bool IsGlobal { get => TenantId == null; }

    /// <summary>
    ///     Checks if this entity is newly created (not yet persisted to a database)
    /// </summary>
    public virtual bool IsNew { get => Version == 0; }

    /// <summary>
    ///     Checks if this entity is soft-deleted
    /// </summary>
    public virtual bool IsDeleted { get => DeletedAt.HasValue; }

    /// <summary> Domain events raised by this entity </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents { get => _domainEvents.AsReadOnly(); }

    /// <summary>
    ///     Unique identifier for the entity
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public virtual TKey Id { get; set; } = default!;

    /// <summary>
    ///     Version number for optimistic concurrency control
    ///     Uses ConcurrencyCheck for cross-database compatibility (Postgres, SQLite, SQL Server)
    /// </summary>
    [ConcurrencyCheck]
    public virtual int Version { get; set; } = 0;

    /// <summary>
    ///     Timestamp when the entity was created
    /// </summary>
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Timestamp when the entity was last updated
    /// </summary>
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public virtual DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Timestamp when the entity was soft-deleted (null if not deleted)
    /// </summary>
    public virtual DateTime? DeletedAt { get; set; }

    /// <summary>
    ///     Updates the UpdatedAt timestamp to the current UTC time
    /// </summary>
    public virtual void Touch() { UpdatedAt = DateTime.UtcNow; }

    /// <summary>
    ///     Soft-delete the entity by setting DeletedAt timestamp
    /// </summary>
    public virtual void SoftDelete()
    {
        if (IsDeleted) return;

        DeletedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     Restore a soft-deleted entity by clearing DeletedAt timestamp
    /// </summary>
    public virtual void Restore()
    {
        if (!IsDeleted) return;

        DeletedAt = null;
        Touch();
    }

    public virtual Guid? TenantId { get; protected set; }

    /// <summary>
    ///     Private non-virtual method to update timestamp during construction
    /// </summary>
    private void UpdateTimestamp() { UpdatedAt = DateTime.UtcNow; }

    /// <summary> Adds a domain event to the entity's event collection </summary>
    /// <param name="domainEvent"> The domain event to add </param>
    public void AddDomainEvent(IDomainEvent domainEvent) { _domainEvents.Add(domainEvent); }

    /// <summary> Removes a specific domain event from the entity's event collection </summary>
    /// <param name="domainEvent"> The domain event to remove </param>
    public void RemoveDomainEvent(IDomainEvent domainEvent) { _domainEvents.Remove(domainEvent); }

    /// <summary> Clears all domain events from the entity's event collection </summary>
    public void ClearDomainEvents() { _domainEvents.Clear(); }

    protected void Raise(IDomainEvent domainEvent) { _domainEvents.Add(domainEvent); }

    /// <summary>
    ///     Gets a dictionary representation of the entity's current state
    /// </summary>
    /// <returns>Dictionary with property names and values</returns>
    public virtual Dictionary<string, object?> ToDictionary()
    {
        var result = new Dictionary<string, object?>();
        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            if (property.CanRead) { result[property.Name] = property.GetValue(this); }
        }

        return result;
    }

    /// <summary>
    ///     Override for better debugging and logging
    /// </summary>
    public override string ToString() { return $"{GetType().Name} {{ Id = {Id}, Version = {Version}, CreatedAt = {CreatedAt:O}, UpdatedAt = {UpdatedAt:O}{(IsDeleted ? " (DELETED)" : "")} }}"; }

    protected virtual void ApplyPartial(object? partial)
    {
        InitializeFromPartial(partial);
    }

    /// <summary>
    ///     Non-virtual initialization method safe to call from constructors.
    ///     Converts the partial object to a property dictionary and applies via SetPropertiesInternal.
    /// </summary>
    private void InitializeFromPartial(object? partial)
    {
        if (partial is null) return;

        if (partial is Dictionary<string, object?> dictionary)
        {
            SetPropertiesInternal(dictionary, true);
            return;
        }

        var properties = partial.GetType().GetProperties();
        var map = new Dictionary<string, object?>(properties.Length, StringComparer.Ordinal);
        foreach (var property in properties) map[property.Name] = property.GetValue(partial);
        SetPropertiesInternal(map, true);
    }

    /// <summary>
    ///     Sets multiple properties from a dictionary (useful for partial updates)
    /// </summary>
    /// <param name="properties">Dictionary of property names and values</param>
    public virtual void SetProperties(Dictionary<string, object?> properties) { SetPropertiesInternal(properties, false); }

    /// <summary>
    ///     Internal method to set properties with option to use non-virtual timestamp update.
    /// </summary>
    /// <param name="properties">Dictionary of property names and values</param>
    /// <param name="isFromConstructor">True if called from constructor to avoid virtual method calls</param>
    /// <exception cref="InvalidOperationException">Thrown when a property value cannot be converted to the target type</exception>
    private void SetPropertiesInternal(Dictionary<string, object?> properties, bool isFromConstructor)
    {
        var entityType = GetType();

        foreach (var property in properties)
        {
            var propertyInfo = entityType.GetProperty(property.Key, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null || !propertyInfo.CanWrite) continue;

            var value = property.Value;

            if (value is null)
            {
                if (!IsNullableProperty(propertyInfo))
                    throw new InvalidOperationException(
                        $"Cannot set non-nullable property '{property.Key}' on {entityType.Name} to null.");

                propertyInfo.SetValue(this, null, null);
                continue; // was incorrectly 'return' — must continue to next property
            }

            var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            try
            {
                value = ConvertToTargetType(value, targetType);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                throw new InvalidOperationException(
                    $"Failed to convert value for property '{property.Key}' on {entityType.Name}. " +
                    $"Expected type '{targetType.Name}', got '{value.GetType().Name}' with value '{value}'.",
                    ex);
            }

            propertyInfo.SetValue(this, value);

            // Don't auto-update UpdatedAt for CreatedAt changes
            if (!string.Equals(property.Key, nameof(CreatedAt), StringComparison.Ordinal))
            {
                if (isFromConstructor)
                    UpdateTimestamp();
                else
                    Touch();
            }
        }
    }

    /// <summary>
    ///     Converts a value to the specified target type, handling common domain types.
    /// </summary>
    private static object ConvertToTargetType(object value, Type targetType)
    {
        // Guid conversion from string
        if (targetType == typeof(Guid) && value is string guidString)
        {
            if (!Guid.TryParse(guidString, out var guid))
                throw new FormatException($"'{guidString}' is not a valid GUID.");
            return guid;
        }

        // TenantId conversion
        if (targetType == typeof(TenantId) || targetType == typeof(TenantId?))
        {
            return value switch
            {
                string tenantIdString when Guid.TryParse(tenantIdString, out var parsedGuid) => new TenantId(parsedGuid),
                Guid tenantIdGuid => new TenantId(tenantIdGuid),
                TenantId tid => tid,
                _ => throw new InvalidCastException($"Cannot convert '{value.GetType().Name}' to TenantId.")
            };
        }

        // Same type — no conversion needed
        if (value.GetType() == targetType || targetType.IsAssignableFrom(value.GetType()))
            return value;

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    ///     Checks whether a property type is nullable (reference type or Nullable&lt;T&gt;).
    /// </summary>
    private static bool IsNullableProperty(PropertyInfo propertyInfo)
    {
        if (!propertyInfo.PropertyType.IsValueType)
            return true; // Reference types are nullable

        return Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null;
    }
}

/// <summary>
///     Base entity class that provides common properties and functionality for all domain entities.
/// </summary>
public class EntityBase : EntityBase<Guid>, IEntity
{
    /// <summary>
    ///     Default constructor — generates a new GUID if Id is empty.
    /// </summary>
    protected EntityBase()
    {
        EnsureIdGenerated();
    }

    /// <summary>
    ///     Constructor for partial initialization (useful for updates)
    /// </summary>
    /// <param name="partial">Partial entity data to initialize with</param>
    protected EntityBase(object partial) : base(partial)
    {
        EnsureIdGenerated();
    }

    /// <summary>
    ///     Non-virtual method to generate a GUID for new entities.
    ///     Safe to call from constructors.
    /// </summary>
    private void EnsureIdGenerated()
    {
        if (Id == Guid.Empty) Id = Guid.NewGuid();
    }

    /// <summary>
    ///     Static factory method to create an entity with initial properties
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="partial">Initial properties</param>
    /// <returns>New instance of the entity</returns>
    public static T Create<T>(object partial) where T : EntityBase, new()
    {
        // Create an instance and set properties
        var instance = new T();

        switch (partial)
        {
            case null: break;
            // Handle Dictionary<string, object?> case
            case Dictionary<string, object?> dict: instance.SetProperties(dict); break;

            default:
                {
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
    ///     Static factory method to create an entity (parameterless)
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <returns>New instance of the entity</returns>
    public static T Create<T>() where T : EntityBase, new() { return new T(); }
}
