using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Domain.Entities;

namespace GameGuild.Modules.Payments.Payments.Application.Services;

/// <summary>Service for managing user wallets</summary>
public interface IWalletService
{
    /// <summary>Create a new wallet for a user</summary>
    Task<UserWallet> CreateWalletAsync(Guid userId, string currency = "USD", CancellationToken cancellationToken = default);

    /// <summary>Get wallet by user ID</summary>
    Task<UserWallet?> GetWalletByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get wallet by ID</summary>
    Task<UserWallet?> GetWalletByIdAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Get wallet balance</summary>
    Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Add funds to wallet</summary>
    Task<WalletTransaction> AddFundsAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken cancellationToken = default);

    /// <summary>Deduct funds from wallet</summary>
    Task<WalletTransaction> DeductFundsAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken cancellationToken = default);

    /// <summary>Transfer funds between wallets</summary>
    Task<(WalletTransaction debitTransaction, WalletTransaction creditTransaction)> TransferFundsAsync(
        Guid fromUserId,
        Guid toUserId,
        decimal amount,
        string description,
        string? referenceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lock a wallet</summary>
    Task LockWalletAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Unlock a wallet</summary>
    Task UnlockWalletAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Get transaction history</summary>
    Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        WalletTransactionType? typeFilter = null,
        TransactionStatus? statusFilter = null,
        CancellationToken cancellationToken = default);
}
