using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Products;

/// <summary>
/// Junction entity representing the relationship between a Product and a Program
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("product_programs")]
[Index(nameof(ProductId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(ProductId), nameof(SortOrder))]
[Index(nameof(ProgramId))]
public class ProductProgram : Entity {
  /// <summary>
  /// Foreign key to the Product entity
  /// </summary>
  [Required]
  public Guid ProductId { get; set; }

  /// <summary>
  /// Navigation property to the Product entity
  /// </summary>
  [ForeignKey(nameof(ProductId))]
  public virtual Product Product { get; set; } = null!;

  /// <summary>
  /// Foreign key to the Program entity
  /// </summary>
  [Required]
  public Guid ProgramId { get; set; }

  /// <summary>
  /// Navigation property to the Program entity
  /// </summary>
  [ForeignKey(nameof(ProgramId))]
  public virtual Programs.Program Program { get; set; } = null!;

  /// <summary>
  /// Display order of programs within the product
  /// </summary>
  public int SortOrder { get; set; }

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
