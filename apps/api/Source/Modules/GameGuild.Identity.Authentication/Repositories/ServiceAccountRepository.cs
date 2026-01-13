using GameGuild.Abstractions;
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
            .FirstOrDefaultAsync(sa => sa.Id == id && !sa.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ServiceAccount?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .FirstOrDefaultAsync(sa => sa.ClientId == clientId && !sa.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .Where(sa => sa.TenantId == tenantId && !sa.IsDeleted)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetGlobalServiceAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .Where(sa => sa.TenantId == null && !sa.IsDeleted)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .Where(sa => sa.IsActive && !sa.IsDeleted && !sa.IsLockedOut)
            .OrderByDescending(sa => sa.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ServiceAccount> CreateAsync(
        ServiceAccount serviceAccount,
        CancellationToken cancellationToken = default)
    {
        serviceAccount.Id = Guid.NewGuid();
        serviceAccount.CreatedAt = DateTime.UtcNow;
        serviceAccount.UpdatedAt = DateTime.UtcNow;

        ServiceAccounts.Add(serviceAccount);
        await context.SaveChangesAsync(cancellationToken);

        return serviceAccount;
    }

    /// <inheritdoc />
    public async Task<ServiceAccount> UpdateAsync(
        ServiceAccount serviceAccount,
        CancellationToken cancellationToken = default)
    {
        serviceAccount.UpdatedAt = DateTime.UtcNow;

        ServiceAccounts.Update(serviceAccount);
        await context.SaveChangesAsync(cancellationToken);

        return serviceAccount;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await GetByIdAsync(id, cancellationToken);

        if (serviceAccount != null)
        {
            // Soft delete
            serviceAccount.IsDeleted = true;
            serviceAccount.DeletedAt = DateTime.UtcNow;
            serviceAccount.UpdatedAt = DateTime.UtcNow;
            serviceAccount.IsActive = false;

            await UpdateAsync(serviceAccount, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ClientIdExistsAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .AnyAsync(sa => sa.ClientId == clientId && !sa.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveCountByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .CountAsync(sa => sa.TenantId == tenantId && sa.IsActive && !sa.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceAccount>> GetExpiringSecretsAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default)
    {
        return await ServiceAccounts
            .Where(sa => sa.IsActive &&
                         !sa.IsDeleted &&
                         sa.SecretExpiresAt.HasValue &&
                         sa.SecretExpiresAt.Value <= beforeDate)
            .OrderBy(sa => sa.SecretExpiresAt)
            .ToListAsync(cancellationToken);
    }
}
