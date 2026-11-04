using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class SignInMfaChallengeTests
{
    [Fact]
    public void OpaqueBearersAreDistinctCanonicalAndPersistedAsDigests()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => SignInMfaChallengeToken.Create()).ToArray();
        Assert.Equal(100, tokens.Distinct(StringComparer.Ordinal).Count());
        foreach (var token in tokens)
        {
            Assert.Equal(43, token.Length);
            Assert.True(SignInMfaChallengeToken.TryHash(token, out var hash));
            Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token))).ToLowerInvariant(), hash);
            Assert.NotEqual(token, hash);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("contains space")]
    public void MalformedBearersNeverBecomeStoreKeys(string? token)
    {
        Assert.False(SignInMfaChallengeToken.TryHash(token, out var hash));
        Assert.Empty(hash);
    }

    [Fact]
    public void AlternateBase64TailEncodingIsRejected()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var token = SignInMfaChallengeToken.Create();
        var alternate = token[..^1] + alphabet[alphabet.IndexOf(token[^1]) + 1];
        Assert.Equal(Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/') + "="),
            Convert.FromBase64String(alternate.Replace('-', '+').Replace('_', '/') + "="));
        Assert.False(SignInMfaChallengeToken.TryHash(alternate, out var hash));
        Assert.Empty(hash);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("subject")]
    [InlineData("tenant")]
    [InlineData("version")]
    [InlineData("bearer")]
    [InlineData("policy")]
    [InlineData("purpose")]
    [InlineData("first-factor")]
    [InlineData("created")]
    [InlineData("expired")]
    [InlineData("long-lifetime")]
    [InlineData("consumed")]
    [InlineData("revoked")]
    [InlineData("already-verified")]
    public void IncompleteOrAlreadyUsedFirstFactorStateCannotBeAdded(string mode)
    {
        var challenge = CreateChallenge();
        switch (mode)
        {
            case "id": challenge.Id = Guid.Empty; break;
            case "subject": challenge.SubjectId = Guid.Empty; break;
            case "tenant": challenge.TenantId = Guid.Empty; break;
            case "version": challenge.SubjectTokenVersion = 0; break;
            case "bearer": challenge.TokenHash = SignInMfaChallengeToken.Create(); break;
            case "policy": challenge.PolicyFingerprint = "invalid"; break;
            case "purpose": challenge.Purpose = (SignInMfaPurpose)0; break;
            case "first-factor": challenge.FirstFactor = (SignInFirstFactor)0; break;
            case "created": challenge.CreatedAt = default; break;
            case "expired": challenge.ExpiresAt = challenge.CreatedAt; break;
            case "long-lifetime": challenge.ExpiresAt = challenge.CreatedAt.AddMinutes(6); break;
            case "consumed": challenge.ConsumedAt = challenge.CreatedAt; break;
            case "revoked": challenge.RevokedAt = challenge.CreatedAt; break;
            case "already-verified": challenge.VerificationMethod = MfaMethod.BackupCode; break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
        Assert.Throws<ArgumentException>(challenge.ValidateForPersistence);
    }

    [Fact]
    public void EnrollmentAndVerificationAreDifferentBoundPurposes()
    {
        var challenge = CreateChallenge();
        challenge.ValidateForPersistence();
        challenge.Purpose = SignInMfaPurpose.EnrollFactor;
        // A bare enrollment challenge is unpersistable: it can never be completed (born bound).
        Assert.Throws<ArgumentException>(challenge.ValidateForPersistence);
        challenge.EnrollmentConfigurationId = Guid.NewGuid();
        challenge.EnrollmentSecretFingerprint = new string('a', 64);
        challenge.EnrollmentInitializedAt = challenge.CreatedAt;
        challenge.ValidateForPersistence();
        Assert.NotEqual(SignInMfaPurpose.VerifyFactor, challenge.Purpose);
    }

    [Theory]
    [InlineData("bare")]
    [InlineData("configuration-only")]
    [InlineData("fingerprint-only")]
    [InlineData("initialization-only")]
    [InlineData("configuration-and-fingerprint")]
    [InlineData("configuration-and-initialization")]
    [InlineData("fingerprint-and-initialization")]
    public void PartiallyBoundEnrollFactorChallengesCannotBePersisted(string mode)
    {
        var challenge = CreateChallenge();
        challenge.Purpose = SignInMfaPurpose.EnrollFactor;
        if (mode.Contains("configuration")) { challenge.EnrollmentConfigurationId = Guid.NewGuid(); }
        if (mode.Contains("fingerprint")) { challenge.EnrollmentSecretFingerprint = new string('a', 64); }
        if (mode.Contains("initialization")) { challenge.EnrollmentInitializedAt = challenge.CreatedAt; }
        Assert.Throws<ArgumentException>(challenge.ValidateForPersistence);
    }

    [Theory]
    [InlineData("configuration")]
    [InlineData("fingerprint")]
    [InlineData("initialization")]
    [InlineData("all")]
    public void VerifyFactorChallengesCannotCarryEnrollmentEvidence(string mode)
    {
        var challenge = CreateChallenge();
        challenge.Purpose = SignInMfaPurpose.VerifyFactor;
        if (mode is "configuration" or "all") { challenge.EnrollmentConfigurationId = Guid.NewGuid(); }
        if (mode is "fingerprint" or "all") { challenge.EnrollmentSecretFingerprint = new string('a', 64); }
        if (mode is "initialization" or "all") { challenge.EnrollmentInitializedAt = challenge.CreatedAt; }
        Assert.Throws<ArgumentException>(challenge.ValidateForPersistence);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("long")]
    [InlineData("non-hex")]
    [InlineData("uppercase")]
    public void EnrollmentFingerprintsMustBeFullLowercaseHexDigests(string mode)
    {
        var challenge = CreateChallenge();
        challenge.Purpose = SignInMfaPurpose.EnrollFactor;
        challenge.EnrollmentConfigurationId = Guid.NewGuid();
        challenge.EnrollmentSecretFingerprint = mode switch
        {
            "short" => new string('a', 63),
            "long" => new string('a', 65),
            "uppercase" => new string('A', 64),
            _ => new string('g', 64)
        };
        challenge.EnrollmentInitializedAt = challenge.CreatedAt;
        Assert.Throws<ArgumentException>(challenge.ValidateForPersistence);
    }

    [Theory]
    [InlineData("before-created")]
    [InlineData("at-expires")]
    [InlineData("after-expires")]
    public void EnrollmentInitializationMustFallInsideTheChallengeWindow(string mode)
    {
        var challenge = CreateChallenge();
        challenge.Purpose = SignInMfaPurpose.EnrollFactor;
        challenge.EnrollmentConfigurationId = Guid.NewGuid();
        challenge.EnrollmentSecretFingerprint = new string('a', 64);
        challenge.EnrollmentInitializedAt = mode switch
        {
            "before-created" => challenge.CreatedAt.AddSeconds(-1),
            "at-expires" => challenge.ExpiresAt,
            _ => challenge.ExpiresAt.AddSeconds(1)
        };
        Assert.Throws<ArgumentException>(challenge.ValidateForPersistence);
    }

    [Fact]
    public void FullyBoundEnrollFactorChallengeValidatesAcrossItsWholeWindow()
    {
        var challenge = CreateChallenge();
        challenge.Purpose = SignInMfaPurpose.EnrollFactor;
        challenge.EnrollmentConfigurationId = Guid.NewGuid();
        challenge.EnrollmentSecretFingerprint = new string('a', 64);
        challenge.EnrollmentInitializedAt = challenge.CreatedAt.AddMinutes(4);
        challenge.ValidateForPersistence();
        Assert.NotEqual(Guid.Empty, challenge.EnrollmentConfigurationId!.Value);
        Assert.True(SignInMfaChallengeToken.IsDigest(challenge.EnrollmentSecretFingerprint!));
        Assert.InRange(challenge.EnrollmentInitializedAt!.Value, challenge.CreatedAt, challenge.ExpiresAt);
    }

    public static SignInMfaChallenge CreateChallenge()
    {
        Assert.True(SignInMfaChallengeToken.TryHash(SignInMfaChallengeToken.Create(), out var tokenHash));
        Assert.True(SignInMfaChallengeToken.TryHash(SignInMfaChallengeToken.Create(), out var policyHash));
        var now = DateTimeOffset.UtcNow;
        return new SignInMfaChallenge
        {
            Id = Guid.NewGuid(), SubjectId = Guid.NewGuid(), TenantId = Guid.NewGuid(), SubjectTokenVersion = 3,
            TokenHash = tokenHash, PolicyFingerprint = policyHash, Purpose = SignInMfaPurpose.VerifyFactor,
            FirstFactor = SignInFirstFactor.Password, CreatedAt = now, ExpiresAt = now.AddMinutes(5)
        };
    }
}
