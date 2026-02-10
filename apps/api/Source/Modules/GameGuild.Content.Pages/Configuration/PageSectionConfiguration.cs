using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Content.Pages;

/// <summary>EF Core configuration for <see cref="PageSection"/>.</summary>
public class PageSectionConfiguration : IEntityTypeConfiguration<PageSection>
{
    public void Configure(EntityTypeBuilder<PageSection> builder)
    {
        builder.Property(s => s.SectionType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Heading).HasMaxLength(300);
        builder.Property(s => s.Subheading).HasMaxLength(500);
        builder.Property(s => s.CssClasses).HasMaxLength(500);

        builder.HasIndex(s => new { s.PageId, s.SortOrder });
    }
}
