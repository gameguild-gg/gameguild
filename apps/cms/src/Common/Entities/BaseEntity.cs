using System.Reflection;
using HotChocolate.Types;

namespace GameGuild.Common.Entities;

/// <summary>
/// Base entity class that provides common properties and functionality for all domain entities.
/// Mirrors the functionality of NestJS EntityBase with UUID primary keys, version control, and soft delete.
/// Uses Guid as the default ID type to match the NestJS implementation.
/// </summary>
public class BaseEntity : BaseEntity<Guid>
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    [GraphQLType(typeof(NonNullType<UuidType>))]
    [GraphQLDescription("The unique identifier for the entity (UUID).")]
    public override Guid Id { get; set; }

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
    public override DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was last updated
    /// </summary>
    [GraphQLType(typeof(NonNullType<DateTimeType>))]
    [GraphQLDescription("The date and time when the entity was last updated.")]
    public override DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entity was soft-deleted (null if not deleted)
    /// </summary>
    [GraphQLType(typeof(DateTimeType))]
    [GraphQLDescription("The date and time when the entity was soft deleted (null if not deleted).")]
    public override DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Whether the entity is soft-deleted
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Indicates whether the entity has been soft deleted.")]
    public override bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// The tenant this entity belongs to (null if global)
    /// </summary>
    [GraphQLType(typeof(GameGuild.Modules.Tenant.GraphQL.TenantType))]
    [GraphQLDescription("The tenant this entity belongs to (null if global).")]
    public override Modules.Tenant.Models.Tenant? Tenant { get; set; }

    /// <summary>
    /// Default constructor
    /// </summary>
    protected BaseEntity()
    {
        // Generate new GUID for new entities
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Constructor for partial initialization (useful for updates)
    /// Mirrors the NestJS EntityDto constructor: constructor(partial: Partial<typeof this>)
    /// </summary>
    /// <param name="partial">Partial entity data to initialize with</param>
    protected BaseEntity(object partial) : base(partial)
    {
        // Generate new GUID for new entities if not provided in partial
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Static factory method to create an entity with initial properties
    /// Mirrors the NestJS EntityDto.create() method
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="partial">Initial properties</param>
    /// <returns>New instance of the entity</returns>
    public static T Create<T>(object partial) where T : BaseEntity, new()
    {
        // Create instance and set properties
        var instance = new T();

        if (partial != null)
        {
            // Handle Dictionary<string, object?> case
            if (partial is Dictionary<string, object?> dict)
            {
                instance.SetProperties(dict);
            }
            else
            {
                // Handle anonymous object case
                var properties = partial.GetType().GetProperties();
                var propDict = new Dictionary<string, object?>();

                foreach (PropertyInfo prop in properties)
                {
                    propDict[prop.Name] = prop.GetValue(partial);
                }

                instance.SetProperties(propDict);
            }
        }

        return instance;
    }

    /// <summary>
    /// Static factory method to create an entity (parameterless)
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <returns>New instance of the entity</returns>
    public static T Create<T>() where T : BaseEntity, new()
    {
        return new T();
    }
}
