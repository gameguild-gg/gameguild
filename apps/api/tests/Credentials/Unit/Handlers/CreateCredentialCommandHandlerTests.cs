using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the CreateCredentialCommandHandler
/// Tests CQRS command handling with mocked dependencies
/// </summary>
public class CreateCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<CreateCredentialCommandHandler>> _mockLogger;
    private readonly Mock<IMediator> _mockMediator;
    private readonly CreateCredentialCommandHandler _handler;

    public CreateCredentialCommandHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<CreateCredentialCommandHandler>>();
        _mockMediator = new Mock<IMediator>();
        _handler = new CreateCredentialCommandHandler(_mockCredentialService.Object, _mockLogger.Object, _mockMediator.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateCredentialSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var command = new CreateCredentialCommand
        {
            UserId = userId,
            Type = "password",
            Value = "hashed_password",
            Metadata = """{"algorithm": "bcrypt"}""",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

        var getUserQuery = new GetUserByIdQuery { UserId = userId };
        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

        var expectedCredential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = command.Type,
            Value = command.Value,
            Metadata = command.Metadata,
            ExpiresAt = command.ExpiresAt,
            IsActive = command.IsActive
        };

        _mockCredentialService.Setup(s => s.CreateCredentialAsync(It.IsAny<Credential>()))
                             .ReturnsAsync(expectedCredential);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Type.Should().Be(command.Type);
        result.Value.Should().Be(command.Value);
        result.Metadata.Should().Be(command.Metadata);
        result.ExpiresAt.Should().Be(command.ExpiresAt);
        result.IsActive.Should().Be(command.IsActive);

        _mockMediator.Verify(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCredentialService.Verify(s => s.CreateCredentialAsync(It.IsAny<Credential>()), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateCredentialCommand
        {
            UserId = userId,
            Type = "password",
            Value = "hashed_password"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((User?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"User with ID {userId} not found");

        _mockMediator.Verify(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCredentialService.Verify(s => s.CreateCredentialAsync(It.IsAny<Credential>()), Times.Never);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenServiceThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var command = new CreateCredentialCommand
        {
            UserId = userId,
            Type = "password",
            Value = "hashed_password"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

        var expectedException = new InvalidOperationException("Database error");
        _mockCredentialService.Setup(s => s.CreateCredentialAsync(It.IsAny<Credential>()))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _mockCredentialService.Verify(s => s.CreateCredentialAsync(It.IsAny<Credential>()), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.IsAny<CredentialCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPublishCredentialCreatedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var command = new CreateCredentialCommand
        {
            UserId = userId,
            Type = "api_key",
            Value = "encrypted_key"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

        var createdCredential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = command.Type,
            Value = command.Value,
            CreatedAt = DateTime.UtcNow
        };

        _mockCredentialService.Setup(s => s.CreateCredentialAsync(It.IsAny<Credential>()))
                             .ReturnsAsync(createdCredential);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMediator.Verify(m => m.Publish(
            It.Is<CredentialCreatedEvent>(e =>
                e.CredentialId == createdCredential.Id &&
                e.UserId == createdCredential.UserId &&
                e.Type == createdCredential.Type &&
                e.CreatedAt == createdCredential.CreatedAt),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectTimestamps()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var command = new CreateCredentialCommand
        {
            UserId = userId,
            Type = "password",
            Value = "hashed_password"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

        Credential? capturedCredential = null;
        _mockCredentialService.Setup(s => s.CreateCredentialAsync(It.IsAny<Credential>()))
                             .Callback<Credential>(c => capturedCredential = c)
                             .ReturnsAsync((Credential c) => c);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCredential.Should().NotBeNull();
        capturedCredential!.Id.Should().NotBeEmpty();
        capturedCredential.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        capturedCredential.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CreateCredentialCommandHandler(null!, _mockLogger.Object, _mockMediator.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CreateCredentialCommandHandler(_mockCredentialService.Object, null!, _mockMediator.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new CreateCredentialCommandHandler(_mockCredentialService.Object, _mockLogger.Object, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public async Task Handle_Should_Handle_Service_Failure_Gracefully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var command = new CreateCredentialCommand
        {
            UserId = userId,
            Type = "password",
            Value = "hashed_password"
        };

        var getUserQuery = new GetUserByIdQuery { UserId = userId };
        _mockMediator.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

        // Setup service to throw an exception (simulating service failure)
        _mockCredentialService.Setup(s => s.CreateCredentialAsync(It.IsAny<Credential>()))
                             .ThrowsAsync(new InvalidOperationException("Database connection failure"));

        // Act & Assert
        await FluentActions.Invoking(async () => await _handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failure");
    }
}