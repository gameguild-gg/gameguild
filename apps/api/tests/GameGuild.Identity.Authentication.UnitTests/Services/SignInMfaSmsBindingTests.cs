using System.Security.Cryptography;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class SignInMfaSmsBindingTests
{
    [Fact]
    public void CommandDiagnosticsDoNotExposeTheFirstFactorContinuation()
    {
        var bearer = SignInMfaChallengeToken.Create();
        Assert.DoesNotContain(bearer, new SendSmsMfaChallengeCommand(bearer).ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-continuation")]
    public async Task MissingOrMalformedContinuationCannotReachDelivery(string bearer)
    {
        var fixture = new Fixture();
        Assert.False((await fixture.Service.SendSmsChallengeAsync(bearer, CancellationToken.None)).Success);
        fixture.Store.VerifyNoOtherCalls();
        fixture.Mfa.VerifyNoOtherCalls();
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingActiveContinuationCannotReachSubjectOrDelivery()
    {
        var fixture = new Fixture();
        fixture.Store.Setup(port => port.FindActiveAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SignInMfaChallenge?)null);
        Assert.False((await fixture.Service.SendSmsChallengeAsync(fixture.Bearer, CancellationToken.None)).Success);
        fixture.Subjects.VerifyNoOtherCalls();
        fixture.Mfa.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("enrollment-purpose")]
    [InlineData("version")]
    [InlineData("policy")]
    [InlineData("locked")]
    [InlineData("unenrolled")]
    [InlineData("no-sms")]
    [InlineData("missing-user")]
    [InlineData("inactive")]
    public async Task StaleOrIneligibleContinuationReturnsGenericFailureWithoutDelivery(string fault)
    {
        var fixture = new Fixture();
        switch (fault)
        {
            case "enrollment-purpose": fixture.Challenge.Purpose = SignInMfaPurpose.EnrollFactor; break;
            case "version": fixture.User.TokenVersion++; break;
            case "policy": fixture.Decision = fixture.Decision with { PolicyFingerprint = new string('b', 64) }; break;
            case "locked": fixture.LockedUntil = DateTime.UtcNow.AddMinutes(5); break;
            case "unenrolled": fixture.Enrolled = false; break;
            case "no-sms": fixture.Methods = [MfaMethod.Totp]; break;
            case "missing-user": fixture.MissingUser = true; break;
            case "inactive": fixture.User.IsActive = false; break;
            default: throw new ArgumentOutOfRangeException(nameof(fault));
        }
        var result = await fixture.Service.SendSmsChallengeAsync(fixture.Bearer, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal("Invalid MFA challenge", result.Message);
        Assert.Empty(result.PhoneNumberMasked);
        fixture.Mfa.VerifyNoOtherCalls();
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidFirstFactorResolvesSubjectAndContinuationBeforeDelivery()
    {
        var fixture = new Fixture();
        fixture.Mfa.Setup(port => port.SendSmsSignInChallengeAsync(fixture.User.Id, fixture.Challenge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsMfaChallengeResult { Success = true, PhoneNumberMasked = "***0123", ExpiresInSeconds = 600 });
        Assert.True((await fixture.Service.SendSmsChallengeAsync(fixture.Bearer, CancellationToken.None)).Success);
        fixture.Mfa.Verify(port => port.SendSmsSignInChallengeAsync(fixture.User.Id, fixture.Challenge.Id, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Store.Verify(port => port.TryConsumeAsync(It.IsAny<string>(), It.IsAny<SignInMfaChallengeBinding>(), It.IsAny<MfaMethod>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SmsCompletionUsesBoundProviderAndAtomicallyConsumedBinding()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.CompleteCodeAsync(fixture.Bearer, "123456", MfaMethod.Sms, fixture.Device, CancellationToken.None);
        Assert.True(result.Success);
        fixture.Mfa.Verify(port => port.VerifySmsSignInCodeAsync(fixture.User.Id, fixture.Challenge.Id, "123456", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Mfa.Verify(port => port.VerifyMfaAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<MfaMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Store.Verify(port => port.TryConsumeAsync(It.IsAny<string>(), It.Is<SignInMfaChallengeBinding>(binding =>
            binding.SubjectId == fixture.User.Id && binding.TenantId == fixture.TenantId && binding.SubjectTokenVersion == 7 &&
            binding.PolicyFingerprint == fixture.Challenge.PolicyFingerprint && binding.Purpose == SignInMfaPurpose.VerifyFactor),
            MfaMethod.Sms, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(fixture.IssuedProof);
        Assert.Equal(MfaMethod.Sms, fixture.IssuedProof.Method);
    }

    [Fact]
    public async Task LostAtomicConsumptionCannotIssueCredentials()
    {
        var fixture = new Fixture { ConsumeResult = false };
        var result = await fixture.Service.CompleteCodeAsync(fixture.Bearer, "123456", MfaMethod.Sms, fixture.Device, CancellationToken.None);
        Assert.False(result.Success);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangedVersionDuringProviderVerificationCannotConsumeOrIssue()
    {
        var fixture = new Fixture();
        fixture.Mfa.Setup(port => port.VerifySmsSignInCodeAsync(fixture.User.Id, fixture.Challenge.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                fixture.User.TokenVersion++;
                return new MfaVerificationResult { Success = true };
            });
        Assert.False((await fixture.Service.CompleteCodeAsync(fixture.Bearer, "123456", MfaMethod.Sms, fixture.Device, CancellationToken.None)).Success);
        fixture.Store.Verify(port => port.TryConsumeAsync(It.IsAny<string>(), It.IsAny<SignInMfaChallengeBinding>(), It.IsAny<MfaMethod>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Issuer.VerifyNoOtherCalls();
    }

    private sealed class Fixture
    {
        public User User { get; } = new() { Id = Guid.NewGuid(), TokenVersion = 7, Email = "sms-binding@example.test", Version = 1 };
        public Guid TenantId { get; } = Guid.NewGuid();
        public string Bearer { get; } = SignInMfaChallengeToken.Create();
        public DeviceInfo Device { get; } = new() { Fingerprint = "sms-binding", IpAddress = "127.0.0.1", UserAgent = "unit-test" };
        public SignInMfaChallenge Challenge { get; }
        public MfaRequirementDecision Decision { get; set; }
        public bool Enrolled { get; set; } = true;
        public bool MissingUser { get; set; }
        public DateTime? LockedUntil { get; set; }
        public IReadOnlyList<MfaMethod> Methods { get; set; } = [MfaMethod.Sms];
        public bool ConsumeResult { get; set; } = true;
        public SignInMfaProof? IssuedProof { get; private set; }
        public Mock<ISignInMfaSubjectReader> Subjects { get; } = new(MockBehavior.Strict);
        public Mock<IMfaSubjectRequirementPolicy> Policy { get; } = new(MockBehavior.Strict);
        public Mock<ISignInMfaChallengeStore> Store { get; } = new(MockBehavior.Strict);
        public Mock<IMfaService> Mfa { get; } = new(MockBehavior.Strict);
        public Mock<IAuthenticatedSessionIssuer> Issuer { get; } = new(MockBehavior.Strict);
        public SignInMfaService Service { get; }

        public Fixture()
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(32))).ToLowerInvariant();
            Decision = new MfaRequirementDecision(User.Id, TenantId, false, ["Member"], fingerprint);
            Assert.True(SignInMfaChallengeToken.TryHash(Bearer, out var hash));
            Challenge = new SignInMfaChallenge { Id = Guid.NewGuid(), SubjectId = User.Id, SubjectTokenVersion = 7,
                TenantId = TenantId, PolicyFingerprint = fingerprint, TokenHash = hash, Purpose = SignInMfaPurpose.VerifyFactor,
                FirstFactor = SignInFirstFactor.Password, CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) };
            Subjects.Setup(port => port.ReadCurrentAsync(User.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => MissingUser ? null : new SignInMfaSubjectState(User, Enrolled, LockedUntil, Methods));
            Policy.Setup(port => port.EvaluateAsync(User.Id, TenantId, It.IsAny<CancellationToken>())).ReturnsAsync(() => Decision);
            Store.Setup(port => port.FindActiveAsync(hash, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(Challenge);
            Store.Setup(port => port.TryConsumeAsync(hash, It.IsAny<SignInMfaChallengeBinding>(), MfaMethod.Sms, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => ConsumeResult);
            Mfa.Setup(port => port.VerifySmsSignInCodeAsync(User.Id, Challenge.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MfaVerificationResult { Success = true });
            Issuer.Setup(port => port.IssueMfaAsync(User, TenantId, Device, It.IsAny<SignInMfaProof>(), It.IsAny<CancellationToken>()))
                .Callback<User, Guid?, DeviceInfo, SignInMfaProof, CancellationToken>((_, _, _, proof, _) => IssuedProof = proof)
                .ReturnsAsync(new SignInResponse { Success = true, UserId = User.Id, TenantId = TenantId, SessionId = Guid.NewGuid(),
                    AccessToken = SignInMfaChallengeToken.Create(), RefreshToken = SignInMfaChallengeToken.Create() });
            var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
            audit.Setup(port => port.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var recovery = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
            recovery.Setup(port => port.CancelLiveByUserAsync(User.Id.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync((MfaRecoveryRequest?)null);
            Service = new SignInMfaService(Subjects.Object, Policy.Object, Store.Object, Mfa.Object, Issuer.Object,
                audit.Object, Mock.Of<ISessionMfaEvidenceStore>(), Mock.Of<ISignInMfaEnrollmentPort>(), recovery.Object);
        }
    }
}
