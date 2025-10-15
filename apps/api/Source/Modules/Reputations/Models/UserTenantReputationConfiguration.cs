namespace GameGuild.Modules.Reputations;

internal sealed class UserTenantReputationConfiguration : IEntityTypeConfiguration<UserTenantReputation> {
  public void Configure(EntityTypeBuilder<UserTenantReputation> builder) {
    ArgumentNullException.ThrowIfNull(builder);

    // Configure the relationship with TenantPermission as optional to avoid query filter warnings
    builder.HasOne(utr => utr.TenantPermission)
           .WithMany()
           .HasForeignKey(utr => utr.TenantPermissionId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with CurrentLevel (can't be done with annotations)
    builder.HasOne(utr => utr.CurrentLevel)
           .WithMany()
           .HasForeignKey(utr => utr.CurrentLevelId)
           .IsRequired(false)
           .OnDelete(DeleteBehavior.SetNull);
  }
}
