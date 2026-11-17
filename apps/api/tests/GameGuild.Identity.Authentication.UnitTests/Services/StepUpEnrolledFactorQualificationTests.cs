using System.Buffers.Binary;
using System.Security.Cryptography;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class StepUpEnrolledFactorQualificationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task StrictTotpCannotCompleteOrReactivateEnrollment(bool enabled, bool complete)
    {
        var f = new Fixture(enabled, complete);
        var secret = Fixture.CreateSyntheticKey();
        f.Configuration.TotpSecretKey = f.Encryption.Encrypt(secret);
        var accepted = await f.Totp.VerifyTotpForStepUpAsync(f.Actor, Fixture.Code(secret, f.Clock.GetUtcNow()));
        Assert.False(accepted);
        Assert.Equal(enabled, f.Configuration.IsEnabled);
        Assert.Equal(complete, f.Configuration.IsSetupComplete);
        Assert.Equal(0, f.Updates);
    }

    [Fact]
    public async Task StrictTotpAcceptsAnAlreadyConfirmedFactor()
    {
        var f = new Fixture(true, true);
        var secret = Fixture.CreateSyntheticKey();
        f.Configuration.TotpSecretKey = f.Encryption.Encrypt(secret);
        Assert.True(await f.Totp.VerifyTotpForStepUpAsync(f.Actor, Fixture.Code(secret, f.Clock.GetUtcNow())));
        Assert.Equal(1, f.Updates);
    }

    [Fact]
    public async Task LegacyEnrollmentConfirmationStillCompletesPendingSetup()
    {
        var f = new Fixture(false, false);
        var secret = Fixture.CreateSyntheticKey();
        f.Configuration.TotpSecretKey = f.Encryption.Encrypt(secret);
        Assert.True(await f.Totp.VerifyTotpAsync(f.Actor, Fixture.Code(secret, f.Clock.GetUtcNow())));
        Assert.True(f.Configuration.IsEnabled);
        Assert.True(f.Configuration.IsSetupComplete);
        Assert.Equal(1, f.Updates);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task StrictBackupCannotConsumeAnUnconfirmedOrDisabledFactor(bool enabled, bool complete)
    {
        var f = new Fixture(enabled, complete);
        const string code = "SYNTHETIC-ENROLLMENT-CONTROL";
        await f.Backup.StoreBackupCodesForSetupAsync(f.Actor, [code]);
        var original = f.Configuration.BackupCodes;
        f.Updates = 0;
        Assert.False(await f.Backup.VerifyBackupCodeForStepUpAsync(f.Actor, code));
        Assert.Equal(original, f.Configuration.BackupCodes);
        Assert.Equal(0, f.Updates);
    }

    [Fact]
    public async Task StrictBackupAcceptsAndConsumesAnAlreadyConfirmedFactor()
    {
        var f = new Fixture(true, true);
        const string code = "SYNTHETIC-ENROLLED-CONTROL";
        await f.Backup.StoreBackupCodesForSetupAsync(f.Actor, [code]);
        var original = f.Configuration.BackupCodes;
        f.Updates = 0;
        Assert.True(await f.Backup.VerifyBackupCodeForStepUpAsync(f.Actor, code));
        Assert.NotEqual(original, f.Configuration.BackupCodes);
        Assert.Equal(1, f.Updates);
    }

    [Fact]
    public async Task ExistingLegacyBackupResultContractIsPreserved()
    {
        var f = new Fixture(true, false);
        const string code = "SYNTHETIC-LEGACY-CONTROL";
        await f.Backup.StoreBackupCodesForSetupAsync(f.Actor, [code]);
        Assert.True(await f.Backup.VerifyBackupCodeAsync(f.Actor, code));
    }

    private sealed class Fixture
    {
        public Guid Actor { get; } = Guid.NewGuid();
        public FixedClock Clock { get; } = new();
        public UserMfaConfiguration Configuration { get; }
        public EncryptionService Encryption { get; }
        public TotpMfaService Totp { get; }
        public BackupCodeMfaService Backup { get; }
        public int Updates { get; set; }

        public Fixture(bool enabled, bool complete)
        {
            Configuration = new UserMfaConfiguration
            {
                Id = Guid.NewGuid(), UserId = Actor, IsEnabled = enabled, IsSetupComplete = complete,
                UpdatedAt = Clock.GetUtcNow().UtcDateTime,
                SetupExpiresAt = Clock.GetUtcNow().UtcDateTime.AddMinutes(5)
            };
            var repository = new Mock<IUserMfaConfigurationRepository>();
            repository.Setup(x => x.GetByUserIdAsync(Actor, It.IsAny<CancellationToken>())).ReturnsAsync(Configuration);
            repository.Setup(x => x.UpdateAsync(It.IsAny<UserMfaConfiguration>(), It.IsAny<CancellationToken>()))
                .Callback(() => Updates++)
                .ReturnsAsync((UserMfaConfiguration row, CancellationToken _) => row);
            var tracking = new Mock<IMfaAttemptTrackingService>();
            var options = new MfaOptions { Enabled = true, TotpClockSkew = 0 };
            Encryption = new EncryptionService(NullLogger<EncryptionService>.Instance,
                new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Encryption:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                }).Build());
            Totp = new TotpMfaService(NullLogger<TotpMfaService>.Instance, repository.Object,
                tracking.Object, Encryption, NativeTotpReplayStub.Create(), options, Clock);
            Backup = new BackupCodeMfaService(NullLogger<BackupCodeMfaService>.Instance,
                repository.Object, tracking.Object, options);
        }

        public static string CreateSyntheticKey()
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            return new string(Enumerable.Range(0, 32).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
        }

        public static string Code(string base32, DateTimeOffset now)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var key = new List<byte>();
            var buffer = 0;
            var bits = 0;
            foreach (var character in base32)
            {
                buffer = (buffer << 5) | alphabet.IndexOf(character);
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    key.Add((byte)(buffer >> bits));
                }
            }
            var counter = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counter, now.ToUnixTimeSeconds() / 30);
            var digest = HMACSHA1.HashData(key.ToArray(), counter);
            var start = digest[^1] & 15;
            var value = BinaryPrimitives.ReadInt32BigEndian(digest.AsSpan(start, 4)) & int.MaxValue;
            return (value % 1000000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 7, 8, 0, 0, TimeSpan.Zero);
    }
}
