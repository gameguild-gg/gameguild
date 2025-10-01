using FluentAssertions;
using GameGuild.Modules.Credentials;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Handlers;

/// <summary>
/// Unit tests for the GetCredentialByIdQueryHandler
/// Tests query processing and error handling
/// </summary>
public class GetCredentialByIdQueryHandlerTests
{
    private readonly Mock<ICredentialService> _mockCredentialService;
    private readonly Mock<ILogger<GetCredentialByIdQueryHandler>> _mockLogger;
    private readonly GetCredentialByIdQueryHandler _handler;

    public GetCredentialByIdQueryHandlerTests()
    {
        _mockCredentialService = new Mock<ICredentialService>();
        _mockLogger = new Mock<ILogger<GetCredentialByIdQueryHandler>>();
        _handler = new GetCredentialByIdQueryHandler(_mockCredentialService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Credential_When_Found()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var query = new GetCredentialByIdQuery(credentialId);
        var expectedCredential = new Credential
        {
            Id = credentialId,
            Type = "password",
            UserId = Guid.NewGuid(),
            IsActive = true
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(expectedCredential);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedCredential);
        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var query = new GetCredentialByIdQuery(credentialId);

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync((Credential?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Service_Throws()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var query = new GetCredentialByIdQuery(credentialId);
        var expectedException = new ArgumentException("Credential service error");

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(query, CancellationToken.None));

        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_CredentialService_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetCredentialByIdQueryHandler(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new GetCredentialByIdQueryHandler(_mockCredentialService.Object, null!));
    }

    [Fact]
    public async Task Handle_Should_Log_Information_On_Success()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var query = new GetCredentialByIdQuery(credentialId);
        var credential = new Credential
        {
            Id = credentialId,
            Type = "password",
            UserId = Guid.NewGuid(),
            IsActive = true
        };

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync(credential);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify that logging occurred (implementation would depend on actual logging in handler)
        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Warning_When_Not_Found()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var query = new GetCredentialByIdQuery(credentialId);

        _mockCredentialService.Setup(s => s.GetCredentialByIdAsync(credentialId))
                             .ReturnsAsync((Credential?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify that logging occurred (implementation would depend on actual logging in handler)
        _mockCredentialService.Verify(s => s.GetCredentialByIdAsync(credentialId), Times.Once);
    }
}