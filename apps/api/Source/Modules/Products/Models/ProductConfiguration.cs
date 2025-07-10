using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Products.Models;

/// <summary>
/// Entity Framework configuration for Product entity
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product> {
  public void Configure(EntityTypeBuilder<Product> builder) {
    // Configure relationship with Creator (can't be done with annotations)
    builder.HasOne(p => p.Creator).WithMany().HasForeignKey(p => p.CreatorId).OnDelete(DeleteBehavior.Restrict);
  }
}
