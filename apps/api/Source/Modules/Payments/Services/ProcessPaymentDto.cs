namespace GameGuild.Modules.Payments;

/// <summary>
/// DTO for processing a payment
/// </summary>
public class ProcessPaymentDto
{
  public Guid PaymentMethodId { get; set; }
  public string? ExternalTransactionId { get; set; }
  public string? PaymentIntentId { get; set; } // For Stripe integration
  public string? Metadata { get; set; }
}
