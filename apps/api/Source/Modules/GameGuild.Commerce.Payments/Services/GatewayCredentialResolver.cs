using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Resolves the effective credential for a gateway slot (issue #413): the active secret
///     from the encrypted-at-rest <see cref="IGatewayCredentialStore" /> first, the
///     configuration fallback second. Returns null when neither exists so callers fail
///     closed — the resolver never guesses or substitutes a default credential.
/// </summary>
public interface IGatewayCredentialResolver
{
    /// <summary>
    ///     Resolves the plaintext credential for a (provider, secret name) slot, or null
    ///     when no encrypted-store credential is active and no configuration fallback exists.
    ///     The returned value is a live credential — callers must never log it.
    /// </summary>
    Task<string?> ResolveAsync(
        string providerKey,
        string secretName,
        string? configurationFallback = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Default <see cref="IGatewayCredentialResolver" />: encrypted store first, configuration
///     fallback second. Store read failures degrade to the fallback (a credential-store outage
///     must not take charging down), which is logged.
/// </summary>
public sealed class GatewayCredentialResolver(
    IGatewayCredentialStore credentialStore,
    ILogger<GatewayCredentialResolver> logger) : IGatewayCredentialResolver
{
    /// <inheritdoc />
    public async Task<string?> ResolveAsync(
        string providerKey,
        string secretName,
        string? configurationFallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        try
        {
            var secret = await credentialStore
                .GetActiveAsync(providerKey, secretName, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(secret?.Plaintext))
            {
                return secret.Plaintext;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Fail toward the fallback rather than throwing: the encrypted store being
            // unavailable must not disable gateways whose credentials are still configured.
            logger.LogWarning(
                exception,
                "Reading the encrypted {ProviderKey}/{SecretName} credential failed; falling back to configuration",
                providerKey,
                secretName);
        }

        return string.IsNullOrWhiteSpace(configurationFallback) ? null : configurationFallback;
    }
}
