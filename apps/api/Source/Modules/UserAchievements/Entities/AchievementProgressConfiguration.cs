using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.UserAchievements.Entities;

internal class AchievementProgressConfiguration : IEntityTypeConfiguration<AchievementProgress> {
  public void Configure(EntityTypeBuilder<AchievementProgress> builder) {
    // Configure the relationship with User as optional to support soft delete global query filter
    builder.HasOne(ap => ap.User)
           .WithMany()
           .HasForeignKey(ap => ap.UserId)
           .IsRequired(false)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure the relationship with Achievement
    builder.HasOne(ap => ap.Achievement)
           .WithMany()
           .HasForeignKey(ap => ap.AchievementId)
           .IsRequired(true)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
