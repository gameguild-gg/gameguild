using GameGuild.Modules.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;


namespace GameGuild.Tests.Modules.Auth.Unit.Handlers;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<RefreshTokenHandler>> _mockLogger;
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<RefreshTokenHandler>>();
        _handler = new RefreshTokenHandler(_mockAuthService.Object, _mockMediator.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_handler);
    }

    [Fact]
    public void Constructor_WithNullAuthService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RefreshTokenHandler(null!, _mockMediator.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RefreshTokenHandler(_mockAuthService.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RefreshTokenHandler(_mockAuthService.Object, _mockMediator.Object, null!));
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSignInResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token",
            TenantId = tenantId,
        };

        var refreshResponse = new RefreshTokenResponseDto
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TenantId = tenantId,
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(
                It.Is<RefreshTokenRequestDto>(r =>
                    r.RefreshToken == command.RefreshToken &&
                    r.TenantId == command.TenantId
                )
            ))
            .ReturnsAsync(refreshResponse);

        _mockMediator.Setup(x => x.Publish(It.IsAny<TokenRefreshedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.Equal(refreshResponse.ExpiresAt, result.Expires);
        Assert.Equal(tenantId, result.TenantId);

        _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()), Times.Once);
        _mockMediator.Verify(x => x.Publish(It.IsAny<TokenRefreshedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "invalid-refresh-token",
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);
        _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()), Times.Once);
        _mockMediator.Verify(x => x.Publish(It.IsAny<TokenRefreshedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyRefreshToken_CallsAuthService()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "",
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ThrowsAsync(new ArgumentException("Refresh token is required"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.RefreshTokenAsync(
            It.Is<RefreshTokenRequestDto>(r => r.RefreshToken == "")), Times.Once);
    }

    [Fact]
    public async Task Handle_AuthServiceThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token",
        };

        var expectedException = new InvalidOperationException("Database error");
        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Database error", thrownException.Message);
        _mockAuthService.Verify(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequestDto>()), Times.Once);
        
        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process refresh token request")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
