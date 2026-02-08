using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Wallet service implementation using repository abstraction
/// </summary>
public class WalletService(IWalletRepository walletRepository, ILogger<WalletService> logger) : IWalletService
{

    public async Task<UserWallet> CreateWalletAsync(Guid userId, string currency = "USD", CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating wallet for user {UserId} with currency {Currency}", userId, currency);

        // Check if wallet already exists
        var existing = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            logger.LogWarning("Wallet already exists for user {UserId}", userId);

            throw new InvalidOperationException($"Wallet already exists for user {userId}");
        }

        var wallet = new UserWallet { UserId = userId, Currency = currency, Balance = 0, IsActive = true, IsLocked = false };

        walletRepository.Add(wallet);
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet created for user {UserId} with ID {WalletId}", userId, wallet.Id);

        return wallet;
    }

    public async Task<UserWallet?> GetWalletByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await walletRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserWallet?> GetWalletByIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await walletRepository.GetByIdAsync(walletId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        return wallet?.Balance ?? 0;
    }

    public async Task<WalletTransaction> AddFundsAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding {Amount} funds to wallet for user {UserId}", amount, userId);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (wallet == null)
        {
            logger.LogWarning("Wallet not found for user {UserId}, creating new wallet", userId);
            wallet = await CreateWalletAsync(userId, cancellationToken : cancellationToken).ConfigureAwait(false);
        }

        wallet.AddFunds(amount, description, referenceId);
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var transaction = wallet.Transactions.OrderByDescending(t => t.CreatedAt).First();
        logger.LogInformation("Funds added: Transaction {TransactionId}, Balance {Balance}", transaction.Id, wallet.Balance);

        return transaction;
    }

    public async Task<WalletTransaction> DeductFundsAsync(Guid userId, decimal amount, string description, string? referenceId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deducting {Amount} funds from wallet for user {UserId}", amount, userId);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (wallet == null)
        {
            logger.LogError("Wallet not found for user {UserId}", userId);

            throw new InvalidOperationException($"Wallet not found for user {userId}");
        }

        wallet.DeductFunds(amount, description, referenceId);
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var transaction = wallet.Transactions.OrderByDescending(t => t.CreatedAt).First();
        logger.LogInformation("Funds deducted: Transaction {TransactionId}, Balance {Balance}", transaction.Id, wallet.Balance);

        return transaction;
    }

    public async Task<(WalletTransaction debitTransaction, WalletTransaction creditTransaction)> TransferFundsAsync(
        Guid fromUserId,
        Guid toUserId,
        decimal amount,
        string description,
        string? referenceId = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Transferring {Amount} from user {FromUserId} to user {ToUserId}", amount, fromUserId, toUserId);

        // Get both wallets
        var fromWallet = await GetWalletByUserIdAsync(fromUserId, cancellationToken).ConfigureAwait(false);
        var toWallet = await GetWalletByUserIdAsync(toUserId, cancellationToken).ConfigureAwait(false);

        if (fromWallet == null) throw new InvalidOperationException($"Source wallet not found for user {fromUserId}");

        if (toWallet == null)
        {
            logger.LogWarning("Destination wallet not found for user {ToUserId}, creating new wallet", toUserId);
            toWallet = await CreateWalletAsync(toUserId, fromWallet.Currency, cancellationToken).ConfigureAwait(false);
        }

        // Currency check
        if (fromWallet.Currency != toWallet.Currency) throw new InvalidOperationException($"Currency mismatch: {fromWallet.Currency} != {toWallet.Currency}");

        // Perform transfer
        var debitDescription = $"Transfer out: {description}";
        var creditDescription = $"Transfer in: {description}";

        fromWallet.DeductFunds(amount, debitDescription, referenceId);
        toWallet.AddFunds(amount, creditDescription, referenceId);

        // Update transaction types
        var debitTransaction = fromWallet.Transactions.OrderByDescending(t => t.CreatedAt).First();
        var creditTransaction = toWallet.Transactions.OrderByDescending(t => t.CreatedAt).First();

        debitTransaction.Type = WalletTransactionType.TransferOut;
        creditTransaction.Type = WalletTransactionType.TransferIn;

        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Transfer completed: Debit {DebitId}, Credit {CreditId}", debitTransaction.Id, creditTransaction.Id);

        return (debitTransaction, creditTransaction);
    }

    public async Task LockWalletAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Locking wallet for user {UserId}: {Reason}", userId, reason);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found for user {userId}");

        wallet.Lock(reason);
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet locked for user {UserId}", userId);
    }

    public async Task UnlockWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Unlocking wallet for user {UserId}", userId);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found for user {userId}");

        wallet.Unlock();
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet unlocked for user {UserId}", userId);
    }

    public async Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        WalletTransactionType? typeFilter = null,
        TransactionStatus? statusFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) return new List<WalletTransaction>();

        return await walletRepository.GetTransactionsAsync(wallet.Id, skip, take, typeFilter, statusFilter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(List<UserWallet> Wallets, int TotalCount)> ListWalletsAsync(
        int page,
        int pageSize,
        string? currency = null,
        bool? isFrozen = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing wallets - Page: {Page}, PageSize: {PageSize}, Currency: {Currency}, IsFrozen: {IsFrozen}",
            page, pageSize, currency, isFrozen);

        return await walletRepository.ListWalletsAsync(page, pageSize, currency, isFrozen, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateWalletSettingsAsync(
        Guid walletId,
        string? currency = null,
        decimal? dailyLimit = null,
        decimal? monthlyLimit = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating wallet settings for {WalletId}", walletId);

        var wallet = await GetWalletByIdAsync(walletId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found: {walletId}");

        if (currency != null) wallet.Currency = currency;
        if (dailyLimit.HasValue) wallet.DailyLimit = dailyLimit.Value;
        if (monthlyLimit.HasValue) wallet.MonthlyLimit = monthlyLimit.Value;

        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet settings updated for {WalletId}", walletId);
    }

    public async Task CloseWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Closing wallet {WalletId}", walletId);

        var wallet = await GetWalletByIdAsync(walletId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found: {walletId}");

        if (wallet.Balance != 0)
            throw new InvalidOperationException($"Cannot close wallet with non-zero balance: {wallet.Balance}");

        wallet.IsActive = false;
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet closed: {WalletId}", walletId);
    }

    public async Task FreezeWalletAsync(Guid walletId, string reason, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Freezing wallet {WalletId}: {Reason}", walletId, reason);

        var wallet = await GetWalletByIdAsync(walletId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found: {walletId}");

        wallet.Lock(reason);
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet frozen: {WalletId}", walletId);
    }

    public async Task UnfreezeWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Unfreezing wallet {WalletId}", walletId);

        var wallet = await GetWalletByIdAsync(walletId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found: {walletId}");

        wallet.Unlock();
        await walletRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Wallet unfrozen: {WalletId}", walletId);
    }

    public async Task<(List<WalletTransaction> Transactions, int TotalCount)> GetWalletAuditLogAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting audit log for wallet {WalletId}", walletId);

        var wallet = await GetWalletByIdAsync(walletId, cancellationToken).ConfigureAwait(false);

        if (wallet == null) throw new InvalidOperationException($"Wallet not found: {walletId}");

        var skip = (page - 1) * pageSize;
        var transactions = await walletRepository.GetTransactionsAsync(walletId, skip, pageSize, null, null, cancellationToken).ConfigureAwait(false);
        var totalCount = await walletRepository.GetTransactionCountAsync(walletId, cancellationToken).ConfigureAwait(false);

        return (transactions, totalCount);
    }
}
