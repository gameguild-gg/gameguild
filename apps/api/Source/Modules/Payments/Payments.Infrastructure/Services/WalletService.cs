using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Payments.Infrastructure.Services;

/// <summary>Wallet service implementation</summary>
public class WalletService : IWalletService
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(PaymentsDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserWallet> CreateWalletAsync(Guid userId, string currency = "USD", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating wallet for user {UserId} with currency {Currency}", userId, currency);

        // Check if wallet already exists
        var existing = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (existing != null)
        {
            _logger.LogWarning("Wallet already exists for user {UserId}", userId);
            throw new InvalidOperationException($"Wallet already exists for user {userId}");
        }

        var wallet = new UserWallet
        {
            UserId = userId,
            Currency = currency,
            Balance = 0,
            IsActive = true,
            IsLocked = false
        };

        _context.UserWallets.Add(wallet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet created for user {UserId} with ID {WalletId}", userId, wallet.Id);
        return wallet;
    }

    public async Task<UserWallet?> GetWalletByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserWallets
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(10))
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<UserWallet?> GetWalletByIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _context.UserWallets
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(10))
            .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);
    }

    public async Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        return wallet?.Balance ?? 0;
    }

    public async Task<WalletTransaction> AddFundsAsync(
        Guid userId,
        decimal amount,
        string description,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding {Amount} funds to wallet for user {UserId}", amount, userId);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet == null)
        {
            _logger.LogWarning("Wallet not found for user {UserId}, creating new wallet", userId);
            wallet = await CreateWalletAsync(userId, cancellationToken: cancellationToken);
        }

        wallet.AddFunds(amount, description, referenceId);
        await _context.SaveChangesAsync(cancellationToken);

        var transaction = wallet.Transactions.OrderByDescending(t => t.CreatedAt).First();
        _logger.LogInformation("Funds added: Transaction {TransactionId}, Balance {Balance}", transaction.Id, wallet.Balance);

        return transaction;
    }

    public async Task<WalletTransaction> DeductFundsAsync(
        Guid userId,
        decimal amount,
        string description,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deducting {Amount} funds from wallet for user {UserId}", amount, userId);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet == null)
        {
            _logger.LogError("Wallet not found for user {UserId}", userId);
            throw new InvalidOperationException($"Wallet not found for user {userId}");
        }

        wallet.DeductFunds(amount, description, referenceId);
        await _context.SaveChangesAsync(cancellationToken);

        var transaction = wallet.Transactions.OrderByDescending(t => t.CreatedAt).First();
        _logger.LogInformation("Funds deducted: Transaction {TransactionId}, Balance {Balance}", transaction.Id, wallet.Balance);

        return transaction;
    }

    public async Task<(WalletTransaction debitTransaction, WalletTransaction creditTransaction)> TransferFundsAsync(
        Guid fromUserId,
        Guid toUserId,
        decimal amount,
        string description,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Transferring {Amount} from user {FromUserId} to user {ToUserId}", amount, fromUserId, toUserId);

        // Get both wallets
        var fromWallet = await GetWalletByUserIdAsync(fromUserId, cancellationToken);
        var toWallet = await GetWalletByUserIdAsync(toUserId, cancellationToken);

        if (fromWallet == null)
            throw new InvalidOperationException($"Source wallet not found for user {fromUserId}");
        if (toWallet == null)
        {
            _logger.LogWarning("Destination wallet not found for user {ToUserId}, creating new wallet", toUserId);
            toWallet = await CreateWalletAsync(toUserId, fromWallet.Currency, cancellationToken);
        }

        // Currency check
        if (fromWallet.Currency != toWallet.Currency)
            throw new InvalidOperationException($"Currency mismatch: {fromWallet.Currency} != {toWallet.Currency}");

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

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Transfer completed: Debit {DebitId}, Credit {CreditId}", debitTransaction.Id, creditTransaction.Id);

        return (debitTransaction, creditTransaction);
    }

    public async Task LockWalletAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Locking wallet for user {UserId}: {Reason}", userId, reason);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet == null)
            throw new InvalidOperationException($"Wallet not found for user {userId}");

        wallet.Lock(reason);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet locked for user {UserId}", userId);
    }

    public async Task UnlockWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Unlocking wallet for user {UserId}", userId);

        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet == null)
            throw new InvalidOperationException($"Wallet not found for user {userId}");

        wallet.Unlock();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet unlocked for user {UserId}", userId);
    }

    public async Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        WalletTransactionType? typeFilter = null,
        TransactionStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet == null)
            return new List<WalletTransaction>();

        var query = _context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (typeFilter.HasValue)
            query = query.Where(t => t.Type == typeFilter.Value);

        if (statusFilter.HasValue)
            query = query.Where(t => t.Status == statusFilter.Value);

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
