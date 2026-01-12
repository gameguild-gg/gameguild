using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Repository interface for UserMetadata
/// </summary>
public interface IUserMetadataRepository
{
    Task<UserMetadata?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(UserMetadata metadata, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserMetadata metadata, CancellationToken cancellationToken = default);

    Task DeleteAsync(UserMetadata metadata, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     EntityFramework implementation of UserMetadata repository
/// </summary>
public class UserMetadataRepository(IApplicationDbContext context) : IUserMetadataRepository
{
    public async Task<UserMetadata?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserMetadata>().FirstOrDefaultAsync(um => um.UserId == userId && um.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Set<UserMetadata>().FirstOrDefaultAsync(um => um.Id == id && um.DeletedAt == null, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(UserMetadata metadata, CancellationToken cancellationToken = default) { await context.Set<UserMetadata>().AddAsync(metadata, cancellationToken).ConfigureAwait(false); }

    public Task UpdateAsync(UserMetadata metadata, CancellationToken cancellationToken = default)
    {
        context.Set<UserMetadata>().Update(metadata);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        metadata.SoftDelete();
        context.Set<UserMetadata>().Update(metadata);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
