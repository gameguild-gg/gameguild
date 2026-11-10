using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class LocalSignUpCaptchaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiRegistration_ValidatesCaptchaBeforeCreatingTheAccount(bool allowed)
    {
        var token = Guid.NewGuid().ToString();
        var verifier = new Mock<ITurnstileVerificationService>();
        verifier.Setup(service => service.VerifyAsync(token, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TurnstileVerificationResult(allowed));
        var auth = new Mock<IAuthService>(MockBehavior.Strict);
        if (allowed)
        {
            auth.Setup(service => service.LocalSignUpAsync(It.IsAny<LocalSignUpRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SignInResponse { Success = false, Message = "Account unavailable" });
        }
        var handler = new LocalSignUpHandler(auth.Object, Mock.Of<IUserRepository>(), Mock.Of<ISender>(),
            NullLogger<LocalSignUpHandler>.Instance, turnstileVerification: verifier.Object);
        var result = await handler.Handle(new LocalSignUpCommand
        {
            Username = "reader", Email = "reader@example.invalid", Password = Guid.NewGuid().ToString(), TurnstileToken = token
        }, CancellationToken.None);

        Assert.False(result.Success);
        if (!allowed) { Assert.Contains("Bot verification failed", result.Message); }
        verifier.Verify(service => service.VerifyAsync(token, null, It.IsAny<CancellationToken>()), Times.Once());
        auth.Verify(service => service.LocalSignUpAsync(It.IsAny<LocalSignUpRequest>(), It.IsAny<CancellationToken>()),
            allowed ? Times.Once() : Times.Never());
    }
}
