using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for DisputeEvidence.
///     Configures the database schema for dispute evidence records.
/// </summary>
public class DisputeEvidenceConfiguration : IEntityTypeConfiguration<DisputeEvidence>
{
    public void Configure(EntityTypeBuilder<DisputeEvidence> builder)
    {
        // Configure table name (snake_case convention for PostgreSQL)
        builder.ToTable("dispute_evidence", "gameguild.payments");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        // Configure required properties
        builder.Property(x => x.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired();

        builder.Property(x => x.EvidenceType)
            .HasColumnName("evidence_type")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(x => x.FileUrl)
            .HasColumnName("file_url")
            .HasMaxLength(1000);

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255);

        builder.Property(x => x.FileSize)
            .HasColumnName("file_size");

        builder.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(100);

        builder.Property(x => x.SubmittedAt)
            .HasColumnName("submitted_at")
            .IsRequired();

        builder.Property(x => x.SubmittedBy)
            .HasColumnName("submitted_by")
            .IsRequired();

        builder.Property(x => x.IsFromMerchant)
            .HasColumnName("is_from_merchant")
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasMaxLength(2000);

        // Configure relationship to PaymentDispute
        builder.HasOne(x => x.Dispute)
            .WithMany(d => d.Evidence)
            .HasForeignKey(x => x.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for common query patterns
        builder.HasIndex(x => x.DisputeId)
            .HasDatabaseName("idx_dispute_evidence_dispute_id");

        builder.HasIndex(x => x.EvidenceType)
            .HasDatabaseName("idx_dispute_evidence_type");

        builder.HasIndex(x => x.SubmittedAt)
            .HasDatabaseName("idx_dispute_evidence_submitted_at");

        builder.HasIndex(x => x.IsFromMerchant)
            .HasDatabaseName("idx_dispute_evidence_is_from_merchant");

        // Configure audit timestamps from EntityBase
        builder.Property("CreatedAt")
            .HasColumnName("created_at")
            .IsRequired();
        
        builder.Property("UpdatedAt")
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
