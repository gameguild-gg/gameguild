using GameGuild.CQRS;


namespace GameGuild.Modules.Payments.Commands.ProcessPayment;

/// <summary>
/// Command to process a payment with enhanced features
/// </summary>
public record ProcessPaymentCommand : ICommand<PaymentResult>
{
    /// <summary>
    /// User making the payment
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Product being purchased (optional)
    /// </summary>
    public Guid? ProductId { get; init; }

    /// <summary>
    /// Subscription being paid for (optional)
    /// </summary>
    public Guid? SubscriptionId { get; init; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public Money Amount { get; init; } = Money.Zero();

    /// <summary>
    /// Payment method identifier
    /// </summary>
    public string PaymentMethodId { get; init; } = string.Empty;

    /// <summary>
    /// Discount code to apply (optional)
    /// </summary>
    public string? DiscountCode { get; init; }

    /// <summary>
    /// Payment description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether to save payment method for future use
    /// </summary>
    public bool SavePaymentMethod { get; init; } = true;

    /// <summary>
    /// Whether to send confirmation email
    /// </summary>
    public bool SendConfirmation { get; init; } = true;

    /// <summary>
    /// Idempotency key for duplicate prevention
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Return URL after payment (for redirect flows)
    /// </summary>
    public string? ReturnUrl { get; init; }

    /// <summary>
    /// User's IP address for fraud prevention
    /// </summary>
    public string? UserIpAddress { get; init; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; init; }
}
