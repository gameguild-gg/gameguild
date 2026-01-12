using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for AuthUser entities
/// </summary>
public class AuthUserRepository(IApplicationDbContext context) : IAuthUserRepository
{
    private DbSet<AuthUser> AuthUsers { get => context.Set<AuthUser>(); }

    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await AuthUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<AuthUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await AuthUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken); }

    public async Task<AuthUser> CreateAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        AuthUsers.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task UpdateAsync(AuthUser user, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;

        AuthUsers.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default) { return await AuthUsers.AsNoTracking().AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken); }
}
