using System.Net;
using System.Security.Cryptography;
using GameGuild.API.Core.Filters;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PasswordSignInCaptchaAdmissionPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    public async Task MissingProofIsDeniedOnlyAfterTheConfiguredSourceFailureThreshold(int failures, bool denied)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var (email, context) = await SeedAsync(db, failures);
        var verifier = new Mock<ITurnstileVerificationService>();
        verifier.Setup(service => service.VerifyAsync(null, context.Connection.RemoteIpAddress!.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TurnstileVerificationResult.MissingToken);
        var admission = Admission(db, verifier.Object);

        if (denied)
        {
            await Assert.ThrowsAsync<PasswordSignInCaptchaRequiredException>(() =>
                admission.AdmitAsync(email, context, AuthenticationTimingOrigin.Start(), CancellationToken.None));
        }
        else
        {
            await using var lease = await admission.AdmitAsync(email, context, AuthenticationTimingOrigin.Start(), CancellationToken.None);
        }
        verifier.Verify(service => service.VerifyAsync(null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            denied ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task RequiredFreshProofIsConsumedOnceAndCannotBeReplayed()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var (email, context) = await SeedAsync(db, 2);
        var token = Guid.NewGuid().ToString();
        var uses = 0;
        var verifier = new Mock<ITurnstileVerificationService>();
        verifier.Setup(service => service.VerifyAsync(token, context.Connection.RemoteIpAddress!.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TurnstileVerificationResult(++uses == 1));
        var admission = Admission(db, verifier.Object);

        await using (var lease = await admission.AdmitAsync(email, context, AuthenticationTimingOrigin.Start(), CancellationToken.None, token)) { }
        Assert.Equal(1, uses);
        await Assert.ThrowsAsync<PasswordSignInCaptchaRequiredException>(() =>
            admission.AdmitAsync(email, context, AuthenticationTimingOrigin.Start(), CancellationToken.None, token));
        Assert.Equal(2, uses);
    }

    private static PasswordSignInAdmissionService Admission(ApplicationDbContext db, ITurnstileVerificationService verifier)
    {
        var protection = new Mock<IUserEnumerationProtectionService>();
        protection.Setup(service => service.AddTimingProtectionDelayAsync(It.IsAny<bool>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        return new PasswordSignInAdmissionService(db, new AuthenticationSecurityOptions
        {
            EnableIpThrottling = false, TurnstileRequiredAfterFailedAttempts = 2,
            MaxFailedAttemptsPerHour = 100
        }, protection.Object, NullLogger<PasswordSignInAdmissionService>.Instance, turnstileVerification: verifier);
    }

    private static async Task<(string Email, DefaultHttpContext Context)> SeedAsync(ApplicationDbContext db, int failures)
    {
        var address = RandomNumberGenerator.GetBytes(16);
        address[0] = 0x20; address[1] = 0x01; address[2] = 0x0d; address[3] = 0xb8;
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = new IPAddress(address);
        var email = $"captcha-{Guid.NewGuid():N}@example.invalid";
        for (var index = 0; index < failures; index++)
        {
            db.Add(new AuthenticationAttempt
            {
                Id = Guid.NewGuid(), Email = email, IpAddress = context.Connection.RemoteIpAddress.ToString(),
                IsSuccessful = false, AttemptedAt = DateTime.UtcNow.AddMinutes(-1), FailureReason = "synthetic"
            });
        }
        await db.SaveChangesAsync();
        return (email, context);
    }
}
