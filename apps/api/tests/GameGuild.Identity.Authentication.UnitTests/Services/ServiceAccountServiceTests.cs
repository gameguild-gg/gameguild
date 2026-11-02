using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class ServiceAccountServiceTests
{
    private readonly Mock<IServiceAccountRepository> _repositoryMock;
    private readonly Mock<IRefreshTokenHasher> _hasherMock;
    private readonly Mock<ILogger<ServiceAccountService>> _loggerMock;
    private readonly ServiceAccountService _service;

    public ServiceAccountServiceTests()
    {
        _repositoryMock = new Mock<IServiceAccountRepository>();
        _hasherMock = new Mock<IRefreshTokenHasher>();
        _loggerMock = new Mock<ILogger<ServiceAccountService>>();
        _service = new ServiceAccountService(_repositoryMock.Object, _hasherMock.Object, _loggerMock.Object);
    }

    #region CreateServiceAccountAsync Tests

    [Fact]
    public async Task CreateServiceAccountAsync_ShouldGenerateUniqueClientId()
    {
        // Arrange
        var name = "Test Service Account";
        var tenantId = Guid.NewGuid();
        var scopes = "read:data write:data";
        var createdBy = "admin@example.com";

        _repositoryMock.Setup(x => x.ClientIdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);
        _hasherMock.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns("hashed_secret");

        // Act
        var (account, secret) = await _service.CreateServiceAccountAsync(
            name, null, tenantId, scopes, createdBy);

        // Assert
        account.Should().NotBeNull();
        account.ClientId.Should().StartWith("svc_");
        account.Name.Should().Be(name);
        account.Scopes.Should().Be(scopes);
        account.IsActive.Should().BeTrue();
        secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateServiceAccountAsync_ShouldRetryIfClientIdExists()
    {
        // Arrange
        var name = "Test Service Account";
        var tenantId = Guid.NewGuid();
        var scopes = "read:data";
        var createdBy = "admin@example.com";

        var callCount = 0;
        _repositoryMock.Setup(x => x.ClientIdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ < 1); // First call returns true, second returns false
        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);
        _hasherMock.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns("hashed_secret");

        // Act
        var (account, secret) = await _service.CreateServiceAccountAsync(
            name, null, tenantId, scopes, createdBy);

        // Assert
        _repositoryMock.Verify(x => x.ClientIdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(2));
    }

    [Fact]
    public async Task CreateServiceAccountAsync_WithExpirationDate_ShouldSetExpiresAt()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);
        
        _repositoryMock.Setup(x => x.ClientIdExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);
        _hasherMock.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns("hashed_secret");

        // Act
        var (account, _) = await _service.CreateServiceAccountAsync(
            "Test", null, Guid.NewGuid(), "read", "admin", expiresAt: expiresAt);

        // Assert
        account.ExpiresAt.Should().Be(expiresAt);
    }

    #endregion

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnAccount()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "test_secret";
        var ipAddress = "192.168.1.1";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = true,
            IsLocked = false,
            ExpiresAt = null
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _hasherMock.Setup(x => x.VerifyToken(clientSecret, serviceAccount.ClientSecretHash))
            .Returns(true);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(serviceAccount);
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa => 
            sa.LastAuthenticatedAt != null && sa.LastAuthenticatedFromIp == ipAddress), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentClientId_ShouldReturnNull()
    {
        // Arrange
        var clientId = "svc_nonexistent";
        var clientSecret = "test_secret";

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount?)null);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveAccount_ShouldReturnNull()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "test_secret";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = false, // Inactive
            IsLocked = false,
            ExpiresAt = null
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithLockedAccount_ShouldReturnNull()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "test_secret";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = true,
            IsLocked = true, // Locked
            ExpiresAt = null
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithExpiredAccount_ShouldReturnNull()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "test_secret";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = true,
            IsLocked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidSecret_ShouldRecordFailure()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "wrong_secret";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = true,
            IsLocked = false,
            ExpiresAt = null,
            FailedAuthenticationAttempts = 0
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _hasherMock.Setup(x => x.VerifyToken(clientSecret, serviceAccount.ClientSecretHash))
            .Returns(false);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, null);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa => 
            sa.FailedAuthenticationAttempts == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithDisallowedIp_ShouldRecordFailure()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "test_secret";
        var ipAddress = "192.168.1.100";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = true,
            IsLocked = false,
            ExpiresAt = null,
            AllowedIpAddresses = "192.168.1.1,192.168.1.2", // Different IPs
            FailedAuthenticationAttempts = 0
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, ipAddress);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa => 
            sa.FailedAuthenticationAttempts == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithAllowedIp_ShouldSucceed()
    {
        // Arrange
        var clientId = "svc_test123";
        var clientSecret = "test_secret";
        var ipAddress = "192.168.1.1";
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = "hashed_secret",
            IsActive = true,
            IsLocked = false,
            ExpiresAt = null,
            AllowedIpAddresses = "192.168.1.1,192.168.1.2" // Allowed
        };

        _repositoryMock.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _hasherMock.Setup(x => x.VerifyToken(clientSecret, serviceAccount.ClientSecretHash))
            .Returns(true);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        var result = await _service.AuthenticateAsync(clientId, clientSecret, ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(serviceAccount);
    }

    #endregion

    #region RotateSecretAsync Tests

    [Fact]
    public async Task RotateSecretAsync_ShouldGenerateNewSecretAndUpdateAccount()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            ClientSecretHash = "old_hash",
            SecretRotationCount = 0
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _hasherMock.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns("new_hash");
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        var newSecret = await _service.RotateSecretAsync(serviceAccountId);

        // Assert
        newSecret.Should().NotBeNullOrEmpty();
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa =>
            sa.ClientSecretHash == "new_hash" && 
            sa.SecretRotationCount == 1 &&
            sa.SecretRotatedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateSecretAsync_WithNonExistentAccount_ShouldThrowException()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RotateSecretAsync(serviceAccountId));
    }

    #endregion

    #region Lock/Unlock Tests

    [Fact]
    public async Task LockAsync_ShouldLockAccountWithReason()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var reason = "Suspicious activity detected";
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            IsLocked = false
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        await _service.LockAsync(serviceAccountId, reason);

        // Assert
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa =>
            sa.IsLocked && sa.LockedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockAsync_ShouldUnlockAccount()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            IsLocked = true,
            LockedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        await _service.UnlockAsync(serviceAccountId);

        // Assert
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa =>
            !sa.IsLocked && sa.LockedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public async Task DeactivateAsync_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        await _service.DeactivateAsync(serviceAccountId);

        // Assert
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa =>
            !sa.IsActive && sa.UpdatedAt > DateTime.UtcNow.AddSeconds(-5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            IsActive = false
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        await _service.ReactivateAsync(serviceAccountId);

        // Assert
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa =>
            sa.IsActive && sa.UpdatedAt > DateTime.UtcNow.AddSeconds(-5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateScopes Tests

    [Fact]
    public async Task UpdateScopesAsync_ShouldUpdateScopesAndTimestamp()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var newScopes = "read:admin write:admin delete:data";
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            Scopes = "read:data"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ServiceAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceAccount sa, CancellationToken ct) => sa);

        // Act
        await _service.UpdateScopesAsync(serviceAccountId, newScopes);

        // Assert
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<ServiceAccount>(sa =>
            sa.Scopes == newScopes && sa.UpdatedAt > DateTime.UtcNow.AddSeconds(-5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAccount()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var serviceAccount = new ServiceAccount { Id = serviceAccountId };

        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);

        // Act
        var result = await _service.GetByIdAsync(serviceAccountId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(serviceAccount);
    }

    [Fact]
    public async Task GetByTenantAsync_ShouldReturnAccountsForTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var accounts = new List<ServiceAccount>
        {
            new ServiceAccount { Id = Guid.NewGuid(), TenantId = tenantId },
            new ServiceAccount { Id = Guid.NewGuid(), TenantId = tenantId }
        };

        _repositoryMock.Setup(x => x.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        // Act
        var result = await _service.GetByTenantAsync(tenantId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(accounts);
    }

    #endregion

    #region GetAuditLogAsync Tests

    [Fact]
    public async Task GetAuditLogAsync_ShouldReturnDerivedAuditEntries()
    {
        // Arrange
        var serviceAccountId = Guid.NewGuid();
        var serviceAccount = new ServiceAccount
        {
            Id = serviceAccountId,
            Name = "API Worker",
            Scopes = "read:users",
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            LastAuthenticatedAt = DateTime.UtcNow.AddHours(-2),
            LastAuthenticatedFromIp = "127.0.0.1",
            AuthenticationCount = 3,
            SecretRotatedAt = DateTime.UtcNow.AddHours(-1),
            SecretRotationCount = 1
        };
        _repositoryMock.Setup(x => x.GetByIdAsync(serviceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceAccount);

        // Act
        var result = await _service.GetAuditLogAsync(serviceAccountId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().Contain(x => x.Action == "Created");
        result.Items.Should().Contain(x => x.Action == "Authenticated");
        result.Items.Should().Contain(x => x.Action == "SecretRotated");
        result.TotalCount.Should().BeGreaterThan(0);
    }

    #endregion
}
