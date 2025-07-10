using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Reputations.Models;

public class UserReputationConfiguration : IEntityTypeConfiguration<UserReputation> {
  public void Configure(EntityTypeBuilder<UserReputation> builder) {
    // Configure a relationship with User (can't be done with annotations)
    builder.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure a relationship with CurrentLevel (can't be done with annotations)
    builder.HasOne(ur => ur.CurrentLevel)
           .WithMany()
           .HasForeignKey(ur => ur.CurrentLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Filtered unique constraint (can't be done with annotations)
    builder.HasIndex(ur => ur.UserId).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
  }
}
