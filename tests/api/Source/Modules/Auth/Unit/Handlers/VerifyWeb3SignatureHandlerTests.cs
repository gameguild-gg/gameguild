using GameGuild.Modules.Auth;
using Moq;

namespace GameGuild.API.Tests.Modules.Auth.Unit.Handlers;

public class VerifyWeb3SignatureHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly VerifyWeb3SignatureHandler _handler;

    public VerifyWeb3SignatureHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _handler = new VerifyWeb3SignatureHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSignInResponse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Signature = "0xabcdef1234567890...",
            Nonce = "random-nonce-123",
            ChainId = "1"
        };

        var expectedResponse = new SignInResponseDto
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                Username = "web3user"
            },
            TenantId = tenantId
        };

        _mockAuthService.Setup(x => x.VerifyWeb3SignatureAsync(
                It.Is<Web3VerifyRequestDto>(r =>
                    r.WalletAddress == command.WalletAddress &&
                    r.Signature == command.Signature &&
                    r.Nonce == command.Nonce &&
                    r.ChainId == command.ChainId
                )
            ))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("user@example.com", result.User.Email);
        Assert.Equal(tenantId, result.TenantId);

        _mockAuthService.Verify(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidSignature_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Signature = "invalid-signature",
            Nonce = "random-nonce-123",
            ChainId = "1"
        };

        _mockAuthService.Setup(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid signature"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.VerifyWeb3SignatureAsync(
            It.Is<Web3VerifyRequestDto>(r => r.Signature == "invalid-signature")), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidWalletAddress_ThrowsArgumentException()
    {
        // Arrange
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "invalid-address",
            Signature = "0xabcdef1234567890...",
            Nonce = "random-nonce-123",
            ChainId = "1"
        };

        _mockAuthService.Setup(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()))
            .ThrowsAsync(new ArgumentException("Invalid wallet address"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.VerifyWeb3SignatureAsync(
            It.Is<Web3VerifyRequestDto>(r => r.WalletAddress == "invalid-address")), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredNonce_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Signature = "0xabcdef1234567890...",
            Nonce = "expired-nonce",
            ChainId = "1"
        };

        _mockAuthService.Setup(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Nonce expired or invalid"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.VerifyWeb3SignatureAsync(
            It.Is<Web3VerifyRequestDto>(r => r.Nonce == "expired-nonce")), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyParameters_CallsAuthService()
    {
        // Arrange
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "",
            Signature = "",
            Nonce = "",
            ChainId = ""
        };

        _mockAuthService.Setup(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()))
            .ThrowsAsync(new ArgumentException("Required parameters missing"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));

        _mockAuthService.Verify(x => x.VerifyWeb3SignatureAsync(
            It.Is<Web3VerifyRequestDto>(r => 
                r.WalletAddress == "" && 
                r.Signature == "" && 
                r.Nonce == "" && 
                r.ChainId == "")), Times.Once);
    }

    [Fact]
    public async Task Handle_AuthServiceThrowsException_RethrowsException()
    {
        // Arrange
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "0x1234567890123456789012345678901234567890",
            Signature = "0xabcdef1234567890...",
            Nonce = "random-nonce-123",
            ChainId = "1"
        };

        var expectedException = new InvalidOperationException("Service error");
        _mockAuthService.Setup(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Equal("Service error", thrownException.Message);
        _mockAuthService.Verify(x => x.VerifyWeb3SignatureAsync(It.IsAny<Web3VerifyRequestDto>()), Times.Once);
    }
}
