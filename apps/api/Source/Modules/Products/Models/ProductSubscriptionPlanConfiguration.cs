using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Products.Models;

/// <summary>
/// Entity Framework configuration for ProductSubscriptionPlan entity
/// </summary>
public class ProductSubscriptionPlanConfiguration : IEntityTypeConfiguration<ProductSubscriptionPlan> {
  public void Configure(EntityTypeBuilder<ProductSubscriptionPlan> builder) {
    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(psp => psp.Product)
           .WithMany(p => p.SubscriptionPlans)
           .HasForeignKey(psp => psp.ProductId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure additional indexes for performance
    builder.HasIndex(psp => psp.ProductId).HasDatabaseName("IX_ProductSubscriptionPlans_ProductId");

    builder.HasIndex(psp => psp.Name).HasDatabaseName("IX_ProductSubscriptionPlans_Name");

    builder.HasIndex(psp => psp.IsActive).HasDatabaseName("IX_ProductSubscriptionPlans_IsActive");

    builder.HasIndex(psp => psp.IsDefault).HasDatabaseName("IX_ProductSubscriptionPlans_IsDefault");

    builder.HasIndex(psp => psp.Price).HasDatabaseName("IX_ProductSubscriptionPlans_Price");

    builder.HasIndex(psp => psp.BillingInterval).HasDatabaseName("IX_ProductSubscriptionPlans_BillingInterval");
  }
}
