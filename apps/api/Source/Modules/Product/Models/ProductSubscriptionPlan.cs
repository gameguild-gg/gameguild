using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Subscription.Models;

namespace GameGuild.Modules.Product.Models;

/// <summary>
/// Entity representing subscription plans for products
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("product_subscription_plans")]
[Index(nameof(ProductId))]
[Index(nameof(Name))]
[Index(nameof(IsActive))]
[Index(nameof(IsDefault))]
[Index(nameof(Price))]
[Index(nameof(BillingInterval))]
public class ProductSubscriptionPlan : BaseEntity {
  private Guid _productId;

  private Product _product = null!;

  private string _name = string.Empty;

  private string? _description;

  private decimal _price;

  private string _currency = "USD";

  private SubscriptionBillingInterval _billingInterval;

  private int _intervalCount = 1;

  private int? _trialPeriodDays;

  private bool _isActive = true;

  private bool _isDefault = false;

  private ICollection<UserSubscription> _userSubscriptions = new List<Subscription.Models.UserSubscription>();

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
  /// Name of the subscription plan
  /// </summary>
  [Required]
  [MaxLength(255)]
  public string Name {
    get => _name;
    set => _name = value;
  }

  /// <summary>
  /// Description of what's included in this plan
  /// </summary>
  [MaxLength(1000)]
  public string? Description {
    get => _description;
    set => _description = value;
  }

  /// <summary>
  /// Price for each billing cycle
  /// </summary>
  [Column(TypeName = "decimal(10,2)")]
  public decimal Price {
    get => _price;
    set => _price = value;
  }

  /// <summary>
  /// Currency code for the price
  /// </summary>
  [MaxLength(3)]
  public string Currency {
    get => _currency;
    set => _currency = value;
  }

  /// <summary>
  /// How often the subscription is billed
  /// </summary>
  public SubscriptionBillingInterval BillingInterval {
    get => _billingInterval;
    set => _billingInterval = value;
  }

  /// <summary>
  /// Number of billing intervals between charges (e.g., 3 months = interval_count: 3, billing_interval: Month)
  /// </summary>
  public int IntervalCount {
    get => _intervalCount;
    set => _intervalCount = value;
  }

  /// <summary>
  /// Free trial period in days
  /// </summary>
  public int? TrialPeriodDays {
    get => _trialPeriodDays;
    set => _trialPeriodDays = value;
  }

  /// <summary>
  /// Whether this plan is currently available for new subscriptions
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// Whether this is the default plan for the product
  /// </summary>
  public bool IsDefault {
    get => _isDefault;
    set => _isDefault = value;
  }

  /// <summary>
  /// Default constructor
  /// </summary>
  public ProductSubscriptionPlan() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial product subscription plan data</param>
  public ProductSubscriptionPlan(object partial) : base(partial) { }

  public virtual ICollection<Subscription.Models.UserSubscription> UserSubscriptions {
    get => _userSubscriptions;
    set => _userSubscriptions = value;
  }
}

/// <summary>
/// Entity Framework configuration for ProductSubscriptionPlan entity
/// </summary>
public class ProductSubscriptionPlanConfiguration : IEntityTypeConfiguration<ProductSubscriptionPlan> {
  public void Configure(EntityTypeBuilder<ProductSubscriptionPlan> builder) {
    // Configure relationship with Product (can't be done with annotations)
    builder.HasOne(psp => psp.Product).WithMany(p => p.SubscriptionPlans).HasForeignKey(psp => psp.ProductId).OnDelete(DeleteBehavior.Cascade);

    // Configure additional indexes for performance
    builder.HasIndex(psp => psp.ProductId).HasDatabaseName("IX_ProductSubscriptionPlans_ProductId");

    builder.HasIndex(psp => psp.Name).HasDatabaseName("IX_ProductSubscriptionPlans_Name");

    builder.HasIndex(psp => psp.IsActive).HasDatabaseName("IX_ProductSubscriptionPlans_IsActive");

    builder.HasIndex(psp => psp.IsDefault).HasDatabaseName("IX_ProductSubscriptionPlans_IsDefault");

    builder.HasIndex(psp => psp.Price).HasDatabaseName("IX_ProductSubscriptionPlans_Price");

    builder.HasIndex(psp => psp.BillingInterval).HasDatabaseName("IX_ProductSubscriptionPlans_BillingInterval");
  }
}
