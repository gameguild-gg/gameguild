using FluentAssertions;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public class VerifyWeb3SignatureHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMapCommandToVerificationRequest_AndReturnAuthServiceResponse()
    {
        var authService = new Mock<IAuthService>();
        Web3VerificationRequest? capturedRequest = null;
        var expectedResponse = new SignInResponse
        {
            Success = true,
            Message = "verified",
            Email = "wallet@example.com"
        };
        using var cancellationTokenSource = new CancellationTokenSource();

        authService
            .Setup(service => service.VerifyWeb3SignatureAsync(It.IsAny<Web3VerificationRequest>(), cancellationTokenSource.Token))
            .Callback<Web3VerificationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(expectedResponse);

        var handler = new VerifyWeb3SignatureHandler(authService.Object);
        var command = new VerifyWeb3SignatureCommand
        {
            WalletAddress = "0xabc123",
            Signature = "signed-payload",
            Nonce = "nonce-value",
            ChainId = "1",
            DeviceFingerprint = "device-1",
            TenantId = Guid.NewGuid()
        };

        var result = await handler.Handle(command, cancellationTokenSource.Token);

        result.Should().BeSameAs(expectedResponse);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.WalletAddress.Should().Be(command.WalletAddress);
        capturedRequest.Signature.Should().Be(command.Signature);
        capturedRequest.Challenge.Should().Be(command.Nonce);

        authService.Verify(
            service => service.VerifyWeb3SignatureAsync(It.IsAny<Web3VerificationRequest>(), cancellationTokenSource.Token),
            Times.Once);
    }
}

public class UserSignedInEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldLogUnknownIp_WhenNotificationDoesNotProvideOne()
    {
        var logger = new TestLogger<UserSignedInEventHandler>();
        var handler = new UserSignedInEventHandler(logger);
        var notification = new TestUserSignedInEvent(
            Guid.NewGuid(),
            "user@example.com",
            "WebAuthn",
            null,
            "Firefox",
            new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc));

        await handler.Handle(notification, CancellationToken.None);

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("Unknown");
        logger.Entries[0].Message.Should().Contain(notification.Email);
        logger.Entries[0].Message.Should().Contain(notification.AuthMethod);
    }

    private sealed record TestUserSignedInEvent(
        Guid UserId,
        string Email,
        string AuthMethod,
        string? IpAddress,
        string? UserAgent,
        DateTime Timestamp)
        : UserSignedInEvent(UserId, Email, AuthMethod, IpAddress, UserAgent, Timestamp);
}

public class SendEmailVerificationRequestedHandlerTests
{
    [Fact]
    public async Task Handle_ShouldLogVerificationLink_WhenEmailSenderIsMissing()
    {
        var logger = new TestLogger<SendEmailVerificationRequestedHandler>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://portal.modu.test/"
            })
            .Build();
        var handler = new SendEmailVerificationRequestedHandler(logger, null, configuration);
        var notification = new EmailVerificationRequestedNotification
        {
            Email = "user@example.com",
            Token = "token-123",
            UserName = null
        };

        await handler.Handle(notification, CancellationToken.None);

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("Unknown");
        logger.Entries[0].Message.Should().Contain("https://portal.modu.test/verify-email?token=token-123");
    }

    [Fact]
    public async Task Handle_ShouldSendVerificationEmail_UsingEmailAsFallbackRecipientName()
    {
        var logger = new TestLogger<SendEmailVerificationRequestedHandler>();
        var emailSender = new Mock<IEmailSender>();
        EmailMessage? capturedMessage = null;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://app.modu.test/"
            })
            .Build();
        var notification = new EmailVerificationRequestedNotification
        {
            Email = "user@example.com",
            Token = "token-abc",
            UserName = "  "
        };

        emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => capturedMessage = message)
            .Returns(Task.CompletedTask);

        var handler = new SendEmailVerificationRequestedHandler(logger, emailSender.Object, configuration);

        await handler.Handle(notification, CancellationToken.None);

        capturedMessage.Should().NotBeNull();
        capturedMessage!.ToEmail.Should().Be(notification.Email);
        capturedMessage.ToName.Should().Be(notification.Email);
        capturedMessage.Subject.Should().Be("Verify your GameGuild email address");
        capturedMessage.PlainTextContent.Should().Contain("https://app.modu.test/verify-email?token=token-abc");
        capturedMessage.HtmlContent.Should().Contain("Verify your email address");

        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Information &&
            entry.Message.Contains("Verification email delivered to user@example.com"));
    }

    [Fact]
    public async Task Handle_ShouldLogErrorAndRethrow_WhenEmailDeliveryFails()
    {
        var logger = new TestLogger<SendEmailVerificationRequestedHandler>();
        var emailSender = new Mock<IEmailSender>();
        var expectedException = new InvalidOperationException("smtp offline");
        var handler = new SendEmailVerificationRequestedHandler(logger, emailSender.Object, null);
        var notification = new EmailVerificationRequestedNotification
        {
            Email = "user@example.com",
            Token = "token-error",
            UserName = "User"
        };

        emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var act = () => handler.Handle(notification, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("smtp offline");
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error &&
            entry.Exception == expectedException &&
            entry.Message.Contains(notification.Email));
    }
}

file sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}