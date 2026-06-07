using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Repository interface for UserPreferences
/// </summary>
public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserPreferences?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     EntityFramework implementation of UserPreferences repository
/// </summary>
public class UserPreferencesRepository(IApplicationDbContext context) : IUserPreferencesRepository
{
    public async Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserPreferences>().FirstOrDefaultAsync(up => up.UserId == userId && up.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserPreferences?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserPreferences>().FirstOrDefaultAsync(up => up.Id == id && up.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default) { await context.Set<UserPreferences>().AddAsync(preferences, cancellationToken).ConfigureAwait(false); }

    public Task UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        if (context is DbContext dbContext && dbContext.Entry(preferences).State != EntityState.Detached)
        {
            return Task.CompletedTask;
        }

        context.Set<UserPreferences>().Update(preferences);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        preferences.SoftDelete();
        if (context is DbContext dbContext && dbContext.Entry(preferences).State != EntityState.Detached)
        {
            return Task.CompletedTask;
        }

        context.Set<UserPreferences>().Update(preferences);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
