using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Subscription.Models;

namespace GameGuild.Modules.Product.Models;

/// <summary>
/// Junction entity representing the relationship between a User and a Product
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("user_products")]
[Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProductId))]
[Index(nameof(AccessStatus))]
[Index(nameof(AcquisitionType))]
[Index(nameof(AccessEndDate))]
[Index(nameof(SubscriptionId))]
public class UserProduct : BaseEntity {
  private Guid _userId;

  private User.Models.User _user = null!;

  private Guid _productId;

  private Product _product = null!;

  private Guid? _subscriptionId;

  private UserSubscription? _subscription;

  private ProductAcquisitionType _acquisitionType;

  private ProductAccessStatus _accessStatus = ProductAccessStatus.Active;

  private decimal _pricePaid;

  private string _currency = "USD";

  private DateTime? _accessStartDate;

  private DateTime? _accessEndDate;

  private Guid? _giftedByUserId;

  private User.Models.User? _giftedByUser;

  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Navigation property to the User entity
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

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
  /// Foreign key to the Subscription entity (optional)
  /// </summary>
  public Guid? SubscriptionId {
    get => _subscriptionId;
    set => _subscriptionId = value;
  }

  /// <summary>
  /// Navigation property to the Subscription entity
  /// </summary>
  [ForeignKey(nameof(SubscriptionId))]
  public virtual Subscription.Models.UserSubscription? Subscription {
    get => _subscription;
    set => _subscription = value;
  }

  /// <summary>
  /// How the user acquired this product
  /// </summary>
  public ProductAcquisitionType AcquisitionType {
    get => _acquisitionType;
    set => _acquisitionType = value;
  }

  /// <summary>
  /// Current access status for this product
  /// </summary>
  public ProductAccessStatus AccessStatus {
    get => _accessStatus;
    set => _accessStatus = value;
  }

  /// <summary>
  /// Amount the user paid for this product
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal PricePaid {
    get => _pricePaid;
    set => _pricePaid = value;
  }

  /// <summary>
  /// Currency code for the price paid
  /// </summary>
  [MaxLength(3)]
  public string Currency {
    get => _currency;
    set => _currency = value;
  }

  /// <summary>
  /// When the user's access to this product starts
  /// </summary>
  public DateTime? AccessStartDate {
    get => _accessStartDate;
    set => _accessStartDate = value;
  }

  /// <summary>
  /// When the user's access to this product ends
  /// </summary>
  public DateTime? AccessEndDate {
    get => _accessEndDate;
    set => _accessEndDate = value;
  }

  /// <summary>
  /// User who gifted this product (if acquisition type is Gift)
  /// </summary>
  public Guid? GiftedByUserId {
    get => _giftedByUserId;
    set => _giftedByUserId = value;
  }

  /// <summary>
  /// Navigation property to the user who gifted this product
  /// </summary>
  [ForeignKey(nameof(GiftedByUserId))]
  public virtual User.Models.User? GiftedByUser {
    get => _giftedByUser;
    set => _giftedByUser = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public UserProduct() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial user product data</param>
  public UserProduct(object partial) : base(partial) { }

  /// <summary>
  /// Check if the user currently has active access to this product
  /// </summary>
  public bool HasActiveAccess() {
    if (AccessStatus != ProductAccessStatus.Active) return false;

    DateTime now = DateTime.UtcNow;

    return (AccessStartDate == null || AccessStartDate <= now) && (AccessEndDate == null || AccessEndDate > now);
  }

  /// <summary>
  /// Grant access to the product
  /// </summary>
  public void GrantAccess(DateTime? startDate = null, DateTime? endDate = null) {
    AccessStatus = ProductAccessStatus.Active;
    AccessStartDate = startDate ?? DateTime.UtcNow;
    AccessEndDate = endDate;
    Touch();
  }

  /// <summary>
  /// Revoke access to the product
  /// </summary>
  public void RevokeAccess() {
    AccessStatus = ProductAccessStatus.Revoked;
    AccessEndDate = DateTime.UtcNow;
    Touch();
  }
}

/// <summary>
/// Entity Framework configuration for UserProduct entity
/// </summary>
public class UserProductConfiguration : IEntityTypeConfiguration<UserProduct> {
  public void Configure(EntityTypeBuilder<UserProduct> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(up => up.User).WithMany().HasForeignKey(up => up.UserId).OnDelete(DeleteBehavior.Restrict);

    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(up => up.Product).WithMany().HasForeignKey(up => up.ProductId).OnDelete(DeleteBehavior.Restrict);

    // Configure relationship with Subscription (can't be done with annotations)
    builder.HasOne(up => up.Subscription).WithMany().HasForeignKey(up => up.SubscriptionId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with GiftedByUser (can't be done with annotations)
    builder.HasOne(up => up.GiftedByUser).WithMany().HasForeignKey(up => up.GiftedByUserId).OnDelete(DeleteBehavior.SetNull);
  }
}
