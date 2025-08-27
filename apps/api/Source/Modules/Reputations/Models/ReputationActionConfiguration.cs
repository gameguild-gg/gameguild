namespace GameGuild.Modules.Reputations;

public class ReputationActionConfiguration : IEntityTypeConfiguration<ReputationAction> {
  public void Configure(EntityTypeBuilder<ReputationAction> builder) {
    // Configure a relationship with RequiredLevel (can't be done with annotations)
    builder.HasOne(ra => ra.RequiredLevel)
           .WithMany()
           .HasForeignKey(ra => ra.RequiredLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a shadow property for TenantId (since ReputationAction implements ITenantable)
    builder.Property<Guid?>("TenantId");

    // Filtered unique constraint (can't be done with annotations)
    builder.HasIndex("ActionType", "TenantId").IsUnique().HasFilter("\"DeletedAt\" IS NULL");
  }
}
