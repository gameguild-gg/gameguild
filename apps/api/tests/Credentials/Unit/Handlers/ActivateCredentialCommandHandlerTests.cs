using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the ActivateCredentialCommandHandler
/// Tests credential activation command handling
/// </summary>
public class ActivateCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<ActivateCredentialCommandHandler>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly ActivateCredentialCommandHandler _handler;

    public ActivateCredentialCommandHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<ActivateCredentialCommandHandler>>();
        _mockMediator = new Mock<IMediator>();
        _handler = new ActivateCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldActivateCredentialSuccessfully()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new ActivateCredentialCommand(credentialId);

        var credential = new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password",
            IsActive = false
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(credential);

        _mockCredentialService.Setup(s => s.ActivateCredentialAsync(credentialId))
                             .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
        _mockCredentialService.Verify(s => s.ActivateCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialActivatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenCredentialNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new ActivateCredentialCommand(credentialId);

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync((Credential?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
        _mockCredentialService.Verify(s => s.ActivateCredentialAsync(It.IsAny<Guid>()), Times.Never);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialActivatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenActivationFails()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new ActivateCredentialCommand(credentialId);

        var credential = new Credential
        {
            Id = credentialId,
            UserId = Guid.NewGuid(),
            Type = "password"
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(credential);

        _mockCredentialService.Setup(s => s.ActivateCredentialAsync(credentialId))
                             .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockCredentialService.Verify(s => s.ActivateCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialActivatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPublishCredentialActivatedEvent_WhenSuccessful()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var credentialType = "password";
        var command = new ActivateCredentialCommand(credentialId);

        var credential = new Credential
        {
            Id = credentialId,
            UserId = userId,
            Type = credentialType,
            IsActive = false
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(credential);

        _mockCredentialService.Setup(s => s.ActivateCredentialAsync(credentialId))
                             .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMediator.Verify(m => m.Publish(
            It.Is<CredentialActivatedEvent>(e =>
                e.CredentialId == credentialId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenServiceThrows()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new ActivateCredentialCommand(credentialId);
        var expectedException = new InvalidOperationException("Database error");

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new ActivateCredentialCommandHandler(null!, _mockMediator.Object, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new ActivateCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new ActivateCredentialCommandHandler(_mockCredentialService.Object, null!, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }
}