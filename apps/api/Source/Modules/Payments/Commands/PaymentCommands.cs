namespace GameGuild.Modules.Payments;

/// <summary>
/// Command to create a payment intent
/// </summary>
public record CreatePaymentCommand : IRequest<CreatePaymentResult> {
  public Guid UserId { get; init; }

  public Guid? ProductId { get; init; }

  public decimal Amount { get; init; }

  public string Currency { get; init; } = "USD";

  public PaymentMethod Method { get; init; }

  public string? Description { get; init; }

  public Guid? TenantId { get; init; }

  public IDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Command to process a payment
/// </summary>
public record ProcessPaymentCommand : IRequest<ProcessPaymentResult> {
  public Guid PaymentId { get; init; }

  public string ProviderTransactionId { get; init; } = string.Empty;

  public IDictionary<string, object>? ProviderMetadata { get; init; }
}

/// <summary>
/// Command to refund a payment
/// </summary>
public record RefundPaymentCommand : IRequest<RefundPaymentResult> {
  public Guid PaymentId { get; init; }

  public decimal? RefundAmount { get; init; } // If null, full refund

  public string Reason { get; init; } = string.Empty;

  public Guid RefundedBy { get; init; }
}

/// <summary>
/// Command to cancel a payment
/// </summary>
public record CancelPaymentCommand : IRequest<CancelPaymentResult> {
  public Guid PaymentId { get; init; }

  public string Reason { get; init; } = string.Empty;

  public Guid CancelledBy { get; init; }
}

/// <summary>
/// Command to apply discount to payment
/// </summary>
public record ApplyDiscountCommand : IRequest<ApplyDiscountResult> {
  public Guid PaymentId { get; init; }

  public string DiscountCode { get; init; } = string.Empty;

  public Guid UserId { get; init; }
}

/// <summary>
/// Result types for payment commands
/// </summary>
public record CreatePaymentResult {
  public bool Success { get; init; }

  public Payment? Payment { get; init; }

  public string? Error { get; init; }

  public string? ClientSecret { get; init; } // For Stripe-like integrations
}

public record ProcessPaymentResult {
  public bool Success { get; init; }

  public Payment? Payment { get; init; }

  public string? Error { get; init; }

  public bool AutoEnrollTriggered { get; init; }
}

public record RefundPaymentResult {
  public bool Success { get; init; }

  public PaymentRefund? Refund { get; init; }

  public string? Error { get; init; }
}

public record CancelPaymentResult {
  public bool Success { get; init; }

  public Payment? Payment { get; init; }

  public string? Error { get; init; }
}

public record ApplyDiscountResult {
  public bool Success { get; init; }

  public Payment? Payment { get; init; }

  public decimal DiscountAmount { get; init; }

  public string? Error { get; init; }
}
