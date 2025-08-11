using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.Payments;

/// <summary>
/// Represents a payment transaction in the system
/// </summary>
public class Payment {
  [Key] public Guid Id { get; set; } = Guid.NewGuid();

  /// <summary>
  /// User who made the payment
  /// </summary>
  public required Guid UserId { get; set; }

  /// <summary>
  /// Product that was purchased (optional for custom payments)
  /// </summary>
  public Guid? ProductId { get; set; }

  /// <summary>
  /// Amount paid in the smallest currency unit (e.g., cents)
  /// </summary>
  public decimal Amount { get; set; }

  /// <summary>
  /// Currency code (e.g., USD, EUR)
  /// </summary>
  [MaxLength(3)]
  public required string Currency { get; set; } = "USD";

  /// <summary>
  /// Payment status
  /// </summary>
  public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

  /// <summary>
  /// Payment method used
  /// </summary>
  public PaymentMethod Method { get; set; }

  /// <summary>
  /// Payment gateway used for processing
  /// </summary>
  public PaymentGateway Gateway { get; set; } = PaymentGateway.Stripe;

  /// <summary>
  /// External payment provider transaction ID
  /// </summary>
  [MaxLength(255)]
  public string? ProviderTransactionId { get; set; }

  /// <summary>
  /// Payment intent ID from payment provider
  /// </summary>
  [MaxLength(255)]
  public string? PaymentIntentId { get; set; }

  /// <summary>
  /// Discount code applied (if any)
  /// </summary>
  public Guid? DiscountCodeId { get; set; }

  /// <summary>
  /// Discount amount applied
  /// </summary>
  public decimal DiscountAmount { get; set; } = 0;

  /// <summary>
  /// Final amount after discounts
  /// </summary>
  public decimal FinalAmount { get; set; }

  /// <summary>
  /// Processing fee charged by payment provider
  /// </summary>
  public decimal ProcessingFee { get; set; } = 0;

  /// <summary>
  /// Net amount received after fees
  /// </summary>
  public decimal NetAmount { get; set; }

  /// <summary>
  /// When the payment was initiated
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When the payment was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When the payment was successfully processed
  /// </summary>
  public DateTime? ProcessedAt { get; set; }

  /// <summary>
  /// When the payment failed (if applicable)
  /// </summary>
  public DateTime? FailedAt { get; set; }

  /// <summary>
  /// Reason for payment failure
  /// </summary>
  [MaxLength(500)]
  public string? FailureReason { get; set; }

  /// <summary>
  /// Additional metadata as JSON
  /// </summary>
  public string? Metadata { get; set; }

  /// <summary>
  /// Refund information if payment was refunded
  /// </summary>
  public virtual ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();

  /// <summary>
  /// Mark payment as successful
  /// </summary>
  public void MarkAsSuccessful() {
    Status = PaymentStatus.Completed;
    ProcessedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
    FailedAt = null;
    FailureReason = null;
  }

  /// <summary>
  /// Mark payment as failed
  /// </summary>
  public void MarkAsFailed(string reason) {
    Status = PaymentStatus.Failed;
    FailedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
    FailureReason = reason;
  }

  /// <summary>
  /// Mark payment as refunded
  /// </summary>
  public void MarkAsRefunded() {
    Status = PaymentStatus.Refunded;
    UpdatedAt = DateTime.UtcNow;
  }
}

/// <summary>
/// Payment status enumeration
/// </summary>
public enum PaymentStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  Refunded = 5,
  PartiallyRefunded = 6,
}

/// <summary>
/// Payment method enumeration
/// </summary>
public enum PaymentMethod {
  CreditCard = 0,
  DebitCard = 1,
  PayPal = 2,
  BankTransfer = 3,
  Cryptocurrency = 4,
  DigitalWallet = 5,
  GiftCard = 6,
  Credits = 7,
}

/// <summary>
/// Discount code model
/// </summary>
public class DiscountCode {
  [Key] public Guid Id { get; set; } = Guid.NewGuid();

  /// <summary>
  /// Discount code (e.g., "SAVE20")
  /// </summary>
  [MaxLength(50)]
  public required string Code { get; set; }

  /// <summary>
  /// Discount type
  /// </summary>
  public DiscountType Type { get; set; }

  /// <summary>
  /// Discount value (percentage or fixed amount)
  /// </summary>
  public decimal Value { get; set; }

  /// <summary>
  /// Minimum order amount required
  /// </summary>
  public decimal? MinimumAmount { get; set; }

  /// <summary>
  /// Maximum discount amount (for percentage discounts)
  /// </summary>
  public decimal? MaximumDiscount { get; set; }

  /// <summary>
  /// When the discount code becomes valid
  /// </summary>
  public DateTime? ValidFrom { get; set; }

  /// <summary>
  /// When the discount code expires
  /// </summary>
  public DateTime? ValidUntil { get; set; }

  /// <summary>
  /// Maximum number of uses allowed
  /// </summary>
  public int? MaxUses { get; set; }

  /// <summary>
  /// Current number of uses
  /// </summary>
  public int CurrentUses { get; set; } = 0;

  /// <summary>
  /// Whether the discount code is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// When the discount code was created
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Discount type enumeration
/// </summary>
public enum DiscountType { Percentage = 0, FixedAmount = 1 }

/// <summary>
/// Payment refund information
/// </summary>
public class PaymentRefund {
  [Key] public Guid Id { get; set; } = Guid.NewGuid();

  /// <summary>
  /// The original payment being refunded
  /// </summary>
  public Guid PaymentId { get; set; }

  public virtual Payment Payment { get; set; } = null!;

  /// <summary>
  /// External refund ID from payment provider
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string ExternalRefundId { get; set; } = string.Empty;

  /// <summary>
  /// Refund amount in cents
  /// </summary>
  public decimal RefundAmount { get; set; }

  /// <summary>
  /// Reason for the refund
  /// </summary>
  [Required]
  [MaxLength(500)]
  public string Reason { get; set; } = string.Empty;

  /// <summary>
  /// Status of the refund
  /// </summary>
  public RefundStatus Status { get; set; } = RefundStatus.Pending;

  /// <summary>
  /// When the refund was processed
  /// </summary>
  public DateTime? ProcessedAt { get; set; }

  /// <summary>
  /// When the refund was created
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When the refund was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Status of a refund
/// </summary>
public enum RefundStatus {
  Pending = 0,
  Processing = 1,
  Succeeded = 2,
  Failed = 3,
  Cancelled = 4,
}
