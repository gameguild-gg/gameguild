using System.Reflection;
using GameGuild.Modules.Tenant.Models;

namespace GameGuild.Common.Entities;

/// <summary>
/// Base entity class that provides common properties and functionality for all domain entities.
/// </summary>
public class BaseEntity : BaseEntity<Guid> {
  /// <summary>
  /// Unique identifier for the entity
  /// </summary>
  [GraphQLType(typeof(NonNullType<UuidType>)), GraphQLDescription("The unique identifier for the entity (UUID).")]
  public override sealed Guid Id { get; set; }

  /// <summary>
  /// Version number for optimistic concurrency control
  /// </summary>
  [GraphQLType(typeof(NonNullType<IntType>)), GraphQLDescription("Version number for optimistic concurrency control.")]
  public override int Version { get; set; }

  /// <summary>
  /// Timestamp when the entity was created
  /// </summary>
  [GraphQLType(typeof(NonNullType<DateTimeType>)), GraphQLDescription("The date and time when the entity was created.")]
  public override sealed DateTime CreatedAt { get; set; }

  /// <summary>
  /// Timestamp when the entity was last updated
  /// </summary>
  [GraphQLType(typeof(NonNullType<DateTimeType>)), GraphQLDescription("The date and time when the entity was last updated.")]
  public override sealed DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Timestamp when the entity was soft-deleted (null if not deleted)
  /// </summary>
  [GraphQLType(typeof(DateTimeType)), GraphQLDescription("The date and time when the entity was soft deleted (null if not deleted).")]
  public override sealed DateTime? DeletedAt { get; set; }

  /// <summary>
  /// Whether the entity is soft-deleted
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>)), GraphQLDescription("Indicates whether the entity has been soft deleted.")]
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
  protected BaseEntity() {
    // Generate new GUID for new entities
    if (Id == Guid.Empty) { Id = Guid.NewGuid(); }
  }

  /// <summary>
  /// Constructor for partial initialization (useful for updates)
  /// </summary>
  /// <param name="partial">Partial entity data to initialize with</param>
  protected BaseEntity(object partial) : base(partial) {
    // Generate new GUID for new entities if not provided in partial
    if (Id == Guid.Empty) { Id = Guid.NewGuid(); }
  }

  /// <summary>
  /// Static factory method to create an entity with initial properties
  /// </summary>
  /// <typeparam name="T">The entity type</typeparam>
  /// <param name="partial">Initial properties</param>
  /// <returns>New instance of the entity</returns>
  public static T Create<T>(object partial) where T : BaseEntity, new() {
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

        foreach (PropertyInfo prop in properties) { propDict[prop.Name] = prop.GetValue(partial); }

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
  public static T Create<T>() where T : BaseEntity, new() { return new T(); }
}
