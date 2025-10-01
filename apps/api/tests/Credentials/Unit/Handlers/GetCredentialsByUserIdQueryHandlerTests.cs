using FluentAssertions;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the GetCredentialsByUserIdQueryHandler
/// Tests query processing for retrieving all credentials for a specific user
/// </summary>
public class GetCredentialsByUserIdQueryHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<GetCredentialsByUserIdQueryHandler>> _mockLogger;
    private readonly GetCredentialsByUserIdQueryHandler _handler;

    public GetCredentialsByUserIdQueryHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<GetCredentialsByUserIdQueryHandler>>();
        _handler = new GetCredentialsByUserIdQueryHandler(_mockCredentialService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Credentials_When_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCredentialsByUserIdQuery(userId);
        var expectedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "password", IsActive = true },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "api_key", IsActive = true }
        };

        _mockCredentialService.Setup(s => s.GetCredentialsByUserIdAsync(userId))
                             .ReturnsAsync(expectedCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedCredentials);
        _mockCredentialService.Verify(s => s.GetCredentialsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_When_No_Credentials_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCredentialsByUserIdQuery(userId);
        var emptyCredentials = new List<Credential>();

        _mockCredentialService.Setup(s => s.GetCredentialsByUserIdAsync(userId))
                             .ReturnsAsync(emptyCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockCredentialService.Verify(s => s.GetCredentialsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Service_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCredentialsByUserIdQuery(userId);
        var expectedException = new ArgumentException("Service error");

        _mockCredentialService.Setup(s => s.GetCredentialsByUserIdAsync(userId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(query, CancellationToken.None));

        _mockCredentialService.Verify(s => s.GetCredentialsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_CredentialService_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetCredentialsByUserIdQueryHandler(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetCredentialsByUserIdQueryHandler(_mockCredentialService.Object, null!));
    }

    [Fact]
    public async Task Handle_Should_Return_Mixed_Credential_Types()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCredentialsByUserIdQuery(userId);
        var mixedCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "password", IsActive = true },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "api_key", IsActive = false },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "oauth_token", IsActive = true },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "2fa_secret", IsActive = true }
        };

        _mockCredentialService.Setup(s => s.GetCredentialsByUserIdAsync(userId))
                             .ReturnsAsync(mixedCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain(c => c.Type == "password");
        result.Should().Contain(c => c.Type == "api_key");
        result.Should().Contain(c => c.Type == "oauth_token");
        result.Should().Contain(c => c.Type == "2fa_secret");
    }

    [Fact]
    public async Task Handle_Should_Handle_Empty_UserId()
    {
        // Arrange
        var userId = Guid.Empty;
        var query = new GetCredentialsByUserIdQuery(userId);
        var emptyCredentials = new List<Credential>();

        _mockCredentialService.Setup(s => s.GetCredentialsByUserIdAsync(userId))
                             .ReturnsAsync(emptyCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockCredentialService.Verify(s => s.GetCredentialsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Only_User_Specific_Credentials()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCredentialsByUserIdQuery(userId);
        var userCredentials = new List<Credential>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "password", IsActive = true },
            new() { Id = Guid.NewGuid(), UserId = userId, Type = "api_key", IsActive = true }
        };

        _mockCredentialService.Setup(s => s.GetCredentialsByUserIdAsync(userId))
                             .ReturnsAsync(userCredentials);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(c => c.UserId == userId);
        _mockCredentialService.Verify(s => s.GetCredentialsByUserIdAsync(userId), Times.Once);
    }
}