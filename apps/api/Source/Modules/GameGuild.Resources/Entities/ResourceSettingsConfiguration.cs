using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     EF Core configuration for ResourceSettings entity
/// </summary>
public class ResourceSettingsConfiguration : IEntityTypeConfiguration<ResourceSettings>
{
    public void Configure(EntityTypeBuilder<ResourceSettings> builder)
    {
        builder.ToTable("resource_settings");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Category });
        builder.HasIndex(e => new { e.UserId, e.Key });

        builder.Property(e => e.Key).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Value).HasMaxLength(4000);
        builder.Property(e => e.DefaultValue).HasMaxLength(4000);
        builder.Property(e => e.DataType).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.ValidationRules).HasMaxLength(1000);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
