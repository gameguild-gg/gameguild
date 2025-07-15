using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Products;

/// <summary>
/// Entity Framework configuration for ProductProgram entity
/// </summary>
public class ProductProgramConfiguration : IEntityTypeConfiguration<ProductProgram> {
  public void Configure(EntityTypeBuilder<ProductProgram> builder) {
    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(pp => pp.Product)
           .WithMany(p => p.ProductPrograms)
           .HasForeignKey(pp => pp.ProductId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pp => pp.Program).WithMany().HasForeignKey(pp => pp.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Filtered unique constraint (can't be done with annotations)
    builder.HasIndex(pp => new { pp.ProductId, pp.ProgramId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
  }
}
