using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace GameGuild.Database;

/// <summary>
/// Custom history repository that uses snake_case naming for PostgreSQL migrations history table
/// Ensures consistent snake_case naming for all database objects including keys, indexes, and constraints
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage
public class SnakeCaseHistoryRepository(HistoryRepositoryDependencies dependencies) : NpgsqlHistoryRepository(dependencies)
{
    protected override void ConfigureTable(EntityTypeBuilder<HistoryRow> history)
    {
        base.ConfigureTable(history);

        // Override table name to use snake_case
        _ = history.ToTable("migrations_history");

        // Override column names to use snake_case
        _ = history.Property(h => h.MigrationId).HasColumnName("migration_id");
        _ = history.Property(h => h.ProductVersion).HasColumnName("product_version");

        // Apply comprehensive snake_case transformation to all database objects
        // This ensures consistency with the main ApplicationDbContext transformation
        ICaseTransformer snakeTransformer = CaseTransformerFactory.Snake;
        IMutableEntityType entityType = history.Metadata;

        // Transform primary key name to snake_case
        IMutableKey? primaryKey = entityType.FindPrimaryKey();

        if (primaryKey != null)
        {
            string? primaryKeyName = primaryKey.GetName();

            if (!string.IsNullOrEmpty(primaryKeyName)) { primaryKey.SetName(snakeTransformer.Transform(primaryKeyName)); }
        }

        // Transform index names to snake_case
        foreach (IMutableIndex index in entityType.GetIndexes())
        {
            string? indexName = index.GetDatabaseName();

            if (!string.IsNullOrEmpty(indexName)) { index.SetDatabaseName(snakeTransformer.Transform(indexName)); }
        }

        // Transform foreign key names to snake_case (if any)
        foreach (IMutableForeignKey foreignKey in entityType.GetForeignKeys())
        {
            string? foreignKeyName = foreignKey.GetConstraintName();

            if (!string.IsNullOrEmpty(foreignKeyName)) { foreignKey.SetConstraintName(snakeTransformer.Transform(foreignKeyName)); }
        }
    }


}
#pragma warning restore EF1001
