using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

public class RevokeTokenHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRevokeToken()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<RevokeTokenHandler>>();
        var handler = new RevokeTokenHandler(mockAuth.Object, mockMediator.Object, mockLogger.Object);

        var validGuid = Guid.NewGuid().ToString();
        var command = new RevokeTokenCommand { RefreshToken = validGuid };

        mockAuth.Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(GameGuild.CQRS.Unit.Value);
    }
}
