using GameGuild.Common;


namespace GameGuild.Modules.Payments.Interfaces;

/// <summary>
/// Interface for financial transaction services
/// </summary>
public interface IFinancialTransactionService {
  Task<Models.FinancialTransaction> CreateTransactionAsync(Models.FinancialTransaction transaction);

  Task<Models.FinancialTransaction?> GetTransactionByIdAsync(int id);

  Task<Models.FinancialTransaction?> GetTransactionByExternalIdAsync(string externalId);

  Task<IEnumerable<Models.FinancialTransaction>> GetUserTransactionsAsync(int userId);

  Task<Models.FinancialTransaction> UpdateTransactionStatusAsync(int id, TransactionStatus status);

  Task<Models.FinancialTransaction> ProcessTransactionAsync(int id);

  Task<decimal> GetUserBalanceAsync(int userId);

  Task<IEnumerable<Models.FinancialTransaction>> GetPendingTransactionsAsync();

  Task<bool> RefundTransactionAsync(int transactionId, decimal? amount = null);
}
