namespace GameGuild.Modules.Payments;

/// <summary>
/// Interface for wallet and balance management services
/// </summary>
public interface IWalletService {
  Task<decimal> GetWalletBalanceAsync(int userId);

  Task<decimal> GetAvailableBalanceAsync(int userId);

  Task<bool> AddFundsAsync(int userId, decimal amount, string description);

  Task<bool> DeductFundsAsync(int userId, decimal amount, string description);

  Task<bool> TransferFundsAsync(int fromUserId, int toUserId, decimal amount, string description);

  Task<bool> FreezeFundsAsync(int userId, decimal amount, string reason);

  Task<bool> UnfreezeFundsAsync(int userId, decimal amount);

  Task<IEnumerable<FinancialTransaction>> GetWalletTransactionsAsync(int userId);
}
