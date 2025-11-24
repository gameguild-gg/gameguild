using GameGuild.Authentication.Entities;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Repository for managing AuthUser entities
/// </summary>
public interface IAuthUserRepository
{
    Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AuthUser> CreateAsync(AuthUser user, CancellationToken cancellationToken = default);

    Task UpdateAsync(AuthUser user, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default);
}
