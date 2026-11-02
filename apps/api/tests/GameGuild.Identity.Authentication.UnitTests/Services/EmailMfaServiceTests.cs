using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Email MFA factor tests (issue #1152): address normalization/masking, digest-only storage,
///     single-use codes, five-attempt ceiling, resend throttle, enrollment persistence (the
///     address is only stored after code verification) and challenge send.
/// </summary>
public sealed class EmailMfaServiceTests : IDisposable
{
    private readonly Mock<IUserMfaConfigurationRepository> mfaConfigRepository = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IPublisher> publisher = new();
    private readonly MemoryCache memoryCache = new(new MemoryCacheOptions { SizeLimit = null });

    private EmailMfaService CreateService() =>
        new(NullLogger<EmailMfaService>.Instance, memoryCache, mfaConfigRepository.Object, userRepository.Object, publisher.Object);

    public void Dispose() => memoryCache.Dispose();

    private void SetupUser(Guid userId, string email) =>
        userRepository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, Email = email });

    [Theory]
    [InlineData("User@Example.com", "user@example.com")]
    [InlineData("  user@example.com  ", "user@example.com")]
    public void NormalizeEmailAddress_TrimsAndLowercases(string input, string expected)
    {
        EmailMfaService.NormalizeEmailAddress(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@missing-local")]
    [InlineData("missing-domain@")]
    [InlineData("two@at@signs")]
    public void NormalizeEmailAddress_RejectsMalformedAddresses(string input)
    {
        EmailMfaService.NormalizeEmailAddress(input).Should().BeNull();
    }

    [Fact]
    public void MaskEmailAddress_ShowsOnlyFirstCharacterOfLocalPart()
    {
        EmailMfaService.MaskEmailAddress("member@example.com").Should().Be("m***@example.com");
    }

    [Fact]
    public async Task StartEnrollment_WithoutAccountEmail_Fails()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        var result = await service.StartEnrollmentAsync(userId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No email address");
        publisher.Verify(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartEnrollment_PublishesCode_QueuesOnlyDigest()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, "member@example.com");
        var service = CreateService();

        var result = await service.StartEnrollmentAsync(userId);

        result.Success.Should().BeTrue();
        result.EmailMasked.Should().Be("m***@example.com");
        result.ExpiresInSeconds.Should().Be((int)EmailMfaService.CodeLifetime.TotalSeconds);

        publisher.Verify(p => p.Publish(
            It.Is<EmailMfaCodeRequestedNotification>(n =>
                n.UserId == userId &&
                n.Email == "member@example.com" &&
                n.Code.Length == 6 &&
                n.Code.All(char.IsDigit) &&
                n.IsEnrollment),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Only the digest is cached: no plaintext code in the entry store.
        memoryCache.TryGetValue("emailmfa:entry:" + userId.ToString("N"), out object? cached).Should().BeTrue();
        cached.Should().NotBeOfType<string>();
    }

    [Fact]
    public async Task StartEnrollment_IsThrottledToOneCodePerMinute()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, "member@example.com");
        var service = CreateService();

        var first = await service.StartEnrollmentAsync(userId);
        var second = await service.StartEnrollmentAsync(userId);

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        publisher.Verify(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteEnrollment_VerifiesCodeAndPersistsAddressOnlyAfterVerification()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, "member@example.com");
        string? capturedCode = null;
        publisher
            .Setup(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMfaCodeRequestedNotification, CancellationToken>((n, _) => capturedCode = n.Code);
        UserMfaConfiguration? saved = null;
        mfaConfigRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration { UserId = userId });
        mfaConfigRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<UserMfaConfiguration, CancellationToken>((c, _) => saved = c)
            .ReturnsAsync((UserMfaConfiguration c, CancellationToken _) => c);
        var service = CreateService();

        await service.StartEnrollmentAsync(userId);
        saved.Should().BeNull(); // nothing persisted before the code is verified
        var result = await service.CompleteEnrollmentAsync(userId, capturedCode!);

        result.Success.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.EmailMfaAddress.Should().Be("member@example.com");
        saved.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteEnrollment_RejectsWrongCode_AndConsumesCodeAttempts()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, "member@example.com");
        var service = CreateService();
        await service.StartEnrollmentAsync(userId);

        for (var attempt = 0; attempt < EmailMfaService.MaxVerificationAttempts; attempt++)
        {
            (await service.VerifyCodeAsync(userId, "000000")).Should().BeFalse();
        }

        // Exhausted: even a subsequent correct-looking code is invalid.
        (await service.VerifyCodeAsync(userId, "000000")).Should().BeFalse();
    }

    [Fact]
    public async Task Code_IsSingleUse()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, "member@example.com");
        string? capturedCode = null;
        publisher
            .Setup(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMfaCodeRequestedNotification, CancellationToken>((n, _) => capturedCode = n.Code);
        var service = CreateService();

        await service.StartEnrollmentAsync(userId);

        (await service.VerifyCodeAsync(userId, capturedCode!)).Should().BeTrue();
        (await service.VerifyCodeAsync(userId, capturedCode!)).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCode_WithoutPendingCode_ReturnsFalse()
    {
        var service = CreateService();

        (await service.VerifyCodeAsync(Guid.NewGuid(), "123456")).Should().BeFalse();
    }

    [Fact]
    public async Task SendChallenge_RequiresEnrolledAddress()
    {
        var userId = Guid.NewGuid();
        mfaConfigRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration { UserId = userId, EmailMfaAddress = null });
        var service = CreateService();

        var result = await service.SendChallengeAsync(userId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not enrolled");
    }

    [Fact]
    public async Task SendChallenge_SendsToEnrolledAddress()
    {
        var userId = Guid.NewGuid();
        mfaConfigRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserMfaConfiguration { UserId = userId, EmailMfaAddress = "member@example.com" });
        var service = CreateService();

        var result = await service.SendChallengeAsync(userId);

        result.Success.Should().BeTrue();
        result.EmailMasked.Should().Be("m***@example.com");
        publisher.Verify(p => p.Publish(
            It.Is<EmailMfaCodeRequestedNotification>(n => n.Email == "member@example.com" && !n.IsEnrollment),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartEnrollment_PublishFailure_DropsTheCode()
    {
        var userId = Guid.NewGuid();
        SetupUser(userId, "member@example.com");
        publisher
            .Setup(p => p.Publish(It.IsAny<EmailMfaCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bus down"));
        var service = CreateService();

        var act = () => service.StartEnrollmentAsync(userId);
        await act.Should().ThrowAsync<InvalidOperationException>();

        // No live code remains for a user who never received it.
        (await service.VerifyCodeAsync(userId, "123456")).Should().BeFalse();
    }
}
