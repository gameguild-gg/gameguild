using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Entity configuration for Achievement
/// </summary>
public class AchievementConfiguration : IEntityTypeConfiguration<Achievement> {
  public void Configure(EntityTypeBuilder<Achievement> builder) {
    // Configure the Prerequisites relationship
    builder.HasMany(a => a.Prerequisites)
           .WithOne(ap => ap.Achievement)
           .HasForeignKey(ap => ap.AchievementId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}

/// <summary>
/// Entity configuration for AchievementPrerequisite
/// </summary>
public class AchievementPrerequisiteConfiguration : IEntityTypeConfiguration<AchievementPrerequisite> {
  public void Configure(EntityTypeBuilder<AchievementPrerequisite> builder) {
    // Configure the PrerequisiteAchievement relationship
    builder.HasOne(ap => ap.PrerequisiteAchievement)
           .WithMany() // No back navigation from Achievement to Prerequisites as PrerequisiteAchievement
           .HasForeignKey(ap => ap.PrerequisiteAchievementId)
           .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete of prerequisite achievements
  }
}

/// <summary>
/// Entity configuration for AchievementLevel
/// </summary>
public class AchievementLevelConfiguration : IEntityTypeConfiguration<AchievementLevel> {
  public void Configure(EntityTypeBuilder<AchievementLevel> builder) {
    // Configure the Achievement relationship
    builder.HasOne(al => al.Achievement)
           .WithMany(a => a.Levels)
           .HasForeignKey(al => al.AchievementId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}

/// <summary>
/// Entity configuration for UserAchievement
/// </summary>
public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement> {
  public void Configure(EntityTypeBuilder<UserAchievement> builder) {
    // Configure the Achievement relationship
    builder.HasOne(ua => ua.Achievement)
           .WithMany(a => a.UserAchievements)
           .HasForeignKey(ua => ua.AchievementId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}

/// <summary>
/// Entity configuration for AchievementProgress
/// </summary>
public class AchievementProgressConfiguration : IEntityTypeConfiguration<AchievementProgress> {
  public void Configure(EntityTypeBuilder<AchievementProgress> builder) {
    // Configure unique constraint for user and achievement combination
    builder.HasIndex(ap => new { ap.UserId, ap.AchievementId })
           .IsUnique();
  }
}
