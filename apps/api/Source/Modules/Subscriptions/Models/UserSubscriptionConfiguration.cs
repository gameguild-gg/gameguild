namespace GameGuild.Modules.Subscriptions.Models;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription> {
  public void Configure(EntityTypeBuilder<UserSubscription> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(us => us.User).WithMany().HasForeignKey(us => us.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with SubscriptionPlan (can't be done with annotations)
    builder.HasOne(us => us.SubscriptionPlan)
           .WithMany()
           .HasForeignKey(us => us.SubscriptionPlanId)
           .OnDelete(DeleteBehavior.Restrict);
  }
}
