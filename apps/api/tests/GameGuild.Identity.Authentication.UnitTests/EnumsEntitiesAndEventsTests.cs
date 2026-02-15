using FluentAssertions;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests;

#region Enum Tests

public class AuthenticationFailureReasonTests
{
    [Theory]
    [InlineData(AuthenticationFailureReason.InvalidCredentials, 0)]
    [InlineData(AuthenticationFailureReason.UserNotFound, 1)]
    [InlineData(AuthenticationFailureReason.AccountLocked, 2)]
    [InlineData(AuthenticationFailureReason.AccountDisabled, 3)]
    [InlineData(AuthenticationFailureReason.EmailNotVerified, 4)]
    [InlineData(AuthenticationFailureReason.MfaRequired, 5)]
    [InlineData(AuthenticationFailureReason.InvalidMfaCode, 6)]
    [InlineData(AuthenticationFailureReason.TokenExpired, 7)]
    [InlineData(AuthenticationFailureReason.InvalidToken, 8)]
    [InlineData(AuthenticationFailureReason.TokenRevoked, 9)]
    [InlineData(AuthenticationFailureReason.InvalidSession, 10)]
    [InlineData(AuthenticationFailureReason.UnauthorizedTenant, 11)]
    [InlineData(AuthenticationFailureReason.OAuthProviderError, 12)]
    [InlineData(AuthenticationFailureReason.InvalidWeb3Signature, 13)]
    [InlineData(AuthenticationFailureReason.RateLimitExceeded, 14)]
    [InlineData(AuthenticationFailureReason.Throttled, 15)]
    [InlineData(AuthenticationFailureReason.SuspiciousActivity, 16)]
    [InlineData(AuthenticationFailureReason.AnomalousActivity, 17)]
    [InlineData(AuthenticationFailureReason.SystemError, 18)]
    [InlineData(AuthenticationFailureReason.Unknown, 19)]
    public void AuthenticationFailureReason_ShouldHaveExpectedValue(AuthenticationFailureReason reason, int expected)
    {
        ((int)reason).Should().Be(expected);
    }

    [Fact]
    public void AuthenticationFailureReason_ShouldHave20Values()
    {
        Enum.GetValues<AuthenticationFailureReason>().Should().HaveCount(20);
    }
}

public class AuthenticationStepTests
{
    [Theory]
    [InlineData(AuthenticationStep.PrimaryCredential, 0)]
    [InlineData(AuthenticationStep.MfaVerification, 1)]
    [InlineData(AuthenticationStep.DeviceTrust, 2)]
    [InlineData(AuthenticationStep.RiskChallenge, 3)]
    public void AuthenticationStep_ShouldHaveExpectedValue(AuthenticationStep step, int expected)
    {
        ((int)step).Should().Be(expected);
    }
}

public class CredentialTypeTests
{
    [Theory]
    [InlineData(CredentialType.Email, 0)]
    [InlineData(CredentialType.Username, 1)]
    [InlineData(CredentialType.Phone, 2)]
    [InlineData(CredentialType.WalletAddress, 3)]
    public void CredentialType_ShouldHaveExpectedValue(CredentialType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

public class MfaMethodTests
{
    [Theory]
    [InlineData(MfaMethod.Totp, 1)]
    [InlineData(MfaMethod.BackupCode, 2)]
    [InlineData(MfaMethod.Sms, 3)]
    [InlineData(MfaMethod.Email, 4)]
    [InlineData(MfaMethod.WebAuthn, 5)]
    public void MfaMethod_ShouldHaveExpectedValue(MfaMethod method, int expected)
    {
        ((int)method).Should().Be(expected);
    }
}

public class RiskLevelTests
{
    [Theory]
    [InlineData(RiskLevel.Low, 0)]
    [InlineData(RiskLevel.Medium, 1)]
    [InlineData(RiskLevel.High, 2)]
    [InlineData(RiskLevel.Critical, 3)]
    public void RiskLevel_ShouldHaveExpectedValue(RiskLevel level, int expected)
    {
        ((int)level).Should().Be(expected);
    }
}

public class SessionTerminationReasonTests
{
    [Theory]
    [InlineData(SessionTerminationReason.UserLogout, 0)]
    [InlineData(SessionTerminationReason.AdministrativeTermination, 1)]
    [InlineData(SessionTerminationReason.Expired, 2)]
    [InlineData(SessionTerminationReason.SecurityViolation, 3)]
    [InlineData(SessionTerminationReason.DeviceChanged, 4)]
    [InlineData(SessionTerminationReason.LocationChanged, 5)]
    [InlineData(SessionTerminationReason.MaxSessionsExceeded, 6)]
    public void SessionTerminationReason_ShouldHaveExpectedValue(SessionTerminationReason reason, int expected)
    {
        ((int)reason).Should().Be(expected);
    }
}

public class SocialProviderTests
{
    [Theory]
    [InlineData(SocialProvider.Google, 0)]
    [InlineData(SocialProvider.Facebook, 1)]
    [InlineData(SocialProvider.Microsoft, 2)]
    [InlineData(SocialProvider.GitHub, 3)]
    [InlineData(SocialProvider.Twitter, 4)]
    [InlineData(SocialProvider.LinkedIn, 5)]
    [InlineData(SocialProvider.Apple, 6)]
    public void SocialProvider_ShouldHaveExpectedValue(SocialProvider provider, int expected)
    {
        ((int)provider).Should().Be(expected);
    }
}

public class VerificationMethodTests
{
    [Theory]
    [InlineData(VerificationMethod.Email, 0)]
    [InlineData(VerificationMethod.Sms, 1)]
    [InlineData(VerificationMethod.VoiceCall, 2)]
    [InlineData(VerificationMethod.GovernmentId, 3)]
    [InlineData(VerificationMethod.Biometric, 4)]
    [InlineData(VerificationMethod.Web3Signature, 5)]
    [InlineData(VerificationMethod.ManualReview, 6)]
    [InlineData(VerificationMethod.ThirdPartyKyc, 7)]
    public void VerificationMethod_ShouldHaveExpectedValue(VerificationMethod method, int expected)
    {
        ((int)method).Should().Be(expected);
    }
}

#endregion

#region Entity Tests

public class BlockchainCertificateAnchorTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var anchor = new BlockchainCertificateAnchor();

        anchor.Id.Should().Be(Guid.Empty);
        anchor.UserId.Should().Be(Guid.Empty);
        anchor.CertificateType.Should().BeEmpty();
        anchor.CertificateHash.Should().BeEmpty();
        anchor.CertificateData.Should().BeEmpty();
        anchor.TransactionHash.Should().BeEmpty();
        anchor.BlockchainNetwork.Should().BeEmpty();
        anchor.BlockNumber.Should().BeNull();
        anchor.IsRevoked.Should().BeFalse();
        anchor.RevokedAt.Should().BeNull();
        anchor.RevocationReason.Should().BeNull();
        anchor.ExpiresAt.Should().BeNull();
        anchor.Metadata.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var anchor = new BlockchainCertificateAnchor
        {
            Id = id,
            UserId = userId,
            CertificateType = "EmailVerified",
            CertificateHash = "0xabc",
            CertificateData = "data",
            TransactionHash = "0xtx",
            BlockchainNetwork = "Ethereum",
            BlockNumber = 12345,
            AnchoredAt = now,
            IsRevoked = true,
            RevokedAt = now,
            RevocationReason = "compromised",
            ExpiresAt = now.AddDays(30),
            Metadata = "{}"
        };

        anchor.Id.Should().Be(id);
        anchor.UserId.Should().Be(userId);
        anchor.CertificateType.Should().Be("EmailVerified");
        anchor.CertificateHash.Should().Be("0xabc");
        anchor.CertificateData.Should().Be("data");
        anchor.TransactionHash.Should().Be("0xtx");
        anchor.BlockchainNetwork.Should().Be("Ethereum");
        anchor.BlockNumber.Should().Be(12345);
        anchor.AnchoredAt.Should().Be(now);
        anchor.IsRevoked.Should().BeTrue();
        anchor.RevokedAt.Should().Be(now);
        anchor.RevocationReason.Should().Be("compromised");
        anchor.ExpiresAt.Should().Be(now.AddDays(30));
        anchor.Metadata.Should().Be("{}");
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNotRevokedAndNotExpired()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        anchor.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNotRevokedAndNoExpiry()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = false,
            ExpiresAt = null
        };

        anchor.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenRevoked()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        anchor.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenExpired()
    {
        var anchor = new BlockchainCertificateAnchor
        {
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        anchor.IsValid.Should().BeFalse();
    }
}

public class ContentTypePermissionTests
{
    [Fact]
    public void DefaultConstructor_ShouldCreateWithDefaults()
    {
        var perm = new ContentTypePermission();

        perm.ContentTypeName.Should().BeEmpty();
        perm.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var perm = new ContentTypePermission(userId, tenantId, "Property");

        perm.ContentTypeName.Should().Be("Property");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContentTypeNameIsNull()
    {
        var act = () => new ContentTypePermission(Guid.NewGuid(), Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsDefaultPermission_ShouldReturnTrue_WhenNoUserId()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Report");

        perm.IsDefaultPermission().Should().BeTrue();
    }

    [Fact]
    public void IsDefaultPermission_ShouldReturnFalse_WhenUserIdSet()
    {
        var perm = new ContentTypePermission(Guid.NewGuid(), Guid.NewGuid(), "Report");

        perm.IsDefaultPermission().Should().BeFalse();
    }

    [Fact]
    public void IsUserSpecificPermission_ShouldReturnTrue_WhenUserIdSet()
    {
        var perm = new ContentTypePermission(Guid.NewGuid(), Guid.NewGuid(), "Document");

        perm.IsUserSpecificPermission().Should().BeTrue();
    }

    [Fact]
    public void IsUserSpecificPermission_ShouldReturnFalse_WhenNoUserId()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Document");

        perm.IsUserSpecificPermission().Should().BeFalse();
    }

    [Fact]
    public void UpdateContentTypeName_ShouldUpdateName()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Old");

        perm.UpdateContentTypeName("New");

        perm.ContentTypeName.Should().Be("New");
    }

    [Fact]
    public void UpdateContentTypeName_ShouldThrow_WhenNullOrEmpty()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Test");

        var actNull = () => perm.UpdateContentTypeName(null!);
        var actEmpty = () => perm.UpdateContentTypeName("");
        var actWhitespace = () => perm.UpdateContentTypeName("   ");

        actNull.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
        actWhitespace.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDescription_ShouldUpdateDescription()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Test");

        perm.UpdateDescription("A new description");

        perm.Description.Should().Be("A new description");
    }

    [Fact]
    public void UpdateDescription_ShouldAllowNull()
    {
        var perm = new ContentTypePermission(null, Guid.NewGuid(), "Test");
        perm.UpdateDescription("something");

        perm.UpdateDescription(null);

        perm.Description.Should().BeNull();
    }
}

#endregion

#region Model Tests

public class Web3ChallengeTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var challenge = new Web3Challenge();

        challenge.Message.Should().BeEmpty();
        challenge.WalletAddress.Should().BeEmpty();
        challenge.Nonce.Should().BeEmpty();
        challenge.TenantId.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var challenge = new Web3Challenge
        {
            Message = "Sign this message",
            WalletAddress = "0x123",
            Nonce = "nonce123",
            IssuedAt = now,
            ExpiresAt = now.AddMinutes(5),
            TenantId = tenantId
        };

        challenge.Message.Should().Be("Sign this message");
        challenge.WalletAddress.Should().Be("0x123");
        challenge.Nonce.Should().Be("nonce123");
        challenge.IssuedAt.Should().Be(now);
        challenge.ExpiresAt.Should().Be(now.AddMinutes(5));
        challenge.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNotExpired()
    {
        var challenge = new Web3Challenge { ExpiresAt = DateTime.UtcNow.AddMinutes(5) };

        challenge.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenExpired()
    {
        var challenge = new Web3Challenge { ExpiresAt = DateTime.UtcNow.AddMinutes(-5) };

        challenge.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SecondsUntilExpiration_ShouldReturnPositive_WhenNotExpired()
    {
        var challenge = new Web3Challenge { ExpiresAt = DateTime.UtcNow.AddMinutes(5) };

        challenge.SecondsUntilExpiration.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SecondsUntilExpiration_ShouldReturnZero_WhenExpired()
    {
        var challenge = new Web3Challenge { ExpiresAt = DateTime.UtcNow.AddMinutes(-5) };

        challenge.SecondsUntilExpiration.Should().Be(0);
    }
}

#endregion

#region Constants Tests

public class JwtClaimTypesTests
{
    [Fact]
    public void TenantId_ShouldBeCorrect()
    {
        JwtClaimTypes.TenantId.Should().Be("tenant_id");
    }

    [Fact]
    public void TenantPermissionFlags1_ShouldBeCorrect()
    {
        JwtClaimTypes.TenantPermissionFlags1.Should().Be("tenant_permission_flags1");
    }

    [Fact]
    public void TenantPermissionFlags2_ShouldBeCorrect()
    {
        JwtClaimTypes.TenantPermissionFlags2.Should().Be("tenant_permission_flags2");
    }

    [Fact]
    public void Username_ShouldBeCorrect()
    {
        JwtClaimTypes.Username.Should().Be("username");
    }

    [Fact]
    public void DisplayName_ShouldBeCorrect()
    {
        JwtClaimTypes.DisplayName.Should().Be("display_name");
    }

    [Fact]
    public void AvatarUrl_ShouldBeCorrect()
    {
        JwtClaimTypes.AvatarUrl.Should().Be("avatar_url");
    }

    [Fact]
    public void MfaEnabled_ShouldBeCorrect()
    {
        JwtClaimTypes.MfaEnabled.Should().Be("mfa_enabled");
    }

    [Fact]
    public void SessionId_ShouldBeCorrect()
    {
        JwtClaimTypes.SessionId.Should().Be("session_id");
    }
}

#endregion

#region Event Tests

public class SealedEventTests
{
    [Fact]
    public void MfaVerifiedEvent_ShouldStoreProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var evt = new MfaVerifiedEvent(userId, "Totp", "1.2.3.4", now);

        evt.UserId.Should().Be(userId);
        evt.Method.Should().Be("Totp");
        evt.IpAddress.Should().Be("1.2.3.4");
        evt.Timestamp.Should().Be(now);
    }

    [Fact]
    public void TokenRefreshedEvent_ShouldStoreProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var evt = new TokenRefreshedEvent(userId, "5.6.7.8", now);

        evt.UserId.Should().Be(userId);
        evt.IpAddress.Should().Be("5.6.7.8");
        evt.Timestamp.Should().Be(now);
    }

    [Fact]
    public void TokenRevokedEvent_ShouldStoreProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var evt = new TokenRevokedEvent(userId, "token-123", null, now);

        evt.UserId.Should().Be(userId);
        evt.TokenId.Should().Be("token-123");
        evt.IpAddress.Should().BeNull();
        evt.Timestamp.Should().Be(now);
    }

    [Fact]
    public void UserSignedUpEvent_ShouldStoreProperties()
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var evt = new UserSignedUpEvent(userId, "user@test.com", "Email", now);

        evt.UserId.Should().Be(userId);
        evt.Email.Should().Be("user@test.com");
        evt.AuthMethod.Should().Be("Email");
        evt.Timestamp.Should().Be(now);
    }
}

#endregion
