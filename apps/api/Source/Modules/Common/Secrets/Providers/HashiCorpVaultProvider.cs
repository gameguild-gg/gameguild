using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Common.Secrets.Providers;

/// <summary>
/// HashiCorp Vault secret provider implementation using HTTP API.
/// </summary>
public sealed class HashiCorpVaultProvider : ISecretProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HashiCorpVaultProvider> _logger;
    private readonly string _mountPath;

    public string ProviderName => "HashiCorpVault";

    public HashiCorpVaultProvider(
        string vaultAddress,
        string token,
        string mountPath,
        ILogger<HashiCorpVaultProvider> logger,
        HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(vaultAddress);
        ArgumentNullException.ThrowIfNull(token);
        _mountPath = mountPath ?? "secret";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(vaultAddress);
        _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<Secret?> GetSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var path = version != null
                ? $"/v1/{_mountPath}/data/{key}?version={version}"
                : $"/v1/{_mountPath}/data/{key}";

            var response = await _httpClient.GetAsync(path, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Secret '{Key}' not found in HashiCorp Vault", key);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<VaultResponse>(cancellationToken: cancellationToken);
            if (content?.Data?.Data == null)
            {
                return null;
            }

            // Extract the actual secret value (first key-value pair in data)
            var secretData = content.Data.Data;
            var secretValue = secretData.TryGetValue("value", out var val) ? val?.ToString() : null;

            if (secretValue == null)
            {
                return null;
            }

            return new Secret
            {
                Key = key,
                Value = secretValue,
                Version = content.Data.Metadata?.TryGetValue("version", out var v) == true ? v?.ToString() : null,
                CreatedAt = content.Data.Metadata?.TryGetValue("created_time", out var ct) == true && DateTime.TryParse(ct?.ToString(), out var createdAt)
                    ? createdAt
                    : null,
                Metadata = content.Data.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{Key}' from HashiCorp Vault", key);
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
            var path = $"/v1/{_mountPath}/data/{key}";

            var payload = new
            {
                data = new Dictionary<string, object>
                {
                    ["value"] = value
                },
                options = metadata != null ? new Dictionary<string, object> { ["metadata"] = metadata } : null
            };

            var response = await _httpClient.PostAsJsonAsync(path, payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<VaultResponse>(cancellationToken: cancellationToken);

            _logger.LogInformation("Secret '{Key}' set in HashiCorp Vault", key);

            return new Secret
            {
                Key = key,
                Value = value,
                Version = content?.Data?.Metadata?.TryGetValue("version", out var v) == true ? v?.ToString() : null,
                CreatedAt = DateTime.UtcNow,
                Metadata = metadata
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to set secret '{Key}' in HashiCorp Vault", key);
            throw;
        }
    }

    public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var path = $"/v1/{_mountPath}/metadata/{key}";
            var response = await _httpClient.DeleteAsync(path, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Secret '{Key}' not found in HashiCorp Vault", key);
                return;
            }

            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Secret '{Key}' deleted from HashiCorp Vault", key);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete secret '{Key}' from HashiCorp Vault", key);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var path = $"/v1/{_mountPath}/metadata?list=true";
            var response = await _httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<VaultListResponse>(cancellationToken: cancellationToken);
            var keys = content?.Data?.Keys ?? new List<string>();

            _logger.LogInformation("Listed {Count} secrets from HashiCorp Vault", keys.Count);
            return keys;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list secrets from HashiCorp Vault");
            throw;
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/sys/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HashiCorp Vault health check failed");
            return false;
        }
    }

    // Internal DTOs for Vault API responses
    private sealed class VaultResponse
    {
        public VaultData? Data { get; set; }
    }

    private sealed class VaultData
    {
        public Dictionary<string, object?>? Data { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
    }

    private sealed class VaultListResponse
    {
        public VaultListData? Data { get; set; }
    }

    private sealed class VaultListData
    {
        public List<string> Keys { get; set; } = new();
    }
}
