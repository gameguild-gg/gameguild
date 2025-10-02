using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

public class GoogleIdTokenSignInHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSuccess()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<GoogleIdTokenSignInHandler>>();
        var handler = new GoogleIdTokenSignInHandler(mockAuth.Object, mockMediator.Object, mockLogger.Object);

        var command = new GoogleIdTokenSignInCommand { IdToken = "token", TenantId = Guid.NewGuid() };
        var response = new SignInResponse
        {
            User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com" },
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        mockAuth.Setup(x => x.GoogleIdTokenSignInAsync(It.IsAny<GoogleIdTokenRequestDto>())).ReturnsAsync(response);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
