using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Reputations;

public class UserTenantReputationConfiguration : IEntityTypeConfiguration<UserTenantReputation> {
  public void Configure(EntityTypeBuilder<UserTenantReputation> builder) {
    // Configure a relationship with UserTenant (can't be done with annotations)
    builder.HasOne(utr => utr.TenantPermission)
           .WithMany()
           .HasForeignKey(utr => utr.TenantPermissionId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure a relationship with CurrentLevel (can't be done with annotations)
    builder.HasOne(utr => utr.CurrentLevel)
           .WithMany()
           .HasForeignKey(utr => utr.CurrentLevelId)
           .OnDelete(DeleteBehavior.SetNull);
  }
}
