using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
/// Unit tests for MfaService - Multi-Factor Authentication service
/// Tests MFA setup, verification, backup codes, TOTP, and lockout mechanisms
/// </summary>
public class MfaServiceTests
{
    private readonly Mock<ILogger<MfaService>> _loggerMock;
    private readonly Mock<IUserMfaConfigurationRepository> _mfaConfigRepositoryMock;
    private readonly Mock<IMfaAttemptRepository> _mfaAttemptRepositoryMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly MfaService _service;

    public MfaServiceTests()
    {
        _loggerMock = new Mock<ILogger<MfaService>>();
        _mfaConfigRepositoryMock = new Mock<IUserMfaConfigurationRepository>();
        _mfaAttemptRepositoryMock = new Mock<IMfaAttemptRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();

        _service = new MfaService(
            _loggerMock.Object,
            _mfaConfigRepositoryMock.Object,
            _mfaAttemptRepositoryMock.Object,
            _encryptionServiceMock.Object);
    }

    #region GetMfaConfigurationAsync Tests

    [Fact]
    public async Task GetMfaConfigurationAsync_WhenNoConfig_ShouldReturnDisabledResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMfaConfiguration?)null);

        // Act
        var result = await _service.GetMfaConfigurationAsync(userId);

        // Assert
        result.IsEnabled.Should().BeFalse();
        result.EnabledMethods.Should().BeEmpty();
        result.EnabledAt.Should().BeNull();
        result.BackupCodesRemaining.Should().Be(0);
    }

    [Fact]
    public async Task GetMfaConfigurationAsync_WhenMfaDisabled_ShouldReturnDisabledResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = false,
            TotpSecretKey = "encrypted_secret",
            BackupCodes = "code1,code2,code3"
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.GetMfaConfigurationAsync(userId);

        // Assert
        result.IsEnabled.Should().BeFalse();
        result.EnabledMethods.Should().BeEmpty();
        result.BackupCodesRemaining.Should().Be(0);
    }

    [Fact]
    public async Task GetMfaConfigurationAsync_WhenMfaEnabledWithTotpAndBackupCodes_ShouldReturnBothMethods()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var enabledAt = DateTime.UtcNow.AddDays(-7);
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            TotpSecretKey = "encrypted_secret_key",
            BackupCodes = "code1,code2,code3,code4,code5",
            EnabledAt = enabledAt
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.GetMfaConfigurationAsync(userId);

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.EnabledMethods.Should().Contain("Totp");
        result.EnabledMethods.Should().Contain("BackupCode");
        result.EnabledMethods.Should().HaveCount(2);
        result.EnabledAt.Should().Be(enabledAt);
        result.BackupCodesRemaining.Should().Be(5);
    }

    [Fact]
    public async Task GetMfaConfigurationAsync_WhenOnlyTotpEnabled_ShouldReturnOnlyTotpMethod()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            TotpSecretKey = "encrypted_secret_key",
            BackupCodes = null,
            EnabledAt = DateTime.UtcNow
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.GetMfaConfigurationAsync(userId);

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.EnabledMethods.Should().ContainSingle();
        result.EnabledMethods.Should().Contain("Totp");
        result.BackupCodesRemaining.Should().Be(0);
    }

    #endregion

    #region GenerateBackupCodesAsync Tests

    [Fact]
    public async Task GenerateBackupCodesAsync_WhenMfaNotEnabled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = false
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        Func<Task> act = async () => await _service.GenerateBackupCodesAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MFA must be enabled to generate backup codes");
    }

    [Fact]
    public async Task GenerateBackupCodesAsync_WhenMfaEnabled_ShouldGenerate10Codes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            TotpSecretKey = "secret"
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        _mfaConfigRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration());

        // Act
        var codes = await _service.GenerateBackupCodesAsync(userId);

        // Assert
        codes.Should().HaveCount(10);
        codes.Should().OnlyContain(code => !string.IsNullOrWhiteSpace(code));
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GenerateBackupCodesAsync_WhenMfaEnabled_ShouldUpdateRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            TotpSecretKey = "secret",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        _mfaConfigRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration());

        // Act
        await _service.GenerateBackupCodesAsync(userId);

        // Assert
        _mfaConfigRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<UserMfaConfiguration>(config => 
                    config.UserId == userId &&
                    !string.IsNullOrEmpty(config.BackupCodes) &&
                    config.UpdatedAt > DateTime.UtcNow.AddMinutes(-1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region SetupTotpAsync Tests

    [Fact]
    public async Task SetupTotpAsync_WhenCalled_ShouldGenerateSecretKeyAndQrCodeUri()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string value) => $"encrypted_{value}");

        _mfaConfigRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration());

        // Act
        var result = await _service.SetupTotpAsync(userId, userEmail);

        // Assert
        result.SecretKey.Should().NotBeNullOrWhiteSpace();
        result.SecretKey.Length.Should().BeGreaterOrEqualTo(16);
        result.QrCodeUri.Should().NotBeNullOrWhiteSpace();
        result.QrCodeUri.Should().Contain("otpauth://totp/");
        result.QrCodeUri.Should().Contain("test%40example.com"); // URL-encoded email
    }

    [Fact]
    public async Task SetupTotpAsync_WhenCalled_ShouldUpdateRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string value) => $"encrypted_{value}");

        _mfaConfigRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration());

        // Act
        await _service.SetupTotpAsync(userId, userEmail);

        // Assert
        _mfaConfigRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<UserMfaConfiguration>(config =>
                    config.UserId == userId &&
                    !string.IsNullOrEmpty(config.TotpSecretKey)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _encryptionServiceMock.Verify(
            x => x.Encrypt(It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region DisableMfaAsync Tests

    [Fact]
    public async Task DisableMfaAsync_WhenMfaConfigExists_ShouldDisableAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            TotpSecretKey = "secret",
            BackupCodes = "codes"
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        _mfaConfigRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration());

        // Act
        var result = await _service.DisableMfaAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mfaConfigRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<UserMfaConfiguration>(config =>
                    config.UserId == userId &&
                    !config.IsEnabled &&
                    config.TotpSecretKey == null &&
                    config.BackupCodes == null &&
                    config.FailedAttempts == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DisableMfaAsync_WhenNoMfaConfig_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMfaConfiguration?)null);

        // Act
        var result = await _service.DisableMfaAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mfaConfigRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetMfaStatusAsync Tests

    [Fact]
    public async Task GetMfaStatusAsync_WhenMfaEnabled_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.GetMfaStatusAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetMfaStatusAsync_WhenMfaDisabled_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = false
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.GetMfaStatusAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetMfaStatusAsync_WhenNoConfig_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMfaConfiguration?)null);

        // Act
        var result = await _service.GetMfaStatusAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsUserLockedOutAsync Tests

    [Fact]
    public async Task IsUserLockedOutAsync_WhenNotLockedOut_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            LockedOutUntil = null
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.IsUserLockedOutAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserLockedOutAsync_WhenLockedOutInPast_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            LockedOutUntil = DateTime.UtcNow.AddMinutes(-10) // Lockout expired
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.IsUserLockedOutAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserLockedOutAsync_WhenCurrentlyLockedOut_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            FailedAttempts = 5, // MaxFailedAttempts
            LockedOutUntil = DateTime.UtcNow.AddMinutes(10) // Still locked out
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        // Act
        var result = await _service.IsUserLockedOutAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ResetFailedAttemptsAsync Tests

    [Fact]
    public async Task ResetFailedAttemptsAsync_WhenMfaConfigExists_ShouldResetAttemptsAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mfaConfig = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsEnabled = true,
            FailedAttempts = 3,
            LockedOutUntil = DateTime.UtcNow.AddMinutes(5)
        };

        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mfaConfig);

        _mfaConfigRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration());

        // Act
        var result = await _service.ResetFailedAttemptsAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mfaConfigRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.Is<UserMfaConfiguration>(config =>
                    config.UserId == userId &&
                    config.FailedAttempts == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetFailedAttemptsAsync_WhenNoMfaConfig_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mfaConfigRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMfaConfiguration?)null);

        // Act
        var result = await _service.ResetFailedAttemptsAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mfaConfigRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetMfaAttemptsAsync Tests

    [Fact]
    public async Task GetMfaAttemptsAsync_WhenCalled_ShouldReturnAttempts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var attempts = new List<MfaAttempt>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, IsSuccessful = true, AttemptedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, IsSuccessful = false, AttemptedAt = DateTime.UtcNow.AddMinutes(-5) }
        };

        _mfaAttemptRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempts);

        // Act
        var result = await _service.GetMfaAttemptsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(attempts);
    }

    [Fact]
    public async Task GetMfaAttemptsAsync_WithCustomLimit_ShouldUseSpecifiedLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var limit = 10;
        var attempts = new List<MfaAttempt>();

        _mfaAttemptRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempts);

        // Act
        await _service.GetMfaAttemptsAsync(userId, limit);

        // Assert
        _mfaAttemptRepositoryMock.Verify(
            x => x.GetByUserIdAsync(userId, limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
