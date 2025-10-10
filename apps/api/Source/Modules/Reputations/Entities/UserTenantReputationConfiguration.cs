using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Reputations.Entities;

internal sealed class UserTenantReputationConfiguration : IEntityTypeConfiguration<UserTenantReputation>
{
    public void Configure(EntityTypeBuilder<UserTenantReputation> builder)
    {
        builder.HasOne(utr => utr.TenantPermission).WithOne().HasForeignKey<UserTenantReputation>(utr => utr.TenantPermissionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(utr => utr.CurrentLevel).WithMany().HasForeignKey(utr => utr.CurrentLevelId).OnDelete(DeleteBehavior.SetNull);

        // Additional indexes
        builder.HasIndex(utr => utr.LastUpdated);
        builder.HasIndex(utr => new { utr.Score, utr.CurrentLevelId });
    }
}
