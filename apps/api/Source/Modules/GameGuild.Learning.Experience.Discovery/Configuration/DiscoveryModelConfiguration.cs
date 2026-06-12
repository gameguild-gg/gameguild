using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Experience.Discovery;

/// <summary>
///     EF Core model configuration for discovery, featured content, and collections.
/// </summary>
public sealed class DiscoveryModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeaturedContent>(entity =>
        {
            entity.ToTable("learning_featured_content");
            entity.HasKey(content => content.Id);
            entity.Property(content => content.Title).HasMaxLength(300).IsRequired();
            entity.Property(content => content.Subtitle).HasMaxLength(500);
            entity.Property(content => content.ImageUrl).HasMaxLength(1000);
            entity.Property(content => content.LinkUrl).HasMaxLength(1000);
            entity.Property(content => content.TargetAudience).HasMaxLength(4000);
            entity.Property(content => content.Type).HasConversion<string>().HasMaxLength(60);
            entity.HasIndex(content => new { content.TenantId, content.IsActive, content.DisplayOrder });
            entity.HasIndex(content => content.CourseId);
            entity.HasIndex(content => content.LearningPathId);
            entity.HasIndex(content => content.Type);
        });

        modelBuilder.Entity<CourseCollection>(entity =>
        {
            entity.ToTable("learning_course_collections");
            entity.HasKey(collection => collection.Id);
            entity.Property(collection => collection.Title).HasMaxLength(300).IsRequired();
            entity.Property(collection => collection.Slug).HasMaxLength(220).IsRequired();
            entity.Property(collection => collection.Description).HasMaxLength(2000);
            entity.Property(collection => collection.ImageUrl).HasMaxLength(1000);
            entity.Property(collection => collection.Type).HasConversion<string>().HasMaxLength(60);
            entity.HasIndex(collection => new { collection.TenantId, collection.Slug }).IsUnique();
            entity.HasIndex(collection => new { collection.TenantId, collection.IsPublished, collection.IsFeatured });
            entity.HasIndex(collection => collection.CuratorId);
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.ToTable("learning_search_history");
            entity.HasKey(search => search.Id);
            entity.Property(search => search.Query).HasMaxLength(500).IsRequired();
            entity.Property(search => search.Filters).HasMaxLength(4000);
            entity.HasIndex(search => search.UserId);
            entity.HasIndex(search => search.TenantId);
            entity.HasIndex(search => search.Query);
            entity.HasIndex(search => search.ClickedCourseId);
        });
    }
}
