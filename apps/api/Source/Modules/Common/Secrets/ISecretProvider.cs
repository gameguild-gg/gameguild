namespace GameGuild.Modules.Common.Secrets;

/// <summary>
/// Represents a secret value with metadata.
/// </summary>
public sealed record Secret
{
    /// <summary>
    /// Gets the secret key/name.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets the secret value.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the secret version/revision.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the created timestamp.
    /// </summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// Gets the expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Gets additional metadata about the secret.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Defines the contract for secret providers (Azure Key Vault, HashiCorp Vault, etc.).
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Gets the provider name (e.g., "AzureKeyVault", "HashiCorpVault").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets a secret by key.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="version">Optional version. If null, gets the latest version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value, or null if not found.</returns>
    Task<Secret?> GetSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a secret.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="value">The secret value.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created/updated secret.</returns>
    Task<Secret> SetSecretAsync(
        string key,
        string value,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret keys (without values).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of secret keys.</returns>
    Task<IReadOnlyList<string>> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider is healthy and can connect to the vault.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
