using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for service account data access operations.
///     Provides CRUD and query operations for OAuth2 client_credentials service accounts.
/// </summary>
public class ServiceAccountRepository(IApplicationDbContext context) : IServiceAccountRepository
{
    private DbSet<ServiceAccount> ServiceAccounts => context.Set<ServiceAccount>();

    /// <inheritdoc />
    public async Task<ServiceAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .FirstOrDefaultAsync(sa => sa.Id == id && sa.IsActive, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ServiceAccount?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .FirstOrDefaultAsync(sa => sa.ClientId == clientId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .Where(sa => sa.TenantId == tenantId)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetGlobalServiceAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .Where(sa => sa.TenantId == null)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .OrderByDescending(sa => sa.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ServiceAccount> CreateAsync(
        ServiceAccount serviceAccount,
        CancellationToken cancellationToken = default)
    {
        serviceAccount.Id = Guid.NewGuid();
        serviceAccount.UpdatedAt = SystemClock.UtcNow;

        ServiceAccounts.Add(serviceAccount);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return serviceAccount;
    }

    /// <inheritdoc />
    public async Task<ServiceAccount> UpdateAsync(
        ServiceAccount serviceAccount,
        CancellationToken cancellationToken = default)
    {
        serviceAccount.UpdatedAt = SystemClock.UtcNow;

        ServiceAccounts.Update(serviceAccount);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return serviceAccount;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (serviceAccount != null)
        {
            // Soft delete by deactivating
            serviceAccount.IsActive = false;
            serviceAccount.UpdatedAt = SystemClock.UtcNow;

            await UpdateAsync(serviceAccount, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ClientIdExistsAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .AnyAsync(sa => sa.ClientId == clientId, cancellationToken).ConfigureAwait(false);
    }
}
