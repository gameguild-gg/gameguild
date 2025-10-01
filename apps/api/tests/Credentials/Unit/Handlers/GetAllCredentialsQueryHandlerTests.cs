using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the GetAllCredentialsQueryHandler
/// Tests CQRS query handling with mocked dependencies
/// </summary>
public class GetAllCredentialsQueryHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<GetAllCredentialsQueryHandler>> _mockLogger;
    private readonly GetAllCredentialsQueryHandler _handler;

    public GetAllCredentialsQueryHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<GetAllCredentialsQueryHandler>>();
        _handler = new GetAllCredentialsQueryHandler(_mockCredentialService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllCredentials()
    {
        // Arrange
        var query = new GetAllCredentialsQuery();
        var expectedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), Type = "password", UserId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Type = "api_key", UserId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Type = "oauth_token", UserId = Guid.NewGuid() }
        };

        _mockCredentialService.Setup(s => s.GetAllCredentialsAsync())
                             .ReturnsAsync(expectedCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedCredentials);
        result.Should().HaveCount(3);
        _mockCredentialService.Verify(s => s.GetAllCredentialsAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoCredentialsExist()
    {
        // Arrange
        var query = new GetAllCredentialsQuery();
        var emptyCredentials = new List<Credential>();

        _mockCredentialService.Setup(s => s.GetAllCredentialsAsync())
                             .ReturnsAsync(emptyCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _mockCredentialService.Verify(s => s.GetAllCredentialsAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenServiceThrows()
    {
        // Arrange
        var query = new GetAllCredentialsQuery();
        var expectedException = new InvalidOperationException("Database connection failed");

        _mockCredentialService.Setup(s => s.GetAllCredentialsAsync())
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _mockCredentialService.Verify(s => s.GetAllCredentialsAsync(), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new GetAllCredentialsQueryHandler(null!, _mockLogger.Object))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new GetAllCredentialsQueryHandler(_mockCredentialService.Object, null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}