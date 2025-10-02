using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNewTokens()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<RefreshTokenHandler>>();
        var handler = new RefreshTokenHandler(mockAuth.Object, mockMediator.Object, mockLogger.Object);

        var command = new RefreshTokenCommand { RefreshToken = "token" };
        var response = new SignInResponse
        {
            User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com" },
            AccessToken = "new-access",
            RefreshToken = "new-refresh"
        };

        mockAuth.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>())).ReturnsAsync(response);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
