using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Product.Models;

namespace GameGuild.Modules.Subscription.Models;

[Table("user_subscriptions")]
[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(SubscriptionPlanId))]
[Index(nameof(CurrentPeriodStart))]
[Index(nameof(CurrentPeriodEnd))]
[Index(nameof(NextBillingAt))]
[Index(nameof(ExternalSubscriptionId))]
public class UserSubscription : BaseEntity {
  private Guid _userId;

  private Guid _subscriptionPlanId;

  private SubscriptionStatus _status = SubscriptionStatus.Active;

  private string? _externalSubscriptionId;

  private DateTime _currentPeriodStart;

  private DateTime _currentPeriodEnd;

  private DateTime? _canceledAt;

  private DateTime? _endsAt;

  private DateTime? _trialEndsAt;

  private DateTime? _lastPaymentAt;

  private DateTime? _nextBillingAt;

  private User.Models.User _user = null!;

  private ProductSubscriptionPlan _subscriptionPlan = null!;

  private ICollection<UserProduct> _userProducts = new List<Product.Models.UserProduct>();

  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  public Guid SubscriptionPlanId {
    get => _subscriptionPlanId;
    set => _subscriptionPlanId = value;
  }

  public SubscriptionStatus Status {
    get => _status;
    set => _status = value;
  }

  /// <summary>
  /// External subscription ID from payment provider (Stripe, PayPal, etc.)
  /// </summary>
  [MaxLength(255)]
  public string? ExternalSubscriptionId {
    get => _externalSubscriptionId;
    set => _externalSubscriptionId = value;
  }

  /// <summary>
  /// Current billing period start date
  /// </summary>
  public DateTime CurrentPeriodStart {
    get => _currentPeriodStart;
    set => _currentPeriodStart = value;
  }

  /// <summary>
  /// Current billing period end date
  /// </summary>
  public DateTime CurrentPeriodEnd {
    get => _currentPeriodEnd;
    set => _currentPeriodEnd = value;
  }

  /// <summary>
  /// Date when the subscription was canceled (null if not canceled)
  /// </summary>
  public DateTime? CanceledAt {
    get => _canceledAt;
    set => _canceledAt = value;
  }

  /// <summary>
  /// Date when the subscription will end (null if indefinite)
  /// </summary>
  public DateTime? EndsAt {
    get => _endsAt;
    set => _endsAt = value;
  }

  /// <summary>
  /// Date when the trial period ends (null if no trial)
  /// </summary>
  public DateTime? TrialEndsAt {
    get => _trialEndsAt;
    set => _trialEndsAt = value;
  }

  /// <summary>
  /// Last successful payment date
  /// </summary>
  public DateTime? LastPaymentAt {
    get => _lastPaymentAt;
    set => _lastPaymentAt = value;
  }

  /// <summary>
  /// Next scheduled billing date
  /// </summary>
  public DateTime? NextBillingAt {
    get => _nextBillingAt;
    set => _nextBillingAt = value;
  }

  // Navigation properties
  [ForeignKey(nameof(UserId))]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  [ForeignKey(nameof(SubscriptionPlanId))]
  public virtual Product.Models.ProductSubscriptionPlan SubscriptionPlan {
    get => _subscriptionPlan;
    set => _subscriptionPlan = value;
  }

  public virtual ICollection<Product.Models.UserProduct> UserProducts {
    get => _userProducts;
    set => _userProducts = value;
  }
}

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription> {
  public void Configure(EntityTypeBuilder<UserSubscription> builder) {
    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(us => us.User).WithMany().HasForeignKey(us => us.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with SubscriptionPlan (can't be done with annotations)
    builder.HasOne(us => us.SubscriptionPlan).WithMany().HasForeignKey(us => us.SubscriptionPlanId).OnDelete(DeleteBehavior.Restrict);
  }
}
