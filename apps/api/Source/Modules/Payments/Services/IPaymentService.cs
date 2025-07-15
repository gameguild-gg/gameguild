using GameGuild.Common;


namespace GameGuild.Modules.Payments;

/// <summary>
/// Service interface for managing payments and financial transactions
/// </summary>
public interface IPaymentService {
  // Payment Methods
  Task<IEnumerable<UserFinancialMethod>> GetUserPaymentMethodsAsync(Guid userId);

  Task<UserFinancialMethod?> GetPaymentMethodByIdAsync(Guid id, Guid userId);

  Task<UserFinancialMethod> CreatePaymentMethodAsync(Guid userId, CreatePaymentMethodDto createDto);

  Task<bool> DeletePaymentMethodAsync(Guid id, Guid userId);

  // Transactions
  Task<IEnumerable<FinancialTransaction>> GetUserTransactionsAsync(Guid userId, int skip = 0, int take = 50, TransactionType? type = null, TransactionStatus? status = null);

  Task<FinancialTransaction?> GetTransactionByIdAsync(Guid id, Guid userId);

  Task<FinancialTransaction> CreateTransactionAsync(Guid userId, CreateTransactionDto createDto);

  Task<FinancialTransaction?> ProcessPaymentAsync(Guid transactionId, Guid userId, ProcessPaymentDto processDto);

  // Admin functions
  Task<IEnumerable<FinancialTransaction>> GetAllTransactionsAsync(int skip = 0, int take = 50, TransactionType? type = null, TransactionStatus? status = null);

  Task<PaymentStatisticsDto> GetPaymentStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

  // Payment processing
  Task<bool> RefundTransactionAsync(Guid transactionId, decimal? amount = null);

  Task<FinancialTransaction?> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status, string? reason = null);
}
