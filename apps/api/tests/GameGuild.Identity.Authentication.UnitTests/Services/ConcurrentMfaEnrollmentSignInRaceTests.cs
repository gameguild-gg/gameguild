using System.Collections.Concurrent;
using System.Reflection;
using FluentAssertions;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Concurrent MFA enrollment and sign-in race (issue #1117): a user enabling MFA on one
///     device while another device signs in (or completes enrollment) must never observe or
///     persist a torn MFA state. The settings-path enrollment operations run under the
///     per-subject <see cref="MfaEnrollmentConcurrencyGuard" /> claim, and the sign-in-side
///     reader consumes one row atomically — so every observation is either the old
///     (MFA-off) or the new (MFA-on) state, never a mix, and a re-run setup can never
///     clobber a concurrently completed enrollment.
/// </summary>
public sealed class ConcurrentMfaEnrollmentSignInRaceTests
{
    private static readonly MethodInfo GenerateTotpCodeMethod = typeof(TotpMfaService)
        .GetMethod("GenerateTotpCode", BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>Computes the RFC 6238 code for the service's current time step.</summary>
    private static string TotpCodeFor(string base32Secret)
    {
        var timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        return (string)GenerateTotpCodeMethod.Invoke(null, [base32Secret, timeStep])!;
    }

    private static string PlainSecret(UserMfaConfiguration configuration) =>
        configuration.TotpSecretKey!.StartsWith("enc:", StringComparison.Ordinal)
            ? configuration.TotpSecretKey["enc:".Length..]
            : configuration.TotpSecretKey!;

    /// <summary>
    ///     Thread-safe single-row MFA configuration store: every read and write is atomic
    ///     under one lock (a faithful in-memory stand-in for the EF row), with an injectable
    ///     write delay that widens check-then-act windows so interleavings actually happen.
    /// </summary>
    private sealed class AtomicMfaConfigurationStore(TimeSpan writeDelay)
    {
        private readonly object _sync = new();
        private UserMfaConfiguration? _row;
        public int Reads;
        public int Writes;

        public UserMfaConfiguration? Get(Guid userId)
        {
            lock (_sync)
            {
                Reads++;
                return _row is { } row ? Clone(row) : null;
            }
        }

        public UserMfaConfiguration Create(UserMfaConfiguration configuration)
        {
            lock (_sync)
            {
                Writes++;
                _row = Clone(configuration);
                return Clone(_row);
            }
        }

        public UserMfaConfiguration Update(UserMfaConfiguration configuration)
        {
            // The delay is OUTSIDE the lock: it pauses the caller between its read and the
            // moment the write lands, which is exactly the interleaving window the guard
            // must close (the pause happens while the operation holds the enrollment claim).
            Thread.Sleep(writeDelay);
            lock (_sync)
            {
                Writes++;
                _row = Clone(configuration);
                return Clone(_row);
            }
        }

        private static UserMfaConfiguration Clone(UserMfaConfiguration row) => new()
        {
            Id = row.Id,
            UserId = row.UserId,
            IsEnabled = row.IsEnabled,
            TotpSecretKey = row.TotpSecretKey,
            BackupCodes = row.BackupCodes,
            EnabledAt = row.EnabledAt,
            LastUsedAt = row.LastUsedAt,
            FailedAttempts = row.FailedAttempts,
            LockedOutUntil = row.LockedOutUntil,
            PreferredMethod = row.PreferredMethod,
            QrCodeSetupData = row.QrCodeSetupData,
            IsSetupComplete = row.IsSetupComplete,
            SetupExpiresAt = row.SetupExpiresAt,
            SmsPhoneNumber = row.SmsPhoneNumber,
            PushDeviceToken = row.PushDeviceToken,
            EmailMfaAddress = row.EmailMfaAddress,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt
        };
    }

    private sealed class DelayingMfaConfigurationRepository(AtomicMfaConfigurationStore store)
        : IUserMfaConfigurationRepository
    {
        public Task<UserMfaConfiguration> CreateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default) =>
            Task.Run(() => store.Create(configuration), cancellationToken);

        public Task<UserMfaConfiguration?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.Run(() => store.Get(userId), cancellationToken);

        public Task<UserMfaConfiguration> UpdateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default) =>
            Task.Run(() => store.Update(configuration), cancellationToken);

        public Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Not part of the enrollment race surface.");
        }

        public Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.Get(userId)?.IsEnabled ?? false);

        public Task<MfaMethod?> GetPreferredMethodAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.Get(userId)?.PreferredMethod);

        public Task IncrementFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ResetFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetLockoutAsync(Guid userId, DateTime lockoutUntil, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    ///     Replay store that accepts each (configuration, fingerprint, step) exactly once —
    ///     the semantic of the production PostgreSQL store: a code replayed into a second
    ///     concurrent verify loses.
    /// </summary>
    private sealed class SingleAcceptanceReplayStore : ITotpReplayStore
    {
        private readonly ConcurrentDictionary<(Guid ConfigurationId, string Fingerprint, long Step), byte> _accepted = new();
        private readonly ConcurrentDictionary<(Guid, string), SemaphoreSlim> _configurationLocks = new();

        public async Task<bool> TryAcceptAsync(
            Guid configurationId,
            string secretFingerprint,
            long timeStep,
            DateTimeOffset now,
            Func<CancellationToken, Task> onAccepted,
            CancellationToken cancellationToken)
        {
            var configurationLock = _configurationLocks.GetOrAdd((configurationId, secretFingerprint), static _ => new SemaphoreSlim(1, 1));
            await configurationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_accepted.TryAdd((configurationId, secretFingerprint, timeStep), 0))
                {
                    return false;
                }

                await onAccepted(cancellationToken).ConfigureAwait(false);
                return true;
            }
            finally
            {
                configurationLock.Release();
            }
        }
    }

    private static (TotpMfaService Service, AtomicMfaConfigurationStore Store) CreateSut(TimeSpan writeDelay)
    {
        var store = new AtomicMfaConfigurationStore(writeDelay);
        var repository = new DelayingMfaConfigurationRepository(store);

        var tracking = new Mock<IMfaAttemptTrackingService>();
        tracking.Setup(service => service.IsLockedOut(It.IsAny<UserMfaConfiguration>())).Returns(false);
        tracking.Setup(service => service.RecordMfaAttemptAsync(
            It.IsAny<Guid>(), It.IsAny<MfaMethod>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        tracking.Setup(service => service.RecordFailedMfaAttemptAsync(
            It.IsAny<UserMfaConfiguration>(), It.IsAny<MfaMethod>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns((string value) => "enc:" + value);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns((string value) =>
            value.StartsWith("enc:", StringComparison.Ordinal) ? value["enc:".Length..] : value);

        var service = new TotpMfaService(
            NullLogger<TotpMfaService>.Instance,
            repository,
            tracking.Object,
            encryption.Object,
            new SingleAcceptanceReplayStore(),
            mfaOptions: new MfaOptions(),
            timeProvider: null,
            enrollmentGuard: new MfaEnrollmentConcurrencyGuard());
        return (service, store);
    }

    // ── The issue's scenario: enrollment on one device, sign-in reading on another ────

    [Fact]
    public async Task ConcurrentEnrollmentAndSignIn_SignInNeverObservesTornMfaState()
    {
        const int rounds = 12;
        for (var round = 0; round < rounds; round++)
        {
            var (service, store) = CreateSut(TimeSpan.FromMilliseconds(8));
            var userId = Guid.NewGuid();
            var observedStates = new ConcurrentBag<(bool IsEnabled, bool IsSetupComplete)>();
            var stopObserving = new CancellationTokenSource();

            // "Device A": full settings enrollment — initiate, then complete with the code
            // the service itself just provisioned.
            var enrollment = Task.Run(async () =>
            {
                var (_, secret, _) = await service.SetupTotpAsync(userId, "racer@example.com");
                return await service.VerifyTotpAsync(userId, TotpCodeFor(secret));
            });

            // "Device B": the sign-in-side reader (PostgreSqlSignInMfaSubjectReader shape)
            // sampling the same row while enrollment is in flight.
            var signIn = Task.Run(async () =>
            {
                while (!stopObserving.IsCancellationRequested)
                {
                    var configuration = store.Get(userId);
                    if (configuration is { } row)
                    {
                        observedStates.Add((row.IsEnabled, row.IsSetupComplete));
                    }

                    await Task.Delay(1);
                }

                return observedStates;
            });

            var enrolled = await enrollment;
            stopObserving.Cancel();
            var observations = await signIn;

            // One side wins cleanly: the enrollment completes with both flags set…
            enrolled.Should().BeTrue("the enrollment task must complete on its own terms");
            var final = store.Get(userId);
            final!.IsEnabled.Should().BeTrue("the completed enrollment persists");
            final.IsSetupComplete.Should().BeTrue("a completed enrollment is never half-flagged");

            // …and every sign-in-side observation was a coherent old-or-new state, never a
            // torn mix (IsEnabled XOR IsSetupComplete is the torn shape).
            observations.Should().NotBeEmpty("the sign-in reader sampled the row during enrollment");
            observations.Should().OnlyContain(state => state.IsEnabled == state.IsSetupComplete,
                "a single-row read is atomic: sign-in sees MFA-off or MFA-on, never a mix");
        }
    }

    // ── The nastier interleaving: re-run setup racing the completing verify ────────────

    [Fact]
    public async Task ConcurrentReSetupAndVerify_NeverClobbersTheCompletedEnrollment()
    {
        const int rounds = 12;
        for (var round = 0; round < rounds; round++)
        {
            var (service, store) = CreateSut(TimeSpan.FromMilliseconds(12));
            var userId = Guid.NewGuid();

            // A pending setup exists (device A started enrollment earlier).
            var (_, initialSecret, _) = await service.SetupTotpAsync(userId, "racer@example.com");

            // Device A re-runs setup (fresh secret) at the same moment device B completes
            // the enrollment with a code. The widened write delay forces the two operations
            // to genuinely overlap; without the per-subject claim, the re-setup's stale
            // read (IsEnabled = false) would clobber the verify's enabling write.
            var reSetup = Task.Run(async () =>
            {
                try
                {
                    await service.SetupTotpAsync(userId, "racer@example.com");
                    return true;
                }
                catch (InvalidOperationException exception) when (exception.Message.Contains("already enabled", StringComparison.OrdinalIgnoreCase))
                {
                    // Verify won the claim: the re-setup correctly refuses to touch an
                    // enabled configuration — a clean loss, not corruption.
                    return false;
                }
            });
            var verify = Task.Run(() =>
            {
                // The code matches whichever secret is current when the verify reads it —
                // the initial secret, or the re-setup's replacement if that landed first.
                var current = store.Get(userId);
                var secret = PlainSecret(current!);
                return service.VerifyTotpAsync(userId, TotpCodeFor(secret));
            });

            var reSetupSucceeded = await reSetup;
            var verified = await verify;

            var final = store.Get(userId);
            final.Should().NotBeNull();

            if (verified)
            {
                // Verify won (or ran after the re-setup with the fresh secret): MFA is on.
                final!.IsEnabled.Should().BeTrue("a successful verify enables the factor");
                final.IsSetupComplete.Should().BeTrue();
            }
            else
            {
                // Re-setup won the write race: the verify consumed a code for the replaced
                // secret and lost cleanly — MFA stays off, uncorrupted, and can be retried.
                reSetupSucceeded.Should().BeTrue("a lost verify implies the re-setup claimed the window");
                final!.IsEnabled.Should().BeFalse("a lost verify must not leave a half-enabled factor");
                final.IsSetupComplete.Should().BeFalse();
                final.TotpSecretKey.Should().NotBeNullOrWhiteSpace("the surviving setup keeps a usable secret");
            }

            // Never torn, regardless of who won.
            final!.IsEnabled.Should().Be(final.IsSetupComplete,
                "IsEnabled and IsSetupComplete only ever change together");
        }
    }

    // ── Double enrollment: two devices completing enrollment, exactly one wins ────────

    [Fact]
    public async Task ConcurrentVerifications_ExactlyOneCompletionWins()
    {
        var (service, store) = CreateSut(TimeSpan.FromMilliseconds(5));
        var userId = Guid.NewGuid();
        var (_, secret, _) = await service.SetupTotpAsync(userId, "racer@example.com");
        var code = TotpCodeFor(secret);

        // Two devices present the SAME code concurrently: the replay store accepts the
        // (configuration, secret, step) exactly once, so only one verify can complete.
        var results = await Task.WhenAll(
            Task.Run(() => service.VerifyTotpAsync(userId, code)),
            Task.Run(() => service.VerifyTotpAsync(userId, code)));

        results.Count(success => success).Should().Be(1, "the single-use code may complete exactly one enrollment");

        var final = store.Get(userId);
        final!.IsEnabled.Should().BeTrue();
        final.IsSetupComplete.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentEnrollmentsOnDifferentUsers_NeverBlockEachOther()
    {
        // The claim is per subject: unrelated users' enrollments must still run in parallel.
        var guard = new MfaEnrollmentConcurrencyGuard();
        var store = new AtomicMfaConfigurationStore(TimeSpan.Zero);
        var completionOrder = new ConcurrentBag<Guid>();

        var tasks = Enumerable.Range(0, 6).Select(_ => Task.Run(async () =>
        {
            var userId = Guid.NewGuid();
            await guard.ExecuteEnrollmentOperationAsync(userId, async () =>
            {
                await Task.Delay(25);
                store.Create(new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = userId, IsEnabled = true, IsSetupComplete = true });
                completionOrder.Add(userId);
            });
        })).ToArray();

        await Task.WhenAll(tasks);

        completionOrder.Should().HaveCount(6, "every subject's enrollment completed");
    }
}
