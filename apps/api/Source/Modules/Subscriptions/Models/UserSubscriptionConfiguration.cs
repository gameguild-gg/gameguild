using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Subscriptions.Models;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription> {
  public void Configure(EntityTypeBuilder<UserSubscription> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(us => us.User).WithMany().HasForeignKey(us => us.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with SubscriptionPlan (can't be done with annotations)
    builder.HasOne(us => us.SubscriptionPlan).WithMany().HasForeignKey(us => us.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);

    // Configure Money value object for Amount property
    builder.Property(us => us.Amount)
      .HasConversion(
        money => money.Amount, // To database
        amount => new Money(amount, "USD") // From database - explicit constructor
      )
      .HasColumnName("amount")
      .HasColumnType("decimal(18,2)");
  }
}
