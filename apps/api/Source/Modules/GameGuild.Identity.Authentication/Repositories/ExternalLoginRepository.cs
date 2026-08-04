using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for ExternalLogin data access operations.
/// </summary>
public class ExternalLoginRepository(IApplicationDbContext context) : IExternalLoginRepository
{
    private DbSet<ExternalLogin> ExternalLogins { get => context.Set<ExternalLogin>(); }

    public async Task<ExternalLogin?> GetByProviderKeyAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        return await ExternalLogins.FirstOrDefaultAsync(
            e => e.Provider == provider && e.ProviderKey == providerKey,
            cancellationToken);
    }

    public async Task<List<ExternalLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await ExternalLogins
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExternalLogin> UpsertAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default)
    {
        var existing = await ExternalLogins.FirstOrDefaultAsync(
            e => e.Provider == externalLogin.Provider && e.ProviderKey == externalLogin.ProviderKey,
            cancellationToken);

        var now = SystemClock.UtcNow;

        if (existing == null)
        {
            externalLogin.Id = Guid.NewGuid();
            externalLogin.CreatedAt = now;
            externalLogin.UpdatedAt = now;

            ExternalLogins.Add(externalLogin);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return externalLogin;
        }

        existing.UserId = externalLogin.UserId;
        existing.UpdatedAt = now;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing;
    }
}
