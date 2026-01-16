using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository implementation for UserWallet entity
/// </summary>
public class WalletRepository(
    IApplicationDbContext context,
    ILogger<WalletRepository> logger) 
    : CommerceRepositoryBase<UserWallet>(context), IWalletRepository
{
    /// <summary>
    ///     Gets the WalletTransactions DbSet
    /// </summary>
    protected DbSet<WalletTransaction> WalletTransactions => Context.Set<WalletTransaction>();

    public new async Task<UserWallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting wallet by ID: {WalletId}", id);
        return await Query
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserWallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting wallet by user ID: {UserId}", userId);
        return await Query
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<UserWallet>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all wallets for user: {UserId}", userId);
        return await Query
            .Where(w => w.UserId == userId)
            .Include(w => w.Transactions)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserWallet?> GetByUserIdAndCurrencyAsync(Guid userId, string currency, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting wallet by user ID {UserId} and currency {Currency}", userId, currency);
        return await Query
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == currency, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(UserWallet wallet)
    {
        logger.LogDebug("Adding wallet for user: {UserId}", wallet.UserId);
        Entities.Add(wallet);
    }

    public void Update(UserWallet wallet)
    {
        logger.LogDebug("Updating wallet: {WalletId}", wallet.Id);
        Entities.Update(wallet);
    }

    public async Task<List<WalletTransaction>> GetTransactionsAsync(
        Guid walletId,
        int skip = 0,
        int take = 50,
        WalletTransactionType? typeFilter = null,
        TransactionStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting transactions for wallet: {WalletId}, skip: {Skip}, take: {Take}", walletId, skip, take);

        var query = WalletTransactions
            .Where(t => t.WalletId == walletId && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (typeFilter.HasValue)
            query = query.Where(t => t.Type == typeFilter.Value);

        if (statusFilter.HasValue)
            query = query.Where(t => t.Status == statusFilter.Value);

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
