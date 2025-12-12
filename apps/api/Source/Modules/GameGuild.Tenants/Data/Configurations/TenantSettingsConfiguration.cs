using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Data.Configurations;

/// <summary>
///     Entity Type Configuration for TenantSettings
/// </summary>
public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure TenantId as required (override nullable from base)
        builder.Property(x => x.TenantId)
            .IsRequired();

        // Configure relationship to Tenant (one-to-one)
        builder.HasOne(x => x.Tenant)
            .WithOne()
            .HasForeignKey<TenantSettings>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure string properties
        builder.Property(x => x.DefaultLanguage)
            .HasMaxLength(10);

        builder.Property(x => x.DefaultTimezone)
            .HasMaxLength(50);

        builder.Property(x => x.DefaultCurrency)
            .HasMaxLength(3);

        // Configure index on TenantId
        builder.HasIndex(x => x.TenantId)
            .IsUnique();
    }
}
