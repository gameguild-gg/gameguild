using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Common.Secrets.Providers;

/// <summary>
/// Azure Key Vault secret provider implementation.
/// </summary>
public sealed class AzureKeyVaultProvider : ISecretProvider
{
    private readonly SecretClient _client;
    private readonly ILogger<AzureKeyVaultProvider> _logger;

    public string ProviderName => "AzureKeyVault";

    public AzureKeyVaultProvider(string vaultUri, ILogger<AzureKeyVaultProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(vaultUri);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Use DefaultAzureCredential for authentication
        var credential = new DefaultAzureCredential();
        _client = new SecretClient(new Uri(vaultUri), credential);
    }

    public AzureKeyVaultProvider(SecretClient client, ILogger<AzureKeyVaultProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Secret?> GetSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            KeyVaultSecret secret;
            if (version != null)
            {
                secret = await _client.GetSecretAsync(key, version, cancellationToken);
            }
            else
            {
                secret = await _client.GetSecretAsync(key, cancellationToken: cancellationToken);
            }

            return new Secret
            {
                Key = secret.Name,
                Value = secret.Value,
                Version = secret.Properties.Version,
                CreatedAt = secret.Properties.CreatedOn?.UtcDateTime,
                ExpiresAt = secret.Properties.ExpiresOn?.UtcDateTime,
                Metadata = secret.Properties.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{Key}' not found in Azure Key Vault", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{Key}' from Azure Key Vault", key);
            throw;
        }
    }

    public async Task<Secret> SetSecretAsync(
        string key,
        string value,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var secretOptions = new KeyVaultSecret(key, value);

            // Add tags/metadata
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    secretOptions.Properties.Tags[kvp.Key] = kvp.Value;
                }
            }

            var response = await _client.SetSecretAsync(secretOptions, cancellationToken);
            var secret = response.Value;

            _logger.LogInformation("Secret '{Key}' set in Azure Key Vault", key);

            return new Secret
            {
                Key = secret.Name,
                Value = secret.Value,
                Version = secret.Properties.Version,
                CreatedAt = secret.Properties.CreatedOn?.UtcDateTime,
                ExpiresAt = secret.Properties.ExpiresOn?.UtcDateTime,
                Metadata = secret.Properties.Tags?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret '{Key}' in Azure Key Vault", key);
            throw;
        }
    }

    public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var operation = await _client.StartDeleteSecretAsync(key, cancellationToken);
            await operation.WaitForCompletionAsync(cancellationToken);

            _logger.LogInformation("Secret '{Key}' deleted from Azure Key Vault", key);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{Key}' not found in Azure Key Vault", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret '{Key}' from Azure Key Vault", key);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var secrets = new List<string>();
            await foreach (var secretProperties in _client.GetPropertiesOfSecretsAsync(cancellationToken))
            {
                secrets.Add(secretProperties.Name);
            }

            _logger.LogInformation("Listed {Count} secrets from Azure Key Vault", secrets.Count);
            return secrets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secrets from Azure Key Vault");
            throw;
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to list secrets as a health check
            await foreach (var _ in _client.GetPropertiesOfSecretsAsync(cancellationToken).Take(1))
            {
                break;
            }

            _logger.LogDebug("Azure Key Vault health check passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Key Vault health check failed");
            return false;
        }
    }
}
