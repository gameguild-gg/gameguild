using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using GameGuild;
using GameGuild.API.Database;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

using GameGuild.API.IntegrationTests.Infrastructure;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class StepUpVerificationTransactionPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    private readonly DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(fixture.ConnectionString).Options;

    [Fact]
    public async Task NativeHostOrdinaryFactorDenialCommitsOneAttempt()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(x => x.VerifyMfaForStepUpAsync(f.Actor, "bad-proof", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Failure("Invalid proof"));
        var service = f.Service(mfa.Object);
        var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var outcome = await f.Run(new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "bad-proof")),
            () => new StepUpCommandHandler(service).Handle(new VerifyStepUpChallengeCommand(challenge.ChallengeId,
                new StepUpVerification(MfaMethod.Totp, "bad-proof")), default));
        Assert.False(outcome.Success); Assert.Null(outcome.Receipt);
        Assert.Equal(1, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
    }

    [Fact]
    public async Task NativeHostUnmarkedFailureControlRollsBackAttempt()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var service = f.Service(Mock.Of<IMfaService>());
        var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "proof"));
        var outcome = await f.Run(command, async () =>
        {
            Assert.True(await f.Store.TryReserveVerificationAttemptAsync(challenge.ChallengeId, f.Subject, DateTimeOffset.UtcNow, default));
            return new UnmarkedDenial();
        });
        Assert.False(outcome.Success); Assert.Equal(0, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
    }

    [Fact]
    public async Task NativeHostProviderInfrastructureFaultRollsBackAttempt()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var fault = new InvalidOperationException("provider persistence unavailable");
        var mfa = new Mock<IMfaService>();
        mfa.Setup(x => x.VerifyMfaForStepUpAsync(f.Actor, "proof", MfaMethod.Totp, It.IsAny<CancellationToken>())).ThrowsAsync(fault);
        var service = f.Service(mfa.Object); var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "proof"));
        Assert.Same(fault, await Record.ExceptionAsync(async () => await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default))));
        Assert.Equal(0, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
        Assert.Null((await f.ReadChallenge(challenge.ChallengeId)).ReceiptHash);
    }

    [Fact]
    public async Task NativeHostRequestCancellationAfterReservationStillRollsBack()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        using var cancellation = new CancellationTokenSource();
        var fault = new OperationCanceledException(cancellation.Token);
        var mfa = new Mock<IMfaService>();
        mfa.Setup(x => x.VerifyMfaForStepUpAsync(f.Actor, "proof", MfaMethod.Totp, cancellation.Token))
            .Callback(() => cancellation.Cancel()).ThrowsAsync(fault);
        var service = f.Service(mfa.Object); var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "proof"));
        Assert.Same(fault, await Record.ExceptionAsync(async () => await f.Run(command,
            () => new StepUpCommandHandler(service).Handle(command, cancellation.Token), cancellation.Token)));
        Assert.Equal(0, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
    }

    [Fact]
    public async Task NativeHostSuccessCommitsOneReceiptAndActualAuditsRemainEnabled()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var mfa = new Mock<IMfaService>();
        mfa.Setup(x => x.VerifyMfaForStepUpAsync(f.Actor, "proof", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Successful("Verified"));
        var service = f.Service(mfa.Object); var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "proof"));
        var outcome = await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default));
        Assert.True(outcome.Success); Assert.NotNull(outcome.Receipt);
        var stored = await f.ReadChallenge(challenge.ChallengeId);
        Assert.Equal(1, stored.VerificationAttempts);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(outcome.Receipt.Receipt))).ToLowerInvariant(), stored.ReceiptHash);
        Assert.True((bool)typeof(ApplicationDbContext).GetField("_entityChangeAuditCaptureEnabled", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Context)!);
        Assert.True(await f.Context.Set<EntityChangeAuditRecord>().AnyAsync(x => x.EntityName == nameof(StepUpChallenge) && x.KeyId == challenge.ChallengeId.ToString()));
    }

    [Fact]
    public async Task NativeHostRealBackupConsumptionAndRequiredAuditFaultRollBackTogether()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var state = await f.BackupState();
        var service = f.Service(state.Mfa); var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var fault = new InvalidOperationException("required backup audit write unavailable");
        var observedConsumedInsideTransaction = false;
        state.Attempts.Setup(x => x.RecordMfaAttemptAsync(f.Actor, MfaMethod.BackupCode, true, null, null, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                var inside = await f.Context.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(x => x.UserId == f.Actor);
                observedConsumedInsideTransaction = inside.BackupCodes != state.OriginalCodes;
                Assert.True(observedConsumedInsideTransaction);
                throw fault;
            });
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.BackupCode, state.Code));
        Assert.Same(fault, await Record.ExceptionAsync(async () => await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default))));
        Assert.True(observedConsumedInsideTransaction);
        Assert.Equal(state.OriginalCodes, (await f.ReadMfa()).BackupCodes);
        Assert.Equal(0, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
        Assert.Null((await f.ReadChallenge(challenge.ChallengeId)).ReceiptHash);
    }

    [Fact]
    public async Task NativeHostRealInvalidBackupProofCommitsBudgetWithoutConsumingCode()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var state = await f.BackupState();
        var service = f.Service(state.Mfa); var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.BackupCode, "different-test-code"));
        var outcome = await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default));
        Assert.False(outcome.Success); Assert.Null(outcome.Receipt);
        Assert.Equal(state.OriginalCodes, (await f.ReadMfa()).BackupCodes);
        Assert.Equal(1, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
    }

    [Fact]
    public async Task NativeHostRealValidBackupProofCommitsConsumptionAndReceipt()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var state = await f.BackupState();
        var service = f.Service(state.Mfa); var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.BackupCode, state.Code));
        var outcome = await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default));
        Assert.True(outcome.Success); Assert.NotNull(outcome.Receipt);
        Assert.NotEqual(state.OriginalCodes, (await f.ReadMfa()).BackupCodes);
        Assert.Equal(1, (await f.ReadChallenge(challenge.ChallengeId)).VerificationAttempts);
        Assert.NotNull((await f.ReadChallenge(challenge.ChallengeId)).ReceiptHash);
    }

    [Fact]
    public async Task NativeHostValidFactorExpiringDuringVerificationRollsBackAttempt()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var clock = new ProbeClock(DateTimeOffset.UtcNow);
        var mfa = new Mock<IMfaService>();
        var proofReached = false;
        mfa.Setup(x => x.VerifyMfaForStepUpAsync(f.Actor, "valid-proof", MfaMethod.Totp, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                proofReached = true;
                clock.Advance(TimeSpan.FromMinutes(6));
                return Task.FromResult(MfaVerificationResult.Successful("Verified"));
            });
        var service = f.Service(mfa.Object, clock);
        var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.Totp, "valid-proof"));
        await Assert.ThrowsAsync<StepUpChallengeUnavailableException>(async () =>
        {
            await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default));
        });
        Assert.True(proofReached);
        var stored = await f.ReadChallenge(challenge.ChallengeId);
        Assert.Equal(0, stored.VerificationAttempts); Assert.Null(stored.VerifiedAt); Assert.Null(stored.ReceiptHash);
    }

    [Fact]
    public async Task NativeHostRealBackupConsumedBeforeExpiryFailureRollsBackFactorAndAttempt()
    {
        await using var f = await Fixture.Create(options);
        f.ActivateActorInCaller();
        var clock = new ProbeClock(DateTimeOffset.UtcNow);
        var state = await f.BackupState();
        var observedConsumedBeforeExpiryFailure = false;
        state.Attempts.Setup(x => x.RecordMfaAttemptAsync(f.Actor, MfaMethod.BackupCode, true, null, null, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                var inside = await f.Context.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(x => x.UserId == f.Actor);
                observedConsumedBeforeExpiryFailure = inside.BackupCodes != state.OriginalCodes;
                Assert.True(observedConsumedBeforeExpiryFailure);
                clock.Advance(TimeSpan.FromMinutes(6));
            });
        var service = f.Service(state.Mfa, clock);
        var challenge = await service.CreateChallengeAsync(Fixture.Binding);
        var command = new VerifyStepUpChallengeCommand(challenge.ChallengeId, new StepUpVerification(MfaMethod.BackupCode, state.Code));
        await Assert.ThrowsAsync<StepUpChallengeUnavailableException>(async () =>
        {
            await f.Run(command, () => new StepUpCommandHandler(service).Handle(command, default));
        });
        Assert.True(observedConsumedBeforeExpiryFailure);
        Assert.Equal(state.OriginalCodes, (await f.ReadMfa()).BackupCodes);
        var stored = await f.ReadChallenge(challenge.ChallengeId);
        Assert.Equal(0, stored.VerificationAttempts); Assert.Null(stored.VerifiedAt); Assert.Null(stored.ReceiptHash);
    }

    private sealed record UnmarkedDenial() : StepUpVerificationOutcome(false, null);

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> options;
        private readonly ActorContextAccessor actor = new();
        private readonly UseCaseOperationContextAccessor operations = new();
        public ApplicationDbContext Context { get; }
        public Guid Actor { get; } = Guid.NewGuid();
        public StepUpSubject Subject { get; }
        public PostgreSqlStepUpChallengeStore Store { get; }
        public static readonly StepUpOperationBinding Binding = new("identity.operation.confirm", "resource/native-host-proof", new string('a', 64));
        private Fixture(DbContextOptions<ApplicationDbContext> options)
        {
            this.options = options;
            Subject = new StepUpSubject(Guid.NewGuid(), Actor, Guid.NewGuid());
            actor.SetActorContext(ActorContextBuilder.ForUser(Actor).WithTenantId(Subject.TenantId).WithAttribute("session_id", Subject.SessionId.ToString()).Build());
            Context = new ApplicationDbContext(options, null, operations);
            Store = new PostgreSqlStepUpChallengeStore(Context);
        }
        public static async Task<Fixture> Create(DbContextOptions<ApplicationDbContext> options)
        {
            var f = new Fixture(options);
            f.Context.Set<User>().Add(new User { Id = f.Actor, Email = "native-host-" + f.Actor.ToString("N") + "@example.test", Name = "Native host probe", Username = "native_" + f.Actor.ToString("N"), IsActive = true });
            await f.Context.SaveChangesAsync();
            return f;
        }
        public void ActivateActorInCaller() => actor.SetActorContext(ActorContextBuilder.ForUser(Actor).WithTenantId(Subject.TenantId).WithAttribute("session_id", Subject.SessionId.ToString()).Build());
        public StepUpReceiptService Service(IMfaService mfa, TimeProvider? clock = null) => new(actor, Store, mfa, Mock.Of<IWebAuthnAuthenticationService>(), clock ?? TimeProvider.System);
        public async Task<StepUpVerificationOutcome> Run(VerifyStepUpChallengeCommand command,
            RequestHandlerDelegate<StepUpVerificationOutcome> next, CancellationToken cancellationToken = default)
        {
            var generic = typeof(ApplicationDbContext).Assembly.GetType("GameGuild.API.Eventing.UseCaseOperationBehavior`2", throwOnError: true)!;
            var type = generic.MakeGenericType(typeof(VerifyStepUpChallengeCommand), typeof(StepUpVerificationOutcome));
            var registryType = typeof(ApplicationDbContext).Assembly.GetType("GameGuild.API.Eventing.UseCaseEventContractRegistry", throwOnError: true)!;
            var verifierType = typeof(ApplicationDbContext).Assembly.GetType("GameGuild.API.Eventing.UseCaseEventVerifier", throwOnError: true)!;
            var registry = (IUseCaseEventContractRegistry)Activator.CreateInstance(registryType, nonPublic: true)!;
            var verifier = (IUseCaseEventVerifier)Activator.CreateInstance(verifierType,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, [Context], null)!;
            var behavior = Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null, args: [Context, actor, operations, registry, verifier, null], culture: null)!;
            try
            {
                return await (Task<StepUpVerificationOutcome>)type.GetMethod("Handle")!.Invoke(behavior, [command, next, cancellationToken])!;
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw;
            }
        }
        public async Task<StepUpChallenge> ReadChallenge(Guid id)
        {
            await using var fresh = new ApplicationDbContext(options, null, new UseCaseOperationContextAccessor());
            return await fresh.Set<StepUpChallenge>().AsNoTracking().SingleAsync(x => x.Id == id);
        }
        public async Task<UserMfaConfiguration> ReadMfa()
        {
            await using var fresh = new ApplicationDbContext(options, null, new UseCaseOperationContextAccessor());
            return await fresh.Set<UserMfaConfiguration>().AsNoTracking().SingleAsync(x => x.UserId == Actor);
        }
        public async Task<BackupFixture> BackupState()
        {
            var repo = new UserMfaConfigurationRepository(Context);
            await repo.CreateAsync(new UserMfaConfiguration { UserId = Actor, IsEnabled = true, IsSetupComplete = true });
            var attempts = new Mock<IMfaAttemptTrackingService>();
            attempts.Setup(x => x.RecordMfaAttemptAsync(Actor, MfaMethod.BackupCode, It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var backup = new BackupCodeMfaService(NullLogger<BackupCodeMfaService>.Instance, repo, attempts.Object, new MfaOptions { Enabled = true });
            const string code = "TEST-ONLY-NATIVE-HOST-BACKUP";
            await backup.StoreBackupCodesForSetupAsync(Actor, [code]);
            var original = Assert.IsType<string>((await repo.GetByUserIdAsync(Actor))!.BackupCodes);
            var mfa = new MfaService(NullLogger<MfaService>.Instance, Mock.Of<ITotpMfaService>(), backup,
                attempts.Object, Mock.Of<IUserRepository>(), Mock.Of<IPasswordHasher>(), mfaOptions: new MfaOptions { Enabled = true });
            return new BackupFixture(code, original, mfa, attempts);
        }
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
    private sealed record BackupFixture(string Code, string OriginalCodes, MfaService Mfa, Mock<IMfaAttemptTrackingService> Attempts);

    private sealed class ProbeClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;
        public override DateTimeOffset GetUtcNow() => current;
        public void Advance(TimeSpan by) => current = current.Add(by);
    }
}
