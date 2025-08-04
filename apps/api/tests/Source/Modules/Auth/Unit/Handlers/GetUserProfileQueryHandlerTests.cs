using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using Microsoft.Extensions.Logging;
using Moq;


namespace GameGuild.Tests.Modules.Auth.Unit.Handlers;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<GetUserProfileQueryHandler>> _mockLogger;
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<GetUserProfileQueryHandler>>();
        _handler = new GetUserProfileQueryHandler(_mockUserService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_handler);
    }

    [Fact]
    public void Constructor_WithNullUserService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GetUserProfileQueryHandler(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GetUserProfileQueryHandler(_mockUserService.Object, null!));
    }

    [Fact]
    public async Task Handle_ValidUserId_ReturnsUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery { UserId = userId };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = "Test User",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("test@example.com", result.Username);
        Assert.Equal("Test User", result.GivenName);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("", result.FamilyName);
        Assert.Equal("", result.Title);
        Assert.Equal("", result.Description);
        Assert.True(result.IsEmailVerified);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.Equal(user.UpdatedAt, result.UpdatedAt);
        Assert.Null(result.CurrentTenant);
        Assert.Empty(result.AvailableTenants);

        _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery { UserId = userId };

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal($"User with ID {userId} not found", exception.Message);
        _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_UserServiceThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery { UserId = userId };

        var expectedException = new InvalidOperationException("Database error");
        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Database error", thrownException.Message);
        _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve user profile")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyUserId_CallsUserService()
    {
        // Arrange
        var userId = Guid.Empty;
        var query = new GetUserProfileQuery { UserId = userId };

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal($"User with ID {userId} not found", exception.Message);
        _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_UserWithNullName_HandlesGracefully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery { UserId = userId };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = null!, // Null name
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("test@example.com", result.Username);
        Assert.Null(result.GivenName);
        Assert.Null(result.DisplayName);

        _mockUserService.Verify(x => x.GetUserByIdAsync(userId), Times.Once);
    }
}
