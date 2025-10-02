using FluentAssertions;
using GameGuild.Modules.Credentials;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the GetDeletedCredentialsQueryHandler
/// Tests CQRS query handling with mocked dependencies
/// </summary>
public class GetDeletedCredentialsQueryHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly GetDeletedCredentialsQueryHandler _handler;

    public GetDeletedCredentialsQueryHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _handler = new GetDeletedCredentialsQueryHandler(_mockCredentialService.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnDeletedCredentials()
    {
        // Arrange
        var query = new GetDeletedCredentialsQuery();
        var expectedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), Type = "password", UserId = Guid.NewGuid(), DeletedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Type = "api_key", UserId = Guid.NewGuid(), DeletedAt = DateTime.UtcNow }
        };

        _mockCredentialService.Setup(s => s.GetDeletedCredentialsAsync())
                             .ReturnsAsync(expectedCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedCredentials);
        result.Should().HaveCount(2);
        _mockCredentialService.Verify(s => s.GetDeletedCredentialsAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCollection_WhenNoDeletedCredentialsExist()
    {
        // Arrange
        var query = new GetDeletedCredentialsQuery();
        var emptyCredentials = new List<Credential>();

        _mockCredentialService.Setup(s => s.GetDeletedCredentialsAsync())
                             .ReturnsAsync(emptyCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _mockCredentialService.Verify(s => s.GetDeletedCredentialsAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRethrowException_WhenServiceThrows()
    {
        // Arrange
        var query = new GetDeletedCredentialsQuery();
        var expectedException = new InvalidOperationException("Database connection failed");

        _mockCredentialService.Setup(s => s.GetDeletedCredentialsAsync())
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _mockCredentialService.Verify(s => s.GetDeletedCredentialsAsync(), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCredentialServiceIsNull()
    {
        // Act & Assert
        FluentActions.Invoking(() => new GetDeletedCredentialsQueryHandler(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("credentialService");
    }
}
