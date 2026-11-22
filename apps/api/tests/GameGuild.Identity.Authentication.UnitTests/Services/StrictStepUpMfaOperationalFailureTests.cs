using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StrictStepUpMfaOperationalFailureTests
{
    [Fact]
    public async Task BackupStrictPathPropagatesTheOriginalStoreFailureWhileLegacyReturnsFalse()
    {
        var actor = Guid.NewGuid();
        var failure = new InvalidOperationException("store unavailable");
        var repo = new Mock<IUserMfaConfigurationRepository>();
        repo.Setup(value => value.GetByUserIdAsync(actor, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var service = new BackupCodeMfaService(NullLogger<BackupCodeMfaService>.Instance, repo.Object,
            Mock.Of<IMfaAttemptTrackingService>(), new MfaOptions { Enabled = true });
        Assert.False(await service.VerifyBackupCodeAsync(actor, "test-proof"));
        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyBackupCodeForStepUpAsync(actor, "test-proof"));
        Assert.Same(failure, observed);
    }

    [Fact]
    public async Task TotpStrictPathPropagatesTheOriginalStoreFailureWhileLegacyReturnsFalse()
    {
        var actor = Guid.NewGuid();
        var failure = new InvalidOperationException("store unavailable");
        var repo = new Mock<IUserMfaConfigurationRepository>();
        repo.Setup(value => value.GetByUserIdAsync(actor, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var service = new TotpMfaService(NullLogger<TotpMfaService>.Instance, repo.Object,
            Mock.Of<IMfaAttemptTrackingService>(), Mock.Of<IEncryptionService>(), Mock.Of<ITotpReplayStore>(), new MfaOptions { Enabled = true });
        Assert.False(await service.VerifyTotpAsync(actor, "123456"));
        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyTotpForStepUpAsync(actor, "123456"));
        Assert.Same(failure, observed);
    }

    [Fact]
    public async Task StrictFacadePreservesOperationalFailureFromTotpWithoutChangingLegacyBehavior()
    {
        var actor = Guid.NewGuid();
        var failure = new InvalidOperationException("factor unavailable");
        var totp = new Mock<ITotpMfaService>();
        totp.Setup(value => value.VerifyTotpAsync(actor, "123456", null, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        totp.Setup(value => value.VerifyTotpForStepUpAsync(actor, "123456", null, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var facade = new MfaService(NullLogger<MfaService>.Instance, totp.Object, Mock.Of<IBackupCodeMfaService>(),
            Mock.Of<IMfaAttemptTrackingService>(), Mock.Of<IUserRepository>(), Mock.Of<IPasswordHasher>());
        Assert.False((await facade.VerifyMfaAsync(actor, "123456", MfaMethod.Totp)).Success);
        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => facade.VerifyMfaForStepUpAsync(actor, "123456", MfaMethod.Totp));
        Assert.Same(failure, observed);
    }
}
