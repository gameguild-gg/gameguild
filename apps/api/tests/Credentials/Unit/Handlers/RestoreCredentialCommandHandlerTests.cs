using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the RestoreCredentialCommandHandler
/// Tests CQRS command handling with mocked dependencies
/// </summary>
public class RestoreCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<RestoreCredentialCommandHandler>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly RestoreCredentialCommandHandler _handler;

    public RestoreCredentialCommandHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<RestoreCredentialCommandHandler>>();
        _mockMediator = new Mock<IMediator>();
        _handler = new RestoreCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldRestoreCredentialSuccessfully()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        RestoreCredentialCommand command = new(credentialId);

        _mockCredentialService.Setup(s => s.RestoreCredentialAsync(credentialId))
                             .ReturnsAsync(true);

        // Act
        bool result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockCredentialService.Verify(s => s.RestoreCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialRestoredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenRestoreFails()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        RestoreCredentialCommand command = new(credentialId);

        _mockCredentialService.Setup(s => s.RestoreCredentialAsync(credentialId))
                             .ReturnsAsync(false);

        // Act
        bool result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockCredentialService.Verify(s => s.RestoreCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialRestoredEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenServiceThrows()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        RestoreCredentialCommand command = new(credentialId);
        InvalidOperationException expectedException = new("Database connection failed");

        _mockCredentialService.Setup(s => s.RestoreCredentialAsync(credentialId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _mockCredentialService.Verify(s => s.RestoreCredentialAsync(credentialId), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new RestoreCredentialCommandHandler(null!, _mockMediator.Object, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new RestoreCredentialCommandHandler(_mockCredentialService.Object, null!, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new RestoreCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}