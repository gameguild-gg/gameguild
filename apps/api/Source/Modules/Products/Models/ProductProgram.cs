using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.Product.Models;

/// <summary>
/// Junction entity representing the relationship between a Product and a Program
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("product_programs")]
[Index(nameof(ProductId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(ProductId), nameof(SortOrder))]
[Index(nameof(ProgramId))]
public class ProductProgram : BaseEntity {
  private Guid _productId;

  private Product _product = null!;

  private Guid _programId;

  private Program.Models.Program _program = null!;

  private int _sortOrder = 0;

  /// <summary>
  /// Foreign key to the Product entity
  /// </summary>
  [Required]
  public Guid ProductId {
    get => _productId;
    set => _productId = value;
  }

  /// <summary>
  /// Navigation property to the Product entity
  /// </summary>
  [ForeignKey(nameof(ProductId))]
  public virtual Product Product {
    get => _product;
    set => _product = value;
  }

  /// <summary>
  /// Foreign key to the Program entity
  /// </summary>
  [Required]
  public Guid ProgramId {
    get => _programId;
    set => _programId = value;
  }

  /// <summary>
  /// Navigation property to the Program entity
  /// </summary>
  [ForeignKey(nameof(ProgramId))]
  public virtual Program.Models.Program Program {
    get => _program;
    set => _program = value;
  }

  /// <summary>
  /// Display order of programs within the product
  /// </summary>
  public int SortOrder {
    get => _sortOrder;
    set => _sortOrder = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public ProductProgram() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial product program data</param>
  public ProductProgram(object partial) : base(partial) { }
}

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
