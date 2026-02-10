using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Content.Pages;

/// <summary>EF Core configuration for <see cref="Page"/>.</summary>
public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Slug).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.Property(p => p.PageType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Self-referencing hierarchy
        builder.HasOne(p => p.ParentPage)
            .WithMany(p => p.ChildPages)
            .HasForeignKey(p => p.ParentPageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sections
        builder.HasMany(p => p.Sections)
            .WithOne(s => s.Page)
            .HasForeignKey(s => s.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
