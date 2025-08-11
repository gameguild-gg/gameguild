using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Reputations;

internal sealed class UserReputationConfiguration : IEntityTypeConfiguration<UserReputation> {
  public void Configure(EntityTypeBuilder<UserReputation> builder) {
    ArgumentNullException.ThrowIfNull(builder);
    
    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(ur => ur.User)
           .WithMany()
           .HasForeignKey(ur => ur.UserId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with CurrentLevel (can't be done with annotations)
    builder.HasOne(ur => ur.CurrentLevel)
           .WithMany()
           .HasForeignKey(ur => ur.CurrentLevelId)
           .IsRequired(false) // Make consistent with other optional relationships
           .OnDelete(DeleteBehavior.SetNull);

    // Filtered unique constraint (can't be done with annotations)
    builder.HasIndex(ur => ur.UserId).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
  }
}
