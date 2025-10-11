namespace GameGuild.Modules.Reputations.Entities;

public class ReputationActionConfiguration : IEntityTypeConfiguration<ReputationAction>
{
    public void Configure(EntityTypeBuilder<ReputationAction> builder)
    {
        builder.HasOne(ra => ra.RequiredLevel).WithMany().HasForeignKey(ra => ra.RequiredLevelId).OnDelete(DeleteBehavior.SetNull);

        // Ensure uniqueness of action types
        builder.HasIndex(ra => ra.ActionType).IsUnique();

        // Additional indexes for performance
        builder.HasIndex(ra => new { ra.IsActive, ra.Points });
    }
}
