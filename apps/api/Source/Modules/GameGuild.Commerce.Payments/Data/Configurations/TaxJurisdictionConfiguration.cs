using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for TaxJurisdiction
/// </summary>
public class TaxJurisdictionConfiguration : IEntityTypeConfiguration<TaxJurisdiction>
{
    public void Configure(EntityTypeBuilder<TaxJurisdiction> builder)
    {
        // Configure table name
        builder.ToTable("tax_jurisdictions");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.TaxRegistrationNumber)
            .HasMaxLength(100);

        // Relationship configurations - self-referential hierarchy
        builder.HasOne(x => x.ParentJurisdiction)
            .WithMany(j => j.ChildJurisdictions)
            .HasForeignKey(x => x.ParentJurisdictionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes for performance
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_tax_jurisdictions_code");
        builder.HasIndex(x => x.Type).HasDatabaseName("ix_tax_jurisdictions_type");
        builder.HasIndex(x => x.ParentJurisdictionId).HasDatabaseName("ix_tax_jurisdictions_parent_id");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_tax_jurisdictions_is_active");
    }
}
