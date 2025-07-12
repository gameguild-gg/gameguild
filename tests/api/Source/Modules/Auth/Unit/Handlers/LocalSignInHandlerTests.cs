using GameGuild.Modules.Authentication;
using Moq;


namespace GameGuild.Tests.Modules.Auth.Unit.Handlers;

public class LocalSignInHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly LocalSignInHandler _handler;

    public LocalSignInHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _handler = new LocalSignInHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSignInResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "password123",
            TenantId = tenantId
        };

        var expectedResponse = new SignInResponseDto
        {
            AccessToken = "mock-access-token",
            RefreshToken = "mock-refresh-token",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser"
            },
            TenantId = tenantId
        };

        _mockAuthService.Setup(x => x.LocalSignInAsync(
                It.Is<LocalSignInRequestDto>(r =>
                    r.Email == command.Email &&
                    r.Password == command.Password &&
                    r.TenantId == command.TenantId
                )
            ))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("mock-access-token", result.AccessToken);
        Assert.Equal("mock-refresh-token", result.RefreshToken);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.Equal(tenantId, result.TenantId);

        _mockAuthService.Verify(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyEmail_CallsAuthService()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "",
            Password = "password123"
        };

        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequestDto>()))
            .ThrowsAsync(new ArgumentException("Email is required"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.LocalSignInAsync(
            It.Is<LocalSignInRequestDto>(r => r.Email == "" && r.Password == "password123")), Times.Once);
    }
}
