using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Entity Framework configuration for UserAchievement entity
/// </summary>
internal sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement> {
  public void Configure(EntityTypeBuilder<UserAchievement> builder) {
    ArgumentNullException.ThrowIfNull(builder);
    
    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(ua => ua.User)
           .WithMany()
           .HasForeignKey(ua => ua.UserId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure the relationship with Achievement
    // This explicitly maps to the UserAchievements collection on Achievement
    builder.HasOne(ua => ua.Achievement)
           .WithMany(a => a.UserAchievements)
           .HasForeignKey(ua => ua.AchievementId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
