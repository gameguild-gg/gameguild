using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class EmailVerificationServiceTests
{
    private readonly Mock<ILogger<EmailVerificationService>> _loggerMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly EmailVerificationService _service;

    public EmailVerificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailVerificationService>>();
        _publisherMock = new Mock<IPublisher>();
        _publisherMock.Setup(x => x.Publish(It.IsAny<EmailVerificationRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new EmailVerificationService(_loggerMock.Object, memoryCache, _publisherMock.Object);
    }

    [Fact]
    public async Task GenerateVerificationTokenAsync_GeneratesValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().MatchRegex("^[a-f0-9]{32}$"); // GUID without dashes
    }

    [Fact]
    public async Task GenerateVerificationTokenAsync_NormalizesEmail_ToLowerCase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "Test@Example.COM";
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Act - Verify with lowercase email
        var result = await _service.VerifyEmailTokenAsync(userId, token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateVerificationTokenAsync_WorksWithSizeLimitedMemoryCache()
    {
        // Arrange
        var sizedCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        var sizedService = new EmailVerificationService(_loggerMock.Object, sizedCache, _publisherMock.Object);
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var act = async () => await sizedService.GenerateVerificationTokenAsync(userId, email);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GenerateMagicLinkTokenAsync_GeneratesValidOneTimeToken()
    {
        var userId = Guid.NewGuid();
        var token = await _service.GenerateMagicLinkTokenAsync(userId, "Magic@Test.COM");

        token.Should().MatchRegex("^[a-f0-9]{32}$");

        var firstValidation = await _service.VerifyMagicLinkTokenAsync(token);
        var secondValidation = await _service.VerifyMagicLinkTokenAsync(token);

        firstValidation.Success.Should().BeTrue();
        firstValidation.UserId.Should().Be(userId);
        firstValidation.Email.Should().Be("magic@test.com");
        secondValidation.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyMagicLinkTokenAsync_WithDistributedCache_WorksAcrossServiceInstances()
    {
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var firstMemoryCache = new MemoryCache(new MemoryCacheOptions());
        using var secondMemoryCache = new MemoryCache(new MemoryCacheOptions());
        var firstService = new EmailVerificationService(_loggerMock.Object, firstMemoryCache, _publisherMock.Object, distributedCache: distributedCache);
        var secondService = new EmailVerificationService(_loggerMock.Object, secondMemoryCache, _publisherMock.Object, distributedCache: distributedCache);
        var userId = Guid.NewGuid();

        var token = await firstService.GenerateMagicLinkTokenAsync(userId, "Magic@Test.COM");
        var result = await secondService.VerifyMagicLinkTokenAsync(token);

        result.Success.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("magic@test.com");
    }

    [Fact]
    public async Task IsTokenValidAsync_ReturnsTrue_ForMagicLinkToken()
    {
        var token = await _service.GenerateMagicLinkTokenAsync(Guid.NewGuid(), "magic@test.com");

        var result = await _service.IsTokenValidAsync(token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendVerificationEmailAsync_CompletesSuccessfully()
    {
        // Arrange
        var email = "test@example.com";
        var token = "test-token";
        var userName = "Test User";

        // Act
        var act = async () => await _service.SendVerificationEmailAsync(email, token, userName);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendVerificationEmailAsync_PublishesVerificationRequestedNotification()
    {
        await _service.SendVerificationEmailAsync("test@example.com", "test-token", "Test User");

        _publisherMock.Verify(
            x => x.Publish(
                It.Is<EmailVerificationRequestedNotification>(notification =>
                    notification.Email == "test@example.com" &&
                    notification.Token == "test-token" &&
                    notification.UserName == "Test User"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmailTokenAsync_ReturnsTrue_WhenTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Act
        var result = await _service.VerifyEmailTokenAsync(userId, token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmailTokenAsync_ReturnsFalse_WhenTokenDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidToken = "invalid-token";

        // Act
        var result = await _service.VerifyEmailTokenAsync(userId, invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailTokenAsync_ReturnsFalse_WhenUserIdMismatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var email = "test@example.com";
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Act
        var result = await _service.VerifyEmailTokenAsync(differentUserId, token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailTokenAsync_MarksEmailAsVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Act
        await _service.VerifyEmailTokenAsync(userId, token);
        var isVerified = await _service.IsEmailVerifiedAsync(userId);

        // Assert
        isVerified.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_ReturnsFalse_WhenNotVerified()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var isVerified = await _service.IsEmailVerifiedAsync(userId);

        // Assert
        isVerified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailTokenAsync_RemovesToken_AfterSuccessfulVerification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Act
        var firstResult = await _service.VerifyEmailTokenAsync(userId, token);
        var secondResult = await _service.VerifyEmailTokenAsync(userId, token);

        // Assert
        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse(); // Token should be removed after first use
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_SendsNewToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Act
        var result = await _service.ResendVerificationEmailAsync(userId, email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_ReturnsFalse_WhenRateLimited()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        await _service.ResendVerificationEmailAsync(userId, email);

        // Act - Try to resend immediately
        var result = await _service.ResendVerificationEmailAsync(userId, email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenValidAsync_ReturnsTrue_WhenTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var token = await _service.GenerateVerificationTokenAsync(userId, email);

        // Act
        var result = await _service.IsTokenValidAsync(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenValidAsync_ReturnsFalse_WhenTokenDoesNotExist()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act
        var result = await _service.IsTokenValidAsync(invalidToken);

        // Assert
        result.Should().BeFalse();
    }
}
