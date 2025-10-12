namespace GameGuild.Modules.Reputations.Entities;

internal sealed class UserReputationConfiguration : IEntityTypeConfiguration<UserReputation>
{
    public void Configure(EntityTypeBuilder<UserReputation> builder)
    {
        builder.HasOne(ur => ur.User).WithOne().HasForeignKey<UserReputation>(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.CurrentLevel).WithMany(rt => rt.UserReputations).HasForeignKey(ur => ur.CurrentLevelId).OnDelete(DeleteBehavior.SetNull);

        // Additional indexes
        builder.HasIndex(ur => ur.LastUpdated);
        builder.HasIndex(ur => new { ur.Score, ur.CurrentLevelId });
    }
}
