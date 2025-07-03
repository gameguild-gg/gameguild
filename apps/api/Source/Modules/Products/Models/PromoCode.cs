using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Payment.Models;


namespace GameGuild.Modules.Product.Models;

/// <summary>
/// Entity representing promotional codes for discounts
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("promo_codes")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Type))]
[Index(nameof(IsActive))]
[Index(nameof(ValidFrom))]
[Index(nameof(ValidUntil))]
public class PromoCode : BaseEntity {
  private string _code = string.Empty;

  private string _name = string.Empty;

  private string? _description;

  private PromoCodeType _type;

  private decimal? _discountPercentage;

  private decimal? _discountAmount;

  private string _currency = "USD";

  private decimal? _minimumOrderAmount;

  private int? _maxUses;

  private int? _maxUsesPerUser;

  private DateTime? _validFrom;

  private DateTime? _validUntil;

  private bool _isActive = true;

  private Guid? _productId;

  private Product? _product;

  private Guid _createdBy;

  private User.Models.User _createdByUser = null!;

  private ICollection<PromoCodeUse> _promoCodeUses = new List<PromoCodeUse>();

  private ICollection<FinancialTransaction> _financialTransactions = new List<Payment.Models.FinancialTransaction>();

  /// <summary>
  /// The promotional code that users enter
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Code {
    get => _code;
    set => _code = value;
  }

  /// <summary>
  /// Display name for the promotional code
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Description of what the promo code does
  /// </summary>
  public string? Description {
    get => _description;
    set => _description = value;
  }

  /// <summary>
  /// Type of discount this promo code provides
  /// </summary>
  public PromoCodeType Type {
    get => _type;
    set => _type = value;
  }

  /// <summary>
  /// Discount percentage (for PercentageOff type)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? DiscountPercentage {
    get => _discountPercentage;
    set => _discountPercentage = value;
  }

  /// <summary>
  /// Fixed discount amount (for FixedAmountOff type)
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal? DiscountAmount {
    get => _discountAmount;
    set => _discountAmount = value;
  }

  /// <summary>
  /// Currency code for fixed amount discounts
  /// </summary>
  [MaxLength(3)]
  public string Currency {
    get => _currency;
    set => _currency = value;
  }

  /// <summary>
  /// Minimum order amount required to use this promo code
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal? MinimumOrderAmount {
    get => _minimumOrderAmount;
    set => _minimumOrderAmount = value;
  }

  /// <summary>
  /// Maximum number of times this code can be used (null = unlimited)
  /// </summary>
  public int? MaxUses {
    get => _maxUses;
    set => _maxUses = value;
  }

  /// <summary>
  /// Maximum uses per user (null = unlimited per user)
  /// </summary>
  public int? MaxUsesPerUser {
    get => _maxUsesPerUser;
    set => _maxUsesPerUser = value;
  }

  /// <summary>
  /// When this promo code becomes valid
  /// </summary>
  public DateTime? ValidFrom {
    get => _validFrom;
    set => _validFrom = value;
  }

  /// <summary>
  /// When this promo code expires
  /// </summary>
  public DateTime? ValidUntil {
    get => _validUntil;
    set => _validUntil = value;
  }

  /// <summary>
  /// Whether this promo code is currently active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// Specific product this code applies to (null = applies to all products)
  /// </summary>
  public Guid? ProductId {
    get => _productId;
    set => _productId = value;
  }

  /// <summary>
  /// Navigation property to the specific product (if applicable)
  /// </summary>
  [ForeignKey(nameof(ProductId))]
  public virtual Product? Product {
    get => _product;
    set => _product = value;
  }

  /// <summary>
  /// Foreign key to the user who created this promo code
  /// </summary>
  [Required]
  public Guid CreatedBy {
    get => _createdBy;
    set => _createdBy = value;
  }

  /// <summary>
  /// Navigation property to the user who created this promo code
  /// </summary>
  [ForeignKey(nameof(CreatedBy))]
  public virtual User.Models.User CreatedByUser {
    get => _createdByUser;
    set => _createdByUser = value;
  }

  /// <summary>
  /// Navigation property to promo code usage records
  /// </summary>
  public virtual ICollection<PromoCodeUse> PromoCodeUses {
    get => _promoCodeUses;
    set => _promoCodeUses = value;
  }

  /// <summary>
  /// Navigation property to financial transactions that used this promo code
  /// </summary>
  public virtual ICollection<Payment.Models.FinancialTransaction> FinancialTransactions {
    get => _financialTransactions;
    set => _financialTransactions = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public PromoCode() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial promo code data</param>
  public PromoCode(object partial) : base(partial) { }

  /// <summary>
  /// Check if the promo code is currently valid
  /// </summary>
  public bool IsCurrentlyValid() {
    if (!IsActive) return false;

    var now = DateTime.UtcNow;

    return (ValidFrom == null || ValidFrom <= now) && (ValidUntil == null || ValidUntil > now);
  }

  /// <summary>
  /// Calculate the discount amount for a given order amount
  /// </summary>
  public decimal CalculateDiscount(decimal orderAmount) {
    if (!IsCurrentlyValid()) return 0;

    if (MinimumOrderAmount.HasValue && orderAmount < MinimumOrderAmount.Value) return 0;

    return Type switch { PromoCodeType.PercentageOff => orderAmount * (DiscountPercentage ?? 0) / 100, PromoCodeType.FixedAmountOff => Math.Min(DiscountAmount ?? 0, orderAmount), _ => 0 };
  }
}

/// <summary>
/// Entity Framework configuration for PromoCode entity
/// </summary>
public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode> {
  public void Configure(EntityTypeBuilder<PromoCode> builder) {
    // Configure relationship with CreatedByUser (can't be done with annotations)
    builder.HasOne(pc => pc.CreatedByUser)
           .WithMany()
           .HasForeignKey(pc => pc.CreatedBy)
           .OnDelete(DeleteBehavior.Restrict);

    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(pc => pc.Product)
           .WithMany(p => p.PromoCodes)
           .HasForeignKey(pc => pc.ProductId)
           .OnDelete(DeleteBehavior.Cascade);
  }
}
