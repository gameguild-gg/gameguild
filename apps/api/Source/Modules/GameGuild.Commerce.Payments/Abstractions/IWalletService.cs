namespace GameGuild.Commerce.Payments;

/// <summary>
///     Service for managing user wallets
/// </summary>
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
        CancellationToken cancellationToken = default
    );

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
        CancellationToken cancellationToken = default
    );

    /// <summary>List all wallets with pagination and filtering (admin)</summary>
    Task<(List<UserWallet> Wallets, int TotalCount)> ListWalletsAsync(
        int page,
        int pageSize,
        string? currency = null,
        bool? isFrozen = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Update wallet settings</summary>
    Task UpdateWalletSettingsAsync(
        Guid walletId,
        string? currency = null,
        decimal? dailyLimit = null,
        decimal? monthlyLimit = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Close/delete a wallet (requires zero balance)</summary>
    Task CloseWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Freeze a wallet by ID</summary>
    Task FreezeWalletAsync(Guid walletId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Unfreeze a wallet by ID</summary>
    Task UnfreezeWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Get wallet audit log</summary>
    Task<(List<WalletTransaction> Transactions, int TotalCount)> GetWalletAuditLogAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
