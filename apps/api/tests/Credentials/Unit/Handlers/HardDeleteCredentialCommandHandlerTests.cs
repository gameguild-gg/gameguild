using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the HardDeleteCredentialCommandHandler
/// Tests CQRS command handling with mocked dependencies
/// </summary>
public class HardDeleteCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<HardDeleteCredentialCommandHandler>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly HardDeleteCredentialCommandHandler _handler;

    public HardDeleteCredentialCommandHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<HardDeleteCredentialCommandHandler>>();
        _mockMediator = new Mock<IMediator>();
        _handler = new HardDeleteCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldHardDeleteCredentialSuccessfully()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new HardDeleteCredentialCommand(credentialId);

        _mockCredentialService.Setup(s => s.HardDeleteCredentialAsync(credentialId))
                             .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockCredentialService.Verify(s => s.HardDeleteCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialDeletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenHardDeleteFails()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new HardDeleteCredentialCommand(credentialId);

        _mockCredentialService.Setup(s => s.HardDeleteCredentialAsync(credentialId))
                             .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockCredentialService.Verify(s => s.HardDeleteCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialDeletedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenServiceThrows()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new HardDeleteCredentialCommand(credentialId);
        var expectedException = new InvalidOperationException("Database connection failed");

        _mockCredentialService.Setup(s => s.HardDeleteCredentialAsync(credentialId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _mockCredentialService.Verify(s => s.HardDeleteCredentialAsync(credentialId), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new HardDeleteCredentialCommandHandler(null!, _mockMediator.Object, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new HardDeleteCredentialCommandHandler(_mockCredentialService.Object, null!, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new HardDeleteCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}
