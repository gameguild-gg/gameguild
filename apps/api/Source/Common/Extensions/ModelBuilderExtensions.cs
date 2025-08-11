using System.Linq.Expressions;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Common;

/// <summary>
/// Extension methods for configuring base entity properties in Entity Framework
/// </summary>
public static class ModelBuilderExtensions {
  /// <summary>
  /// Configures all entities that inherit from BaseEntity with common configurations
  /// </summary>
  /// <param name="modelBuilder">The model builder</param>
  public static void ConfigureBaseEntities(this ModelBuilder modelBuilder) {
    // Find all entity types that inherit from BaseEntity or BaseEntity<T>
    var entityTypes = modelBuilder.Model.GetEntityTypes().Where(t => t.ClrType != null && IsBaseEntity(t.ClrType));

    foreach (var entityType in entityTypes) {
      // Skip abstract types entirely - they should not be included in the model configuration
      var isAbstractType = entityType.ClrType.IsAbstract;
      if (isAbstractType) continue;

      // Configure common properties
      modelBuilder.Entity(
        entityType.ClrType,
        builder => {
          // In TPC inheritance, each concrete type gets its own complete table
          // We need to configure base properties for all concrete types, not just root types
          var isTpcInheritanceType = IsTpcInheritanceEntity(entityType.ClrType);

          // Skip key configuration for TPC inheritance entities - EF handles it automatically
          if (!isTpcInheritanceType) {
            // Id configuration (UUID) - for entities not using TPC inheritance
            builder.HasKey(nameof(Entity.Id));

            builder.Property(nameof(Entity.Id))
                   .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL UUID generation
                   .ValueGeneratedOnAdd();
          }

          // Version configuration for optimistic concurrency
          // Use ConcurrencyCheck instead of IsRowVersion for cross-database compatibility
          // Database default ensures new entities start with Version = 1
          builder.Property(nameof(Entity.Version))
                 .IsConcurrencyToken()
                 .HasDefaultValue(1)
                 .ValueGeneratedOnAdd();

          // Timestamp and soft delete configuration - for all concrete types in TPC
          builder.Property(nameof(Entity.CreatedAt))
                 .IsRequired()
                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
                 .ValueGeneratedOnAdd();

          builder.Property(nameof(Entity.UpdatedAt))
                 .IsRequired()
                 .HasDefaultValueSql("CURRENT_TIMESTAMP")
                 .ValueGeneratedOnAddOrUpdate();

          builder.Property(nameof(Entity.DeletedAt)).IsRequired(false);

          // Add indexes for performance - for all concrete types in TPC
          builder.HasIndex(nameof(Entity.CreatedAt));
          builder.HasIndex(nameof(Entity.DeletedAt));
        }
      );
    }
  }

  /// <summary>
  /// Configures global query filters for soft delete
  /// </summary>
  /// <param name="modelBuilder">The model builder</param>
  public static void ConfigureSoftDelete(this ModelBuilder modelBuilder) {
    // Find all entity types that inherit from BaseEntity or BaseEntity<T>
    var entityTypes = modelBuilder.Model.GetEntityTypes().Where(t => t.ClrType != null && IsBaseEntity(t.ClrType));

    foreach (var entityType in entityTypes) {
      // Skip abstract types - they don't get tables in TPC inheritance
      var isAbstractType = entityType.ClrType.IsAbstract;

      if (isAbstractType) continue;

      // Skip types that are part of TPC inheritance hierarchies
      // These are handled differently by EF Core and would cause conflicts
      if (IsTpcInheritanceEntity(entityType.ClrType)) continue;

      // Add global query filter to exclude soft-deleted entities for concrete types
      // that are NOT part of TPC inheritance hierarchies
      var parameter = Expression.Parameter(entityType.ClrType, "e");
      var deletedAtProperty = Expression.Property(parameter, nameof(Entity.DeletedAt));
      var condition = Expression.Equal(deletedAtProperty, Expression.Constant(null, typeof(DateTime?)));
      var lambda = Expression.Lambda(condition, parameter);

      modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
    }
  }

  /// <summary>
  /// Checks if a type inherits from BaseEntity or BaseEntity&lt;T&gt;
  /// </summary>
  private static bool IsBaseEntity(Type type) {
    if (type == null) return false;

    // Check for direct inheritance from BaseEntity
    if (typeof(Entity).IsAssignableFrom(type)) return true;

    // Check for inheritance from BaseEntity<T>
    var baseType = type.BaseType;

    while (baseType != null) {
      if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Entity<>)) return true;

      baseType = baseType.BaseType;
    }

    return false;
  }

  /// <summary>
  /// Checks if a type is part of a TPC inheritance hierarchy
  /// In this case, we check if it inherits from ResourceBase which uses TPC mapping strategy
  /// </summary>
  private static bool IsTpcInheritanceEntity(Type type) {
    if (type == null) return false;

    // Check if the type inherits from ResourceBase (which uses TPC inheritance)
    return typeof(Resource).IsAssignableFrom(type);
  }

  /// <summary>
  /// Sets up automatic UpdatedAt timestamp updates
  /// </summary>
  /// <param name="modelBuilder">The model builder</param>
  public static void ConfigureTimestamps(this ModelBuilder modelBuilder) {
    // This is handled by overriding SaveChanges in the DbContext
    // The configuration here is just for the database schema
  }
}
