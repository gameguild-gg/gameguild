namespace GameGuild.Modules.Tenants;

/// <summary>
/// Entity Framework configuration for RoleTemplate entity
/// </summary>
public class RoleTemplateConfiguration : IEntityTypeConfiguration<RoleTemplate>
{
    public void Configure(EntityTypeBuilder<RoleTemplate> builder)
    {
        builder.ToTable("RoleTemplates");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rt => rt.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(rt => rt.Description)
            .HasMaxLength(500);

        builder.Property(rt => rt.Category)
            .HasMaxLength(50);

        builder.Property(rt => rt.IsSystemTemplate)
            .HasDefaultValue(false);

        builder.Property(rt => rt.IsActive)
            .HasDefaultValue(true);

        builder.Property(rt => rt.Priority)
            .HasDefaultValue(0);

        builder.Property(rt => rt.CanBeAssignedByTenantAdmin)
            .HasDefaultValue(true);

        // Configure Permissions as JSON column
        builder.Property(rt => rt.Permissions)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<PermissionType[]>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? Array.Empty<PermissionType>()
            )
            .HasColumnType("jsonb");

        // Configure Metadata as JSON column
        builder.Property(rt => rt.Metadata)
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null)
            )
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(rt => rt.Name)
            .IsUnique()
            .HasDatabaseName("IX_RoleTemplates_Name");

        builder.HasIndex(rt => rt.IsSystemTemplate)
            .HasDatabaseName("IX_RoleTemplates_IsSystemTemplate");

        builder.HasIndex(rt => rt.Category)
            .HasDatabaseName("IX_RoleTemplates_Category");

        builder.HasIndex(rt => rt.IsActive)
            .HasDatabaseName("IX_RoleTemplates_IsActive");

        // Relationships
        builder.HasMany(rt => rt.TenantApplications)
            .WithOne(tra => tra.RoleTemplate)
            .HasForeignKey(tra => tra.RoleTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete query filter
        builder.HasQueryFilter(rt => rt.DeletedAt == null);
    }
}
