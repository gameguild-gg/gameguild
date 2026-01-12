using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     EntityBase Framework implementation of the User repository
/// </summary>
public class UserRepository(IApplicationDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) { return await context.Set<User>().Where(u => u.DeletedAt == null).ToListAsync(cancellationToken).ConfigureAwait(false); }

    public async Task<(IEnumerable<User> Users, int TotalCount)> SearchAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Set<User>().Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(searchTerm)) { query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm)); }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var users = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (users, totalCount);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) { await context.Set<User>().AddAsync(user, cancellationToken).ConfigureAwait(false); }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Set<User>().Update(user);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.SoftDelete(); // Soft delete
        context.Set<User>().Update(user);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }

    // Bulk operations
    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().Where(u => ids.Contains(u.Id) && u.DeletedAt == null).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<User>> GetByEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().Where(u => emails.Contains(u.Email) && u.DeletedAt == null).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default) { await context.Set<User>().AddRangeAsync(users, cancellationToken).ConfigureAwait(false); }

    public async Task UpdateRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
    {
        context.Set<User>().UpdateRange(users);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task DeleteRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(users);

        foreach (var user in users)
        {
            user.SoftDelete(); // Assuming soft delete
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().Where(u => u.IsActive && u.DeletedAt == null).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<User>> GetInactiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().Where(u => !u.IsActive && u.DeletedAt == null).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Set<User>().Where(u => u.DeletedAt == null);

        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var skip = (pageNumber - 1) * pageSize;
        var users = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return (users, totalCount);
    }

    public async Task<IDictionary<string, bool>> CheckEmailsExistAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        var existingEmails = await context.Set<User>().Where(u => emails.Contains(u.Email) && u.DeletedAt == null).Select(u => u.Email).ToListAsync(cancellationToken).ConfigureAwait(false);

        return emails.ToDictionary(email => email, email => existingEmails.Contains(email));
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) { return await context.Set<User>().AnyAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken).ConfigureAwait(false); }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Set<User>().AnyAsync(u => u.Email == email && u.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task PurgeAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Set<User>().Remove(user);
        await Task.CompletedTask;
    }

    public async Task PurgeRangeAsync(IEnumerable<User> users, CancellationToken cancellationToken = default)
    {
        context.Set<User>().RemoveRange(users);
        await Task.CompletedTask;
    }

    public IQueryable<User> GetQueryable()
    {
        return context.Set<User>().AsQueryable();
    }
}
