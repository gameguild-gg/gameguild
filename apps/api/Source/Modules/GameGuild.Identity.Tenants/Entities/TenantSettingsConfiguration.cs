using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Entity Framework configuration for TenantSettings entity
/// </summary>
public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Primary Key
        builder.HasKey(ts => ts.Id);

        // Properties
        builder.Property(ts => ts.TenantId).IsRequired();
        builder.Property(ts => ts.DefaultLanguage).HasMaxLength(10);
        builder.Property(ts => ts.DefaultTimezone).HasMaxLength(50);
        builder.Property(ts => ts.DefaultCurrency).HasMaxLength(3);
        builder.Property(ts => ts.AllowUserRegistration).IsRequired();
        builder.Property(ts => ts.RequireRegistrationApproval).IsRequired();
        builder.Property(ts => ts.RequireTwoFactorAuth).IsRequired();
        builder.Property(ts => ts.MaxUsers).IsRequired(false);
        builder.Property(ts => ts.StorageQuota).IsRequired(false);
        builder.Property(ts => ts.EnableAuditLogging).IsRequired();
        builder.Property(ts => ts.EnableApiAccess).IsRequired();
        builder.Property(ts => ts.BrandingSettings).IsRequired(false);
        builder.Property(ts => ts.NotificationSettings).IsRequired(false);
        builder.Property(ts => ts.SecuritySettings).IsRequired(false);
        builder.Property(ts => ts.IntegrationSettingsJson).IsRequired(false);

        // Soft delete query filter
        builder.HasQueryFilter(ts => ts.DeletedAt == null);

        // Relationships
        builder.HasOne(ts => ts.Tenant).WithOne(t => t.TenantSettings).HasForeignKey<TenantSettings>(ts => ts.TenantId).OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ts => ts.TenantId).IsUnique();
    }
}
