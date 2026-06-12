using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
///     EF Core model configuration for curated learning paths and learner path progress.
/// </summary>
public sealed class LearningPathsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningPath>(entity =>
        {
            entity.ToTable("learning_paths");
            entity.HasKey(path => path.Id);
            entity.Property(path => path.Title).HasMaxLength(300).IsRequired();
            entity.Property(path => path.Slug).HasMaxLength(220).IsRequired();
            entity.Property(path => path.Description).HasMaxLength(4000);
            entity.Property(path => path.ImageUrl).HasMaxLength(1000);
            entity.Property(path => path.Difficulty).HasConversion<string>().HasMaxLength(40);
            entity.HasMany(path => path.Courses)
                .WithOne()
                .HasForeignKey(course => course.LearningPathId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(path => path.Courses).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(path => new { path.TenantId, path.Slug }).IsUnique();
            entity.HasIndex(path => new { path.TenantId, path.IsPublished, path.IsFeatured });
            entity.HasIndex(path => path.CreatorId);
        });

        modelBuilder.Entity<LearningPathCourse>(entity =>
        {
            entity.ToTable("learning_path_courses");
            entity.HasKey(course => new { course.LearningPathId, course.CourseId });
            entity.Property(course => course.Order).HasColumnName("SortOrder");
            entity.HasIndex(course => new { course.LearningPathId, course.Order }).IsUnique();
            entity.HasIndex(course => course.CourseId);
        });

        modelBuilder.Entity<LearningPathEnrollment>(entity =>
        {
            entity.ToTable("learning_path_enrollments");
            entity.HasKey(enrollment => enrollment.Id);
            entity.Property(enrollment => enrollment.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(enrollment => new { enrollment.LearningPathId, enrollment.UserId }).IsUnique();
            entity.HasIndex(enrollment => enrollment.UserId);
            entity.HasIndex(enrollment => enrollment.Status);
        });
    }
}
