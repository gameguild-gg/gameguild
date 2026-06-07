using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources.Contents.Configuration;

/// <summary>
/// EF Core configuration for ContentVersion entity
/// </summary>
public class ContentVersionEntityConfiguration : IEntityTypeConfiguration<ContentVersion>
{
    public void Configure(EntityTypeBuilder<ContentVersion> builder)
    {
        builder.ToTable("content_versions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Summary)
            .HasMaxLength(2000);

        builder.Property(e => e.Body)
            .HasColumnType("text");

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.ChangeNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Indexes
        builder.HasIndex(e => new { e.EntityId, e.EntityType });
        builder.HasIndex(e => new { e.EntityId, e.EntityType, e.VersionNumber });
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.ScheduledPublishAt);
    }
}

/// <summary>
/// EF Core configuration for ContentVersionReview entity
/// </summary>
public class ContentVersionReviewEntityConfiguration : IEntityTypeConfiguration<ContentVersionReview>
{
    public void Configure(EntityTypeBuilder<ContentVersionReview> builder)
    {
        builder.ToTable("content_version_reviews");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Feedback)
            .HasMaxLength(2000);

        builder.Property(e => e.Suggestions)
            .HasColumnType("jsonb");

        builder.Property(e => e.Decision)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Global query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Indexes
        builder.HasIndex(e => e.ContentVersionId);
        builder.HasIndex(e => e.ReviewerId);
    }
}

/// <summary>
/// EF Core configuration for DocumentTemplate entity
/// </summary>
public class DocumentTemplateEntityConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.ToTable("document_templates");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TemplateKey)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Category)
            .HasMaxLength(120);

        builder.Property(e => e.SupportedEntityType)
            .HasMaxLength(120);

        builder.Property(e => e.PlaceholderSchema)
            .HasColumnType("jsonb");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.TemplateKey)
            .IsUnique();

        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.SupportedEntityType);
    }
}
