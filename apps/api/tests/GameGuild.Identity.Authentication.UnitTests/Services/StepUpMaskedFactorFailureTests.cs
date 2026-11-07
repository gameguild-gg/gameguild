using System.Text.Json;
using GameGuild;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static GameGuild.Identity.Authentication.UnitTests.Services.StepUpReceiptServiceTestHarness;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpMaskedFactorFailureTests
{
    [Fact]
    public async Task AuditFailureAfterActualBackupConsumptionMustNotCommitTheFactorWriteAsAnOrdinaryDenial()
    {
        var state = await CreateAsync(failSuccessAudit: true);
        var row = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), state.Actor, Guid.NewGuid(), row, state.Mfa);
        var challenge = await service.CreateChallengeAsync(Binding);
        StepUpVerificationOutcome? outcome = null;
        var error = await Record.ExceptionAsync(async () => outcome = await new StepUpCommandHandler(service).Handle(
            new VerifyStepUpChallengeCommand(challenge.ChallengeId,
                new StepUpVerification(MfaMethod.BackupCode, state.Code)), CancellationToken.None));
        // The actual backup-code verifier already executed its repository update;
        // its audit exception was converted into a plain false by the legacy facade.
        Assert.Single(state.Writes);
        Assert.NotEqual(state.OriginalCodes, state.Writes.Single());
        if (error is not null)
        {
            Assert.Same(state.RequiredAuditFailure, error);
        }
        else
        {
            var returned = Assert.IsAssignableFrom<StepUpVerificationOutcome>(outcome);
            Assert.False(returned.Success);
            Assert.Null(returned.Receipt);
            Assert.True(CommandOutcome.ShouldRollback(returned),
                "A required audit failure after consuming a valid backup code must roll back that write.");
        }
    }

    [Fact]
    public async Task OrdinaryInvalidBackupCodeCanCommitTheAttemptWithoutConsumingACode()
    {
        var state = await CreateAsync(failSuccessAudit: false);
        var row = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), state.Actor, Guid.NewGuid(), row, state.Mfa);
        var challenge = await service.CreateChallengeAsync(Binding);
        var outcome = await new StepUpCommandHandler(service).Handle(new VerifyStepUpChallengeCommand(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.BackupCode, "different-test-code")), CancellationToken.None);
        Assert.False(outcome.Success);
        Assert.Null(outcome.Receipt);
        Assert.Empty(state.Writes);
        Assert.False(CommandOutcome.ShouldRollback(outcome));
    }

    [Fact]
    public async Task ActualBackupCodeVerificationWithoutOperationalFailureProducesAReceipt()
    {
        var state = await CreateAsync(failSuccessAudit: false);
        var row = new RecordingStepUpChallengeStore();
        var service = CreateService(Guid.NewGuid(), state.Actor, Guid.NewGuid(), row, state.Mfa);
        var challenge = await service.CreateChallengeAsync(Binding);
        var outcome = await new StepUpCommandHandler(service).Handle(new VerifyStepUpChallengeCommand(
            challenge.ChallengeId, new StepUpVerification(MfaMethod.BackupCode, state.Code)), CancellationToken.None);
        Assert.True(outcome.Success);
        Assert.NotNull(outcome.Receipt);
        Assert.Single(state.Writes);
        await service.ConsumeAsync(Binding, outcome.Receipt.Receipt);
    }

    private static async Task<State> CreateAsync(bool failSuccessAudit)
    {
        var actor = Guid.NewGuid();
        var config = new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = actor, IsEnabled = true, IsSetupComplete = true };
        var writes = new List<string>();
        var repo = new Mock<IUserMfaConfigurationRepository>();
        repo.Setup(value => value.GetByUserIdAsync(actor, It.IsAny<CancellationToken>())).ReturnsAsync(config);
        repo.Setup(value => value.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<UserMfaConfiguration, CancellationToken>((updated, _) => writes.Add(updated.BackupCodes!))
            .ReturnsAsync((UserMfaConfiguration updated, CancellationToken _) => updated);
        var attempts = new Mock<IMfaAttemptTrackingService>();
        attempts.Setup(value => value.RecordMfaAttemptAsync(actor, MfaMethod.BackupCode, It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var auditFailure = new InvalidOperationException("required audit persistence unavailable");
        if (failSuccessAudit)
        {
            attempts.Setup(value => value.RecordMfaAttemptAsync(actor, MfaMethod.BackupCode, true, null, null,
                It.IsAny<CancellationToken>())).ThrowsAsync(auditFailure);
        }
        var backup = new BackupCodeMfaService(NullLogger<BackupCodeMfaService>.Instance, repo.Object,
            attempts.Object, new MfaOptions { Enabled = true });
        const string code = "TEST-ONLY-BACKUP";
        // Use the actual public setup writer, including its issued-set encoding.
        await backup.StoreBackupCodesForSetupAsync(actor, new[] { code });
        writes.Clear();
        var originalCodes = Assert.IsType<string>(config.BackupCodes);
        var mfa = new MfaService(NullLogger<MfaService>.Instance, Mock.Of<ITotpMfaService>(), backup,
            attempts.Object, Mock.Of<IUserRepository>(), Mock.Of<IPasswordHasher>(), mfaOptions: new MfaOptions { Enabled = true });
        return new State(actor, code, originalCodes, mfa, writes, auditFailure);
    }

    private sealed record State(Guid Actor, string Code, string OriginalCodes, MfaService Mfa, List<string> Writes, Exception RequiredAuditFailure);
}
