using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.Domain.Entities;

/// <summary>Entity tracking when and how promo codes are used</summary>
[Table("promo_code_uses")]
public class PromoCodeUse : EntityBase
{
    /// <summary>Default constructor</summary>
    public PromoCodeUse() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial promo code use data</param>
    public PromoCodeUse(object partial) : base(partial) { }

    /// <summary>Foreign key to the PromoCode entity</summary>
    [Required]
    public Guid PromoCodeId { get; set; }

    /// <summary>Navigation property to the PromoCode entity</summary>
    [ForeignKey(nameof(PromoCodeId))]
    public virtual PromoCode PromoCode { get; set; } = null!;

    /// <summary>Foreign key to the User entity</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Navigation property to the User entity</summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>The actual discount amount that was applied</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountApplied { get; set; }
}