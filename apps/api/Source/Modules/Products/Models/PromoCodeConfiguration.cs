using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Products.Models;

/// <summary>
/// Entity Framework configuration for PromoCode entity
/// </summary>
public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode> {
  public void Configure(EntityTypeBuilder<PromoCode> builder) {
    // Configure relationship with CreatedByUser (can't be done with annotations)
    builder.HasOne(pc => pc.CreatedByUser)
           .WithMany()
           .HasForeignKey(pc => pc.CreatedBy)
           .OnDelete(DeleteBehavior.Restrict);

    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(pc => pc.Product)
           .WithMany(p => p.PromoCodes)
           .HasForeignKey(pc => pc.ProductId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
