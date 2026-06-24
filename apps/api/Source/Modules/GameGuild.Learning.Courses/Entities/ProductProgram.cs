using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Commerce.Products;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Represents the relationship between a Product and a Program
/// Allows products to include multiple programs with ordering
/// </summary>
[Table("product_programs")]
[Index(nameof(ProductId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(ProductId))]
[Index(nameof(ProgramId))]
public class ProductProgram : EntityBase
{
    /// <summary>
    /// Product ID (foreign key to Products module)
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Program ID
    /// </summary>
    [Required]
    public Guid ProgramId { get; set; }

    /// <summary>
    /// Sort order within the product
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this program is optional within the product
    /// </summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>
    /// Navigation property to Program
    /// </summary>
    [ForeignKey(nameof(ProgramId))]
    public virtual Program? Program { get; set; }
}

public class ProductProgramConfiguration : IEntityTypeConfiguration<ProductProgram>
{
    public void Configure(EntityTypeBuilder<ProductProgram> builder)
    {
        builder.HasKey(pp => pp.Id);

        builder.HasIndex(pp => new { pp.ProductId, pp.ProgramId }).IsUnique();
        builder.HasIndex(pp => pp.ProductId);
        builder.HasIndex(pp => pp.ProgramId);

        builder.HasOne(pp => pp.Program)
            .WithMany()
            .HasForeignKey(pp => pp.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
