namespace GameGuild.Modules.Payments;

/// <summary>
/// DTO for payment statistics
/// </summary>
public class PaymentStatisticsDto
{
  public decimal TotalRevenue { get; set; }
  public int TotalTransactions { get; set; }
  public int SuccessfulTransactions { get; set; }
  public int FailedTransactions { get; set; }
  public decimal AverageTransactionAmount { get; set; }
  public Dictionary<string, decimal> RevenueByMethod { get; set; } = new();
  public Dictionary<string, int> TransactionsByStatus { get; set; } = new();
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
}
