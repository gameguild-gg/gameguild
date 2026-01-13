using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Programs;

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
