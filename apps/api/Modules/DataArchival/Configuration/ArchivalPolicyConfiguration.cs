using GameGuild.Modules.DataArchival.Entities;


namespace GameGuild.Modules.DataArchival.Configuration;

/// <summary>
/// Entity Framework configuration for ArchivalPolicy.
/// </summary>
public class ArchivalPolicyConfiguration : IEntityTypeConfiguration<ArchivalPolicy>
{
    public void Configure(EntityTypeBuilder<ArchivalPolicy> builder)
    {
        builder.ToTable("ArchivalPolicies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.RetentionDays)
            .IsRequired();

        builder.Property(p => p.ArchiveAfterDays)
            .IsRequired();

        builder.Property(p => p.DeleteAfterDays)
            .IsRequired(false);

        builder.Property(p => p.StorageTier)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Hot");

        builder.Property(p => p.CompressionEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.EncryptionEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.LastExecutedAt)
            .IsRequired(false);

        builder.Property(p => p.ExecutionCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.EntityType);
        builder.HasIndex(p => new { p.TenantId, p.EntityType });
        builder.HasIndex(p => p.IsEnabled);
    }
}
