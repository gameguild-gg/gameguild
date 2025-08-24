namespace GameGuild.Modules.Products;

/// <summary>
/// Entity Framework configuration for UserProduct entity
/// </summary>
public class UserProductConfiguration : IEntityTypeConfiguration<UserProduct> {
  public void Configure(EntityTypeBuilder<UserProduct> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(up => up.User).WithMany().HasForeignKey(up => up.UserId).OnDelete(DeleteBehavior.Restrict);

    // Configure relationship with Product (can't be done with annotations)
    // This explicitly maps to the UserProducts collection on Product
    builder.HasOne(up => up.Product)
           .WithMany(p => p.UserProducts)
           .HasForeignKey(up => up.ProductId)
           .OnDelete(DeleteBehavior.Restrict);

    // Configure relationship with Subscription (can't be done with annotations)
    builder.HasOne(up => up.Subscription)
           .WithMany()
           .HasForeignKey(up => up.SubscriptionId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with GiftedByUser (can't be done with annotations)
    builder.HasOne(up => up.GiftedByUser)
           .WithMany()
           .HasForeignKey(up => up.GiftedByUserId)
           .OnDelete(DeleteBehavior.SetNull);
  }
}
