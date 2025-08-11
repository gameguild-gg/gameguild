using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Products;

/// <summary>
/// Entity Framework configuration for Product entity
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product> {
  public void Configure(EntityTypeBuilder<Product> builder) {
    ArgumentNullException.ThrowIfNull(builder);
    
    // Configure the relationship with User as optional to avoid query filter warnings
    builder.HasOne(p => p.Creator)
           .WithMany()
           .HasForeignKey(p => p.CreatorId)
           .IsRequired(false) // Make the relationship optional
           .OnDelete(DeleteBehavior.SetNull);
  }
}
