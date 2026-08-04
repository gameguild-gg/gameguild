namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository for managing ExternalLogin links between GameGuild users and external identity providers.
/// </summary>
public interface IExternalLoginRepository
{
    /// <summary>
    ///     Looks up an external login by its provider and provider-scoped key.
    /// </summary>
    Task<ExternalLogin?> GetByProviderKeyAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all external logins linked to the given user.
    /// </summary>
    Task<List<ExternalLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Inserts a new external login, or — if a row already exists for the same
    ///     (Provider, ProviderKey) — updates the linked UserId on the existing row.
    ///     Idempotent under repeated calls with the same (Provider, ProviderKey).
    /// </summary>
    Task<ExternalLogin> UpsertAsync(ExternalLogin externalLogin, CancellationToken cancellationToken = default);
}
