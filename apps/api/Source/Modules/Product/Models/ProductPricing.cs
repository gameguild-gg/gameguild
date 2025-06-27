using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.Product.Models;

/// <summary>
/// Entity representing pricing information for products
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("product_pricing")]
[Index(nameof(ProductId))]
[Index(nameof(IsDefault))]
[Index(nameof(Currency))]
[Index(nameof(SaleStartDate))]
[Index(nameof(SaleEndDate))]
public class ProductPricing : BaseEntity {
  private Guid _productId;

  private Product _product = null!;

  private string _name = string.Empty;

  private decimal _basePrice;

  private decimal? _salePrice;

  private string _currency = "USD";

  private DateTime? _saleStartDate;

  private DateTime? _saleEndDate;

  private bool _isDefault = false;

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
  /// Name of this pricing option (e.g., "Standard", "Premium", "Early Bird")
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Regular price for this product
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal BasePrice {
    get => _basePrice;
    set => _basePrice = value;
  }

  /// <summary>
  /// Sale price (if on sale)
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal? SalePrice {
    get => _salePrice;
    set => _salePrice = value;
  }

  /// <summary>
  /// Currency code for prices
  /// </summary>
  [MaxLength(3)]
  public string Currency {
    get => _currency;
    set => _currency = value;
  }

  /// <summary>
  /// When the sale price becomes active
  /// </summary>
  public DateTime? SaleStartDate {
    get => _saleStartDate;
    set => _saleStartDate = value;
  }

  /// <summary>
  /// When the sale price expires
  /// </summary>
  public DateTime? SaleEndDate {
    get => _saleEndDate;
    set => _saleEndDate = value;
  }

  /// <summary>
  /// Whether this is the default pricing option for the product
  /// </summary>
  public bool IsDefault {
    get => _isDefault;
    set => _isDefault = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public ProductPricing() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial product pricing data</param>
  public ProductPricing(object partial) : base(partial) { }

  /// <summary>
  /// Get the current effective price (sale price if active, otherwise base price)
  /// </summary>
  public decimal GetCurrentPrice() {
    if (SalePrice.HasValue && IsSaleActive()) { return SalePrice.Value; }

    return BasePrice;
  }

  /// <summary>
  /// Check if a sale is currently active
  /// </summary>
  public bool IsSaleActive() {
    if (!SalePrice.HasValue) return false;

    var now = DateTime.UtcNow;

    return (SaleStartDate == null || SaleStartDate <= now) && (SaleEndDate == null || SaleEndDate > now);
  }
}
