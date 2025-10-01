using FluentAssertions;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.Tests.Credentials.Unit.Services;

/// <summary>
/// Unit tests for the CredentialService
/// Tests service layer operations with mocked dependencies
/// </summary>
public class CredentialServiceTests
{
    private readonly Mock<ICredentialRepository> _mockRepository;
    private readonly Mock<ILogger<CredentialService>> _mockLogger;
    private readonly CredentialService _service;

    public CredentialServiceTests()
    {
        _mockRepository = new Mock<ICredentialRepository>();
        _mockLogger = new Mock<ILogger<CredentialService>>();
        _service = new CredentialService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCredentialsByUserIdAsync_ShouldReturnCredentials()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "password" },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "api_key" }
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedCredentials);

        // Act
        var result = await _service.GetCredentialsByUserIdAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(expectedCredentials);
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCredentialByIdAsync_ShouldReturnCredential_WhenExists()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var expectedCredential = new Credential { Id = credentialId, Type = "password" };

        _mockRepository.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedCredential);

        // Act
        var result = await _service.GetCredentialByIdAsync(credentialId);

        // Assert
        result.Should().Be(expectedCredential);
        _mockRepository.Verify(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCredentialByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Credential?)null);

        // Act
        var result = await _service.GetCredentialByIdAsync(credentialId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCredentialIncludingDeletedAsync_ShouldReturnCredential_WhenExists()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var expectedCredential = new Credential { Id = credentialId, Type = "password", DeletedAt = DateTime.UtcNow };

        _mockRepository.Setup(r => r.GetByIdIncludingDeletedAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedCredential);

        // Act
        var result = await _service.GetCredentialIncludingDeletedAsync(credentialId);

        // Assert
        result.Should().Be(expectedCredential);
        _mockRepository.Verify(r => r.GetByIdIncludingDeletedAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCredentialByUserIdAndTypeAsync_ShouldReturnCredential_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialType = "password";
        var expectedCredential = new Credential { Id = Guid.NewGuid(), UserId = userId, Type = credentialType };

        _mockRepository.Setup(r => r.GetByUserIdAndTypeAsync(userId, credentialType, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedCredential);

        // Act
        var result = await _service.GetCredentialByUserIdAndTypeAsync(userId, credentialType);

        // Assert
        result.Should().Be(expectedCredential);
        _mockRepository.Verify(r => r.GetByUserIdAndTypeAsync(userId, credentialType, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCredentialAsync_ShouldCreateAndReturnCredential()
    {
        // Arrange
        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "hashed_password"
        };

        _mockRepository.Setup(r => r.AddAsync(credential, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(credential);

        // Act
        var result = await _service.CreateCredentialAsync(credential);

        // Assert
        result.Should().Be(credential);
        _mockRepository.Verify(r => r.AddAsync(credential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCredentialAsync_ShouldThrowArgumentNullException_WhenCredentialIsNull()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.CreateCredentialAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("credential");
    }

    [Fact]
    public async Task UpdateCredentialAsync_ShouldUpdateAndReturnCredential()
    {
        // Arrange
        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = "password",
            Value = "new_hashed_password"
        };

        _mockRepository.Setup(r => r.UpdateAsync(credential, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(credential);

        // Act
        var result = await _service.UpdateCredentialAsync(credential);

        // Assert
        result.Should().Be(credential);
        _mockRepository.Verify(r => r.UpdateAsync(credential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCredentialAsync_ShouldThrowArgumentNullException_WhenCredentialIsNull()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.UpdateCredentialAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("credential");
    }

    [Fact]
    public async Task SoftDeleteCredentialAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.SoftDeleteAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.SoftDeleteCredentialAsync(credentialId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.SoftDeleteAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCredentialAsync_ShouldReturnFalse_WhenNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.SoftDeleteAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(false);

        // Act
        var result = await _service.SoftDeleteCredentialAsync(credentialId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.SoftDeleteAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreCredentialAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.RestoreAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.RestoreCredentialAsync(credentialId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.RestoreAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HardDeleteCredentialAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.HardDeleteAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.HardDeleteCredentialAsync(credentialId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.HardDeleteAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkCredentialAsUsedAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.MarkAsUsedAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.MarkCredentialAsUsedAsync(credentialId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.MarkAsUsedAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateCredentialAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.ActivateAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.ActivateCredentialAsync(credentialId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.ActivateAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateCredentialAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        _mockRepository.Setup(r => r.DeactivateAsync(credentialId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true);

        // Act
        var result = await _service.DeactivateCredentialAsync(credentialId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeactivateAsync(credentialId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCredentialsAsync_ShouldReturnAllCredentials()
    {
        // Arrange
        var expectedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), Type = "password" },
            new() { Id = Guid.NewGuid(), Type = "api_key" }
        };

        _mockRepository.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(expectedCredentials);

        // Act
        var result = await _service.GetAllCredentialsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedCredentials);
        _mockRepository.Verify(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDeletedCredentialsAsync_ShouldReturnDeletedCredentials()
    {
        // Arrange
        var deletedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), Type = "password", DeletedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Type = "api_key", DeletedAt = DateTime.UtcNow }
        };

        _mockRepository.Setup(r => r.GetDeletedAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(deletedCredentials);

        // Act
        var result = await _service.GetDeletedCredentialsAsync();

        // Assert
        result.Should().BeEquivalentTo(deletedCredentials);
        _mockRepository.Verify(r => r.GetDeletedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteCredentialAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.SoftDeleteCredentialAsync(Guid.Empty))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RestoreCredentialAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.RestoreCredentialAsync(Guid.Empty))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HardDeleteCredentialAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.HardDeleteCredentialAsync(Guid.Empty))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MarkCredentialAsUsedAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.MarkCredentialAsUsedAsync(Guid.Empty))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ActivateCredentialAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.ActivateCredentialAsync(Guid.Empty))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeactivateCredentialAsync_ShouldThrowArgumentException_WhenIdIsEmpty()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _service.DeactivateCredentialAsync(Guid.Empty))
            .Should().ThrowAsync<ArgumentException>();
    }
}