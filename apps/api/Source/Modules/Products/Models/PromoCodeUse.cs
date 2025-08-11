using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Payments;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Products;

/// <summary>
/// Entity tracking when and how promo codes are used
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("promo_code_uses")]
public class PromoCodeUse : Entity {
  /// <summary>
  /// Foreign key to the PromoCode entity
  /// </summary>
  [Required]
  public Guid PromoCodeId { get; set; }

  /// <summary>
  /// Navigation property to the PromoCode entity
  /// </summary>
  [ForeignKey(nameof(PromoCodeId))]
  public virtual PromoCode PromoCode { get; set; } = null!;

  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// Navigation property to the User entity
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Foreign key to the FinancialTransaction entity
  /// </summary>
  [Required]
  public Guid FinancialTransactionId { get; set; }

  /// <summary>
  /// Navigation property to the FinancialTransaction entity
  /// </summary>
  [ForeignKey(nameof(FinancialTransactionId))]
  public virtual FinancialTransaction FinancialTransaction { get; set; } = null!;

  /// <summary>
  /// The actual discount amount that was applied
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal DiscountApplied { get; set; }

  /// <summary>
  /// Default constructor
  /// </summary>
  public PromoCodeUse() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial promo code use data</param>
  public PromoCodeUse(object partial) : base(partial) { }
}
