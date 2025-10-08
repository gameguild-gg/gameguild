using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameGuild.Modules.Common.Secrets;

/// <summary>
/// Unified secret management service with caching and multi-provider support.
/// </summary>
public interface ISecretService
{
    /// <summary>
    /// Gets a secret value with caching support.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="version">Optional version. If null, gets the latest version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value, or null if not found.</returns>
    Task<string?> GetSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a secret with full metadata.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="version">Optional version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret object, or null if not found.</returns>
    Task<Secret?> GetSecretWithMetadataAsync(string key, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a secret.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="value">The secret value.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetSecretAsync(
        string key,
        string value,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret and invalidates cache.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret keys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of secret keys.</returns>
    Task<IReadOnlyList<string>> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache for a specific secret.
    /// </summary>
    /// <param name="key">The secret key.</param>
    void InvalidateCache(string key);

    /// <summary>
    /// Clears all cached secrets.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Rotates a secret by generating a new value and updating the vault.
    /// </summary>
    /// <param name="key">The secret key to rotate.</param>
    /// <param name="valueGenerator">Function to generate the new secret value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RotateSecretAsync(string key, Func<string> valueGenerator, CancellationToken cancellationToken = default);
}
