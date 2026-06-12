using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
///     EF Core model configuration for recommendations and learner preference profiles.
/// </summary>
public sealed class RecommendationsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseRecommendation>(entity =>
        {
            entity.ToTable("learning_course_recommendations");
            entity.HasKey(recommendation => recommendation.Id);
            entity.Property(recommendation => recommendation.Type).HasConversion<string>().HasMaxLength(60);
            entity.Property(recommendation => recommendation.Reason).HasMaxLength(1000);
            entity.HasIndex(recommendation => new { recommendation.UserId, recommendation.IsDismissed, recommendation.ExpiresAt });
            entity.HasIndex(recommendation => new { recommendation.UserId, recommendation.CourseId });
            entity.HasIndex(recommendation => recommendation.CourseId);
            entity.HasIndex(recommendation => recommendation.Type);
        });

        modelBuilder.Entity<UserLearningProfile>(entity =>
        {
            entity.ToTable("learning_user_profiles");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.PreferredCategories).HasMaxLength(4000);
            entity.Property(profile => profile.PreferredDifficulty).HasMaxLength(80);
            entity.Property(profile => profile.PreferredDuration).HasMaxLength(80);
            entity.Property(profile => profile.LearningGoals).HasMaxLength(4000);
            entity.Property(profile => profile.Skills).HasMaxLength(4000);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => profile.LastActivityAt);
        });
    }
}
