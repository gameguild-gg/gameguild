using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Assets.Configuration;

/// <summary>
/// EF Core configuration for AssetReport entity.
/// </summary>
public class AssetReportConfiguration : IEntityTypeConfiguration<AssetReport>
{
    public void Configure(EntityTypeBuilder<AssetReport> builder)
    {
        builder.ToTable("AssetReports", "assets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AssetReferenceId)
            .IsRequired();

        builder.Property(e => e.ReportedByUserId)
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Decision)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ReviewedByUserId);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.ReviewedAt);

        // Relationships
        builder.HasOne(e => e.Reference)
            .WithMany()
            .HasForeignKey(e => e.AssetReferenceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.AssetReferenceId)
            .HasDatabaseName("IX_AssetReports_ReferenceId");

        builder.HasIndex(e => e.ReportedByUserId)
            .HasDatabaseName("IX_AssetReports_ReporterId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_AssetReports_Status");

        builder.HasIndex(e => new { e.AssetReferenceId, e.ReportedByUserId })
            .IsUnique()
            .HasDatabaseName("IX_AssetReports_Unique_UserReport");
    }
}
