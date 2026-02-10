using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Content.Pages;

/// <summary>EF Core configuration for <see cref="ContentResource"/>.</summary>
public class ContentResourceConfiguration : IEntityTypeConfiguration<ContentResource>
{
    public void Configure(EntityTypeBuilder<ContentResource> builder)
    {
        builder.HasIndex(r => r.Slug).IsUnique();

        builder.Property(r => r.Slug).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Summary).HasMaxLength(2000);

        builder.Property(r => r.ResourceType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.CategorySlug).HasMaxLength(200);
        builder.Property(r => r.Tags).HasMaxLength(1000);
        builder.Property(r => r.AuthorName).HasMaxLength(200);
        builder.Property(r => r.LinkedEntityType).HasMaxLength(100);
    }
}
