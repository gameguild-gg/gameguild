using FluentAssertions;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the GetCredentialByUserIdAndTypeQueryHandler
/// Tests query processing for retrieving credentials by user ID and type
/// </summary>
public class GetCredentialByUserIdAndTypeQueryHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly GetCredentialByUserIdAndTypeQueryHandler _handler;

    public GetCredentialByUserIdAndTypeQueryHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _handler = new GetCredentialByUserIdAndTypeQueryHandler(_mockCredentialService.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Credential_When_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialType = "password";
        var query = new GetCredentialByUserIdAndTypeQuery(userId, credentialType);
        var expectedCredential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = credentialType,
            IsActive = true
        };

        _mockCredentialService.Setup(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType))
                             .ReturnsAsync(expectedCredential);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedCredential);
        _mockCredentialService.Verify(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialType = "api_key";
        var query = new GetCredentialByUserIdAndTypeQuery(userId, credentialType);

        _mockCredentialService.Setup(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType))
                             .ReturnsAsync((Credential?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockCredentialService.Verify(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Service_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialType = "oauth_token";
        var query = new GetCredentialByUserIdAndTypeQuery(userId, credentialType);
        var expectedException = new ArgumentException("Service error");

        _mockCredentialService.Setup(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(query, CancellationToken.None));

        _mockCredentialService.Verify(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType), Times.Once);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_CredentialService_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetCredentialByUserIdAndTypeQueryHandler(null!));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("oauth_token")]
    [InlineData("2fa_secret")]
    public async Task Handle_Should_Work_With_Different_Credential_Types(string credentialType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetCredentialByUserIdAndTypeQuery(userId, credentialType);
        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = credentialType,
            IsActive = true
        };

        _mockCredentialService.Setup(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType))
                             .ReturnsAsync(credential);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(credentialType);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_Should_Handle_Empty_UserId()
    {
        // Arrange
        var userId = Guid.Empty;
        var credentialType = "password";
        var query = new GetCredentialByUserIdAndTypeQuery(userId, credentialType);

        _mockCredentialService.Setup(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType))
                             .ReturnsAsync((Credential?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockCredentialService.Verify(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Handle_Query_Failure_Gracefully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var credentialType = "password";
        var query = new GetCredentialByUserIdAndTypeQuery(userId, credentialType);
        var serviceException = new InvalidOperationException("Database connection failure");

        _mockCredentialService.Setup(s => s.GetCredentialByUserIdAndTypeAsync(userId, credentialType))
                             .ThrowsAsync(serviceException);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(query, CancellationToken.None));
    }
}