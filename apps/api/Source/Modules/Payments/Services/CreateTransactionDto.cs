using GameGuild.Common;


namespace GameGuild.Modules.Payments.Services;

/// <summary>
/// DTO for creating a transaction
/// </summary>
public class CreateTransactionDto
{
  public Guid? ToUserId { get; set; }
  public TransactionType Type { get; set; }
  public decimal Amount { get; set; }
  public string Currency { get; set; } = "USD";
  public Guid? PaymentMethodId { get; set; }
  public string? Description { get; set; }
  public string? Metadata { get; set; } // JSON string for additional data
}
