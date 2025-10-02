using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

public class VerifyWeb3SignatureHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSuccess()
    {
        // Arrange
        var mockAuth = new Mock<IAuthService>();
        var handler = new VerifyWeb3SignatureHandler(mockAuth.Object);

        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "0x123",
            Signature = "0xsig",
            Nonce = "nonce",
            ChainId = "1"
        };
        var response = new SignInResponse
        {
            User = new GameGuild.Modules.Users.UserDto { Id = Guid.NewGuid(), Email = "test@example.com" },
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        mockAuth.Setup(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3AuthenticationVerificationRequest>())).ReturnsAsync(response);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
