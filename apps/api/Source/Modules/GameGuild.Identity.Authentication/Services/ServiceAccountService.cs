using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for managing service accounts and client credentials authentication.
/// </summary>
public sealed class ServiceAccountService : IServiceAccountService
{
    private readonly IServiceAccountRepository _repository;
    private readonly IRefreshTokenHasher _hasher;
    private readonly ILogger<ServiceAccountService> _logger;
    private const int ClientSecretLength = 64;
    private const int LockThreshold = 10;

    public ServiceAccountService(
        IServiceAccountRepository repository,
        IRefreshTokenHasher hasher,
        ILogger<ServiceAccountService> logger)
    {
        _repository = repository;
        _hasher = hasher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(ServiceAccount Account, string ClientSecret)> CreateServiceAccountAsync(
        string name,
        string? description,
        Guid? tenantId,
        string scopes,
        string createdBy,
        string? allowedIpAddresses = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        // Generate unique client ID
        var clientId = $"svc_{Guid.NewGuid():N}";

        // Ensure client ID is unique
        while (await _repository.ClientIdExistsAsync(clientId, cancellationToken))
        {
            clientId = $"svc_{Guid.NewGuid():N}";
        }

        // Generate secure client secret
        var clientSecret = GenerateSecureSecret();
        var clientSecretHash = _hasher.HashToken(clientSecret);

        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            Name = name,
            Description = description,
            TenantId = tenantId,
            Scopes = scopes,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow,
            AllowedIpAddresses = allowedIpAddresses
        };

        var created = await _repository.CreateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation(
            "Created service account {ServiceAccountId} with client ID {ClientId} for tenant {TenantId}",
            created.Id, created.ClientId, tenantId);

        return (created, clientSecret);
    }

    /// <inheritdoc />
    public async Task<ServiceAccount?> AuthenticateAsync(
        string clientId,
        string clientSecret,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var serviceAccount = await _repository.GetByClientIdAsync(clientId, cancellationToken);

        if (serviceAccount == null)
        {
            _logger.LogWarning("Authentication failed: service account with client ID {ClientId} not found", clientId);
            return null;
        }

        if (!serviceAccount.CanAuthenticate)
        {
            _logger.LogWarning(
                "Authentication failed: service account {ServiceAccountId} cannot authenticate (IsActive={IsActive}, IsLocked={IsLocked}, Expired={Expired})",
                serviceAccount.Id, serviceAccount.IsActive, serviceAccount.IsLocked,
                serviceAccount.ExpiresAt.HasValue && serviceAccount.ExpiresAt <= DateTime.UtcNow);
            return null;
        }

        // Validate IP address if restrictions are set
        if (!string.IsNullOrEmpty(serviceAccount.AllowedIpAddresses) && !string.IsNullOrEmpty(ipAddress))
        {
            if (!IsIpAllowed(ipAddress, serviceAccount.AllowedIpAddresses))
            {
                _logger.LogWarning(
                    "Authentication failed: IP {IpAddress} not in allowed list for service account {ServiceAccountId}",
                    ipAddress, serviceAccount.Id);
                serviceAccount.RecordFailedAuthentication(LockThreshold);
                await _repository.UpdateAsync(serviceAccount, cancellationToken);
                return null;
            }
        }

        // Verify client secret
        if (!_hasher.VerifyToken(clientSecret, serviceAccount.ClientSecretHash))
        {
            _logger.LogWarning(
                "Authentication failed: invalid client secret for service account {ServiceAccountId}",
                serviceAccount.Id);
            serviceAccount.RecordFailedAuthentication(LockThreshold);
            await _repository.UpdateAsync(serviceAccount, cancellationToken);
            return null;
        }

        // Record successful authentication
        serviceAccount.RecordSuccessfulAuthentication(ipAddress);
        await _repository.UpdateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation(
            "Service account {ServiceAccountId} authenticated successfully from IP {IpAddress}",
            serviceAccount.Id, ipAddress);

        return serviceAccount;
    }

    /// <inheritdoc />
    public async Task<string> RotateSecretAsync(Guid serviceAccountId, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await _repository.GetByIdAsync(serviceAccountId, cancellationToken)
                             ?? throw new InvalidOperationException($"Service account {serviceAccountId} not found");

        var newSecret = GenerateSecureSecret();
        var newSecretHash = _hasher.HashToken(newSecret);

        serviceAccount.RotateSecret(newSecretHash);
        await _repository.UpdateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation(
            "Rotated secret for service account {ServiceAccountId} (rotation #{RotationCount})",
            serviceAccountId, serviceAccount.SecretRotationCount);

        return newSecret;
    }

    /// <inheritdoc />
    public async Task UnlockAsync(Guid serviceAccountId, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await _repository.GetByIdAsync(serviceAccountId, cancellationToken)
                             ?? throw new InvalidOperationException($"Service account {serviceAccountId} not found");

        serviceAccount.Unlock();
        await _repository.UpdateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation("Unlocked service account {ServiceAccountId}", serviceAccountId);
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(Guid serviceAccountId, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await _repository.GetByIdAsync(serviceAccountId, cancellationToken)
                             ?? throw new InvalidOperationException($"Service account {serviceAccountId} not found");

        serviceAccount.IsActive = false;
        serviceAccount.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation("Deactivated service account {ServiceAccountId}", serviceAccountId);
    }

    /// <inheritdoc />
    public async Task ReactivateAsync(Guid serviceAccountId, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await _repository.GetByIdAsync(serviceAccountId, cancellationToken)
                             ?? throw new InvalidOperationException($"Service account {serviceAccountId} not found");

        serviceAccount.IsActive = true;
        serviceAccount.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation("Reactivated service account {ServiceAccountId}", serviceAccountId);
    }

    /// <inheritdoc />
    public async Task UpdateScopesAsync(Guid serviceAccountId, string scopes, CancellationToken cancellationToken = default)
    {
        var serviceAccount = await _repository.GetByIdAsync(serviceAccountId, cancellationToken)
                             ?? throw new InvalidOperationException($"Service account {serviceAccountId} not found");

        serviceAccount.Scopes = scopes;
        serviceAccount.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(serviceAccount, cancellationToken);

        _logger.LogInformation(
            "Updated scopes for service account {ServiceAccountId} to: {Scopes}",
            serviceAccountId, scopes);
    }

    /// <inheritdoc />
    public Task<ServiceAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ServiceAccount>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _repository.GetByTenantIdAsync(tenantId, cancellationToken);

    private static string GenerateSecureSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(ClientSecretLength);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static bool IsIpAllowed(string ipAddress, string allowedIpAddresses)
    {
        var allowed = allowedIpAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var allowedIp in allowed)
        {
            // Simple exact match for now (CIDR support could be added)
            if (allowedIp.Equals(ipAddress, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
