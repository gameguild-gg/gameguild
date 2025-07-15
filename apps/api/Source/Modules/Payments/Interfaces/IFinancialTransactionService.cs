using GameGuild.Common;


namespace GameGuild.Modules.Payments;

/// <summary>
/// Interface for financial transaction services
/// </summary>
public interface IFinancialTransactionService {
  Task<FinancialTransaction> CreateTransactionAsync(FinancialTransaction transaction);

  Task<FinancialTransaction?> GetTransactionByIdAsync(int id);

  Task<FinancialTransaction?> GetTransactionByExternalIdAsync(string externalId);

  Task<IEnumerable<FinancialTransaction>> GetUserTransactionsAsync(int userId);

  Task<FinancialTransaction> UpdateTransactionStatusAsync(int id, TransactionStatus status);

  Task<FinancialTransaction> ProcessTransactionAsync(int id);

  Task<decimal> GetUserBalanceAsync(int userId);

  Task<IEnumerable<FinancialTransaction>> GetPendingTransactionsAsync();

  Task<bool> RefundTransactionAsync(int transactionId, decimal? amount = null);
}
