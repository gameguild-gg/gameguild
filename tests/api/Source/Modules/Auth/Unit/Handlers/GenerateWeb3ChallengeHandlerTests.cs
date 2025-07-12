using GameGuild.Modules.Authentication;
using Moq;


namespace GameGuild.Tests.Modules.Auth.Unit.Handlers;

public class GenerateWeb3ChallengeHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly GenerateWeb3ChallengeHandler _handler;

    public GenerateWeb3ChallengeHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _handler = new GenerateWeb3ChallengeHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsWeb3ChallengeResponse()
    {
        // Arrange
        var command = new GenerateWeb3ChallengeCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            ChainId = "1"
        };

        var expectedResponse = new Web3ChallengeResponseDto
        {
            Challenge = "Sign this message to authenticate with GameGuild",
            Nonce = "random-nonce-123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockAuthService.Setup(x => x.GenerateWeb3ChallengeAsync(
                It.Is<Web3ChallengeRequestDto>(r =>
                    r.WalletAddress == command.WalletAddress &&
                    r.ChainId == command.ChainId
                )
            ))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Sign this message to authenticate with GameGuild", result.Challenge);
        Assert.Equal("random-nonce-123", result.Nonce);
        Assert.Equal(expectedResponse.ExpiresAt, result.ExpiresAt);

        _mockAuthService.Verify(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidWalletAddress_ThrowsArgumentException()
    {
        // Arrange
        var command = new GenerateWeb3ChallengeCommand
        {
            WalletAddress = "invalid-address",
            ChainId = "1"
        };

        _mockAuthService.Setup(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequestDto>()))
            .ThrowsAsync(new ArgumentException("Invalid wallet address"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.GenerateWeb3ChallengeAsync(
            It.Is<Web3ChallengeRequestDto>(r => r.WalletAddress == "invalid-address")), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyWalletAddress_CallsAuthService()
    {
        // Arrange
        var command = new GenerateWeb3ChallengeCommand
        {
            WalletAddress = "",
            ChainId = "1"
        };

        _mockAuthService.Setup(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequestDto>()))
            .ThrowsAsync(new ArgumentException("Wallet address is required"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.GenerateWeb3ChallengeAsync(
            It.Is<Web3ChallengeRequestDto>(r => r.WalletAddress == "" && r.ChainId == "1")), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidChainId_CallsAuthService()
    {
        // Arrange
        var command = new GenerateWeb3ChallengeCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            ChainId = "-1"
        };

        _mockAuthService.Setup(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequestDto>()))
            .ThrowsAsync(new ArgumentException("Invalid chain ID"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.GenerateWeb3ChallengeAsync(
            It.Is<Web3ChallengeRequestDto>(r => r.ChainId == "-1")), Times.Once);
    }

    [Fact]
    public async Task Handle_AuthServiceThrowsException_RethrowsException()
    {
        // Arrange
        var command = new GenerateWeb3ChallengeCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            ChainId = "1"
        };

        var expectedException = new InvalidOperationException("Service error");
        _mockAuthService.Setup(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequestDto>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Service error", thrownException.Message);
        _mockAuthService.Verify(x => x.GenerateWeb3ChallengeAsync(It.IsAny<Web3ChallengeRequestDto>()), Times.Once);
    }
}
