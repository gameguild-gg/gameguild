using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

public class GenerateWeb3ChallengeHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnChallenge()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        var handler = new GenerateWeb3ChallengeHandler(mockAuth.Object);
        var command = new GenerateWeb3ChallengeCommand { WalletAddress = "0x123", ChainId = "1" };
        var response = new Web3ChallengeResponse { Challenge = "test", ExpiresAt = DateTime.UtcNow.AddMinutes(5) };

        mockAuth.Setup(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequest>())).ReturnsAsync(response);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Challenge.Should().Be("test");
    }
}
