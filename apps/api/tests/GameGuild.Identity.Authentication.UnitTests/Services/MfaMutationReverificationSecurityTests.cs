using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class MfaMutationReverificationSecurityTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BackupAndPushMutations_RequireSuccessfulReverification(bool push, bool verified)
    {
        var user = Guid.NewGuid();
        var password = Guid.NewGuid().ToString();
        var proof = new Mock<IMfaReverificationService>();
        var operation = push ? "push-device-register" : "backup-codes-regenerate";
        proof.Setup(service => service.VerifyPasswordAsync(user, password, operation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verified);
        var mfa = new Mock<IMfaService>(MockBehavior.Strict);
        var delivery = new Mock<IPushMfaService>(MockBehavior.Strict);
        if (verified)
        {
            mfa.Setup(service => service.GenerateBackupCodesAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync([Guid.NewGuid().ToString()]);
            delivery.Setup(service => service.RegisterDeviceAsync(user, "device", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        if (push)
        {
            var handler = new SmsPushMfaCommandHandler(Mock.Of<ISmsMfaService>(), delivery.Object, proof.Object);
            Assert.Equal(verified, await handler.Handle(new RegisterPushMfaDeviceCommand(user, "device", password), CancellationToken.None));
            delivery.Verify(service => service.RegisterDeviceAsync(user, "device", It.IsAny<CancellationToken>()),
                verified ? Times.Once() : Times.Never());
        }
        else
        {
            var handler = new MfaMutationCommandHandler(mfa.Object, proof.Object);
            var codes = await handler.Handle(new RegenerateMfaBackupCodesCommand(user, password), CancellationToken.None);
            Assert.Equal(verified ? 1 : 0, codes.Length);
            mfa.Verify(service => service.GenerateBackupCodesAsync(user, It.IsAny<CancellationToken>()),
                verified ? Times.Once() : Times.Never());
        }
        proof.Verify(service => service.VerifyPasswordAsync(user, password, operation, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task MissingVerifier_DeniesBothMutations()
    {
        var user = Guid.NewGuid();
        var mfa = new Mock<IMfaService>(MockBehavior.Strict);
        var push = new Mock<IPushMfaService>(MockBehavior.Strict);
        Assert.Empty(await new MfaMutationCommandHandler(mfa.Object).Handle(
            new RegenerateMfaBackupCodesCommand(user, Guid.NewGuid().ToString()), CancellationToken.None));
        Assert.False(await new SmsPushMfaCommandHandler(Mock.Of<ISmsMfaService>(), push.Object).Handle(
            new RegisterPushMfaDeviceCommand(user, "device", Guid.NewGuid().ToString()), CancellationToken.None));
    }

    [Theory]
    [InlineData("missing-user")]
    [InlineData("passwordless")]
    [InlineData("wrong-password")]
    [InlineData("valid")]
    public async Task PasswordProof_RequiresAnExistingPasswordAndCorrectValue(string scenario)
    {
        var user = Guid.NewGuid();
        var hash = Guid.NewGuid().ToString();
        var password = Guid.NewGuid().ToString();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario == "missing-user" ? null : new User
            {
                Id = user, PasswordHash = scenario == "passwordless" ? null : hash
            });
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(service => service.VerifyPassword(hash, password)).Returns(scenario == "valid");
        var verifier = new MfaReverificationService(users.Object, hasher.Object, NullLogger<MfaReverificationService>.Instance);
        Assert.Equal(scenario == "valid", await verifier.VerifyPasswordAsync(user, password, "test"));
    }
    [Fact]
    public async Task BackupRegeneration_ExpectedConfigurationDenialReturnsEmptyAfterReverification()
    {
        var user = Guid.NewGuid();
        var password = Guid.NewGuid().ToString();
        var proof = new Mock<IMfaReverificationService>(MockBehavior.Strict);
        proof.Setup(service => service.VerifyPasswordAsync(user, password, "backup-codes-regenerate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mfa = new Mock<IMfaService>(MockBehavior.Strict);
        mfa.Setup(service => service.GenerateBackupCodesAsync(user, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MfaConfigurationDeniedException("MFA must be enabled to generate backup codes"));

        var result = await new MfaMutationCommandHandler(mfa.Object, proof.Object).Handle(
            new RegenerateMfaBackupCodesCommand(user, password), CancellationToken.None);

        Assert.Empty(result);
        proof.Verify(service => service.VerifyPasswordAsync(user, password, "backup-codes-regenerate", It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task BackupRegeneration_UnexpectedFailuresStillPropagate(int scenario)
    {
        var user = Guid.NewGuid();
        var password = Guid.NewGuid().ToString();
        using var cancellation = new CancellationTokenSource();
        Exception expected = scenario switch
        {
            0 => new InvalidOperationException("Synthetic backup storage failure"),
            1 => new UnauthorizedAccessException("Synthetic backup permission denial"),
            _ => new OperationCanceledException(cancellation.Token)
        };
        var proof = new Mock<IMfaReverificationService>(MockBehavior.Strict);
        proof.Setup(service => service.VerifyPasswordAsync(user, password, "backup-codes-regenerate", cancellation.Token))
            .ReturnsAsync(true);
        var mfa = new Mock<IMfaService>(MockBehavior.Strict);
        mfa.Setup(service => service.GenerateBackupCodesAsync(user, cancellation.Token)).ThrowsAsync(expected);

        var actual = await Record.ExceptionAsync(() => new MfaMutationCommandHandler(mfa.Object, proof.Object).Handle(
            new RegenerateMfaBackupCodesCommand(user, password), cancellation.Token));

        Assert.Same(expected, actual);
        proof.Verify(service => service.VerifyPasswordAsync(user, password, "backup-codes-regenerate", cancellation.Token), Times.Once());
    }

}
