using GameGuild.Modules.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;


namespace GameGuild.Tests.Modules.Auth.Unit.Handlers;

public class RevokeTokenHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<RevokeTokenHandler>> _mockLogger;
    private readonly RevokeTokenHandler _handler;

    public RevokeTokenHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<RevokeTokenHandler>>();
        _handler = new RevokeTokenHandler(_mockAuthService.Object, _mockMediator.Object, _mockLogger.Object);
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
            new RevokeTokenHandler(null!, _mockMediator.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RevokeTokenHandler(_mockAuthService.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RevokeTokenHandler(_mockAuthService.Object, _mockMediator.Object, null!));
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUnitValue()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token",
            IpAddress = "192.168.1.1",
        };

        _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync(
                command.RefreshToken,
                command.IpAddress!
            ))
            .Returns(Task.CompletedTask);

        _mockMediator.Setup(x => x.Publish(It.IsAny<TokenRevokedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(MediatR.Unit.Value, result);
        
        _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(
            command.RefreshToken, 
            command.IpAddress!), Times.Once);
        
        _mockMediator.Verify(x => x.Publish(
            It.Is<TokenRevokedNotification>(n => 
                n.RefreshToken == command.RefreshToken && 
                n.IpAddress == command.IpAddress), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullIpAddress_UsesUnknownAsDefault()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token",
            IpAddress = null,
        };

        _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync(
                command.RefreshToken,
                "Unknown"
            ))
            .Returns(Task.CompletedTask);

        _mockMediator.Setup(x => x.Publish(It.IsAny<TokenRevokedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(MediatR.Unit.Value, result);
        
        _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(
            command.RefreshToken, 
            "Unknown"), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "invalid-refresh-token",
            IpAddress = "192.168.1.1",
        };

        _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Invalid refresh token", exception.Message);
        _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(
            command.RefreshToken, 
            command.IpAddress!), Times.Once);
        
        // Should not publish notification if revocation fails
        _mockMediator.Verify(x => x.Publish(It.IsAny<TokenRevokedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyRefreshToken_ThrowsArgumentException()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "",
            IpAddress = "192.168.1.1",
        };

        _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Refresh token cannot be empty"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync("", "192.168.1.1"), Times.Once);
    }

    [Fact]
    public async Task Handle_AuthServiceThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var command = new RevokeTokenCommand
        {
            RefreshToken = "valid-refresh-token",
            IpAddress = "192.168.1.1",
        };

        var expectedException = new InvalidOperationException("Database error");
        _mockAuthService.Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Database error", thrownException.Message);
        _mockAuthService.Verify(x => x.RevokeRefreshTokenAsync(
            command.RefreshToken, 
            command.IpAddress!), Times.Once);

        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to revoke token")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
