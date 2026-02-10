using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Learning.Courses;

/// <summary>
/// EF Core configuration for CoursePrerequisite entity
/// </summary>
public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.ToTable("course_prerequisites");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.CourseId)
            .IsRequired();

        builder.Property(cp => cp.PrerequisiteCourseId)
            .IsRequired();

        builder.Property(cp => cp.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(cp => cp.MinimumGrade);

        builder.Property(cp => cp.Description)
            .HasMaxLength(500);

        builder.Property(cp => cp.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(cp => cp.PrerequisiteGroup)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(cp => cp.Course)
            .WithMany()
            .HasForeignKey(cp => cp.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.PrerequisiteCourse)
            .WithMany()
            .HasForeignKey(cp => cp.PrerequisiteCourseId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Indexes
        builder.HasIndex(cp => new { cp.CourseId, cp.PrerequisiteCourseId })
            .IsUnique();

        builder.HasIndex(cp => cp.CourseId);
        builder.HasIndex(cp => cp.PrerequisiteCourseId);
        builder.HasIndex(cp => cp.TenantId);

        // Global query filter for soft delete
        builder.HasQueryFilter(cp => cp.DeletedAt == null);
    }
}
