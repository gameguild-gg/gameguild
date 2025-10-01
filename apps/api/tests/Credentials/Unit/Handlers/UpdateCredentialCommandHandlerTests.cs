using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the UpdateCredentialCommandHandler
/// Tests CQRS command handling with mocked dependencies
/// </summary>
public class UpdateCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<UpdateCredentialCommandHandler>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly UpdateCredentialCommandHandler _handler;

    public UpdateCredentialCommandHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<UpdateCredentialCommandHandler>>();
        _mockMediator = new Mock<IMediator>();
        _handler = new UpdateCredentialCommandHandler(_mockCredentialService.Object, _mockLogger.Object, _mockMediator.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCredentialSuccessfully()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        UpdateCredentialCommand command = new()
        {
            Id = credentialId,
            Value = "new_value",
            Metadata = "{\"updated\": true}",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        Credential existingCredential = new()
        {
            Id = credentialId,
            Value = "old_value",
            Metadata = "{\"old\": true}",
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        };

        Credential updatedCredential = new()
        {
            Id = credentialId,
            Value = command.Value,
            Metadata = command.Metadata,
            ExpiresAt = command.ExpiresAt
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(existingCredential);
        _mockCredentialService.Setup(s => s.UpdateCredentialAsync(It.IsAny<Credential>()))
                             .ReturnsAsync(updatedCredential);

        // Act
        Credential result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(credentialId);
        result.Value.Should().Be("new_value");
        _mockCredentialService.Verify(s => s.UpdateCredentialAsync(It.IsAny<Credential>()), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCredentialNotFound()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        UpdateCredentialCommand command = new()
        {
            Id = credentialId,
            Value = "new_value"
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync((Credential?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Credential with ID {credentialId} not found");

        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
        _mockCredentialService.Verify(s => s.UpdateCredentialAsync(It.IsAny<Credential>()), Times.Never);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenUpdateServiceThrows()
    {
        // Arrange
        Guid credentialId = Guid.NewGuid();
        UpdateCredentialCommand command = new()
        {
            Id = credentialId,
            Value = "new_value"
        };

        Credential existingCredential = new()
        {
            Id = credentialId,
            Value = "old_value"
        };

        InvalidOperationException expectedException = new("Database connection failed");

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(existingCredential);
        _mockCredentialService.Setup(s => s.UpdateCredentialAsync(It.IsAny<Credential>()))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
        _mockCredentialService.Verify(s => s.UpdateCredentialAsync(It.IsAny<Credential>()), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new UpdateCredentialCommandHandler(null!, _mockLogger.Object, _mockMediator.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new UpdateCredentialCommandHandler(_mockCredentialService.Object, null!, _mockMediator.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new UpdateCredentialCommandHandler(_mockCredentialService.Object, _mockLogger.Object, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }
}