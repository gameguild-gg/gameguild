using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Modules.Programs;


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
    // This explicitly maps to the ProductPrograms collection on Program
    builder.HasOne(pp => pp.Program)
           .WithMany(p => p.ProductPrograms)
           .HasForeignKey(pp => pp.ProgramId)
           .OnDelete(DeleteBehavior.Cascade);

    // Filtered unique constraint (can't be done with annotations)
    builder.HasIndex(pp => new { pp.ProductId, pp.ProgramId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
  }
}
