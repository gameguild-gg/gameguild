using GameGuild.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Data.Configurations;

/// <summary>
///     Entity Type Configuration for TenantDomain
/// </summary>
public class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure TenantId as required (override nullable from base)
        builder.Property(x => x.TenantId)
            .IsRequired();

        // Configure relationship to Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure string properties
        builder.Property(x => x.TopLevelDomain)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Subdomain)
            .HasMaxLength(100);

        // Configure indexes
        builder.HasIndex(x => new { x.TopLevelDomain, x.Subdomain })
            .IsUnique();
        
        builder.HasIndex(x => new { x.TenantId, x.IsMainDomain });
    }
}
