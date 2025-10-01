using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the DeactivateCredentialCommandHandler
/// Tests CQRS command handling with mocked dependencies
/// </summary>
public class DeactivateCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<DeactivateCredentialCommandHandler>> _mockLogger;
    private readonly DeactivateCredentialCommandHandler _handler;

    public DeactivateCredentialCommandHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<DeactivateCredentialCommandHandler>>();
        _handler = new DeactivateCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_Should_Deactivate_Credential_Successfully()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        _mockCredentialService.Setup(s => s.DeactivateCredentialAsync(credentialId))
                             .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockCredentialService.Verify(s => s.DeactivateCredentialAsync(credentialId), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialDeactivatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Service_Throws()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);
        var expectedException = new ArgumentException("Credential not found");

        _mockCredentialService.Setup(s => s.DeactivateCredentialAsync(credentialId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(command, CancellationToken.None));

        exception.Should().Be(expectedException);
        _mockCredentialService.Verify(s => s.DeactivateCredentialAsync(credentialId), Times.Once);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_CredentialService_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeactivateCredentialCommandHandler(null!, _mockMediator.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Mediator_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeactivateCredentialCommandHandler(_mockCredentialService.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeactivateCredentialCommandHandler(_mockCredentialService.Object, _mockMediator.Object, null!));
    }

    [Fact]
    public async Task Handle_Should_Log_Information_On_Success()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        _mockCredentialService.Setup(s => s.DeactivateCredentialAsync(credentialId))
                             .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deactivated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Warning_When_Deactivation_Fails()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);

        _mockCredentialService.Setup(s => s.DeactivateCredentialAsync(credentialId))
                             .ReturnsAsync(false); // Service returns false indicating failure

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deactivation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialDeactivatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Not_Log_Error_On_Exception()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var command = new DeactivateCredentialCommand(credentialId);
        var expectedException = new InvalidOperationException("Service error");

        _mockCredentialService.Setup(s => s.DeactivateCredentialAsync(credentialId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));

        // Verify no logging occurred since exceptions are not caught
        _mockLogger.VerifyNoOtherCalls();
    }
}