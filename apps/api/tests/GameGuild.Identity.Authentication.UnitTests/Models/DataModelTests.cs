using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Models;

public class DeviceInfoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var device = new DeviceInfo();

        device.Fingerprint.Should().BeEmpty();
        device.DeviceId.Should().BeEmpty();
        device.IpAddress.Should().BeNull();
        device.DeviceName.Should().BeNull();
        device.DeviceType.Should().BeNull();
        device.OperatingSystem.Should().BeNull();
        device.OsVersion.Should().BeNull();
        device.Browser.Should().BeNull();
        device.BrowserVersion.Should().BeNull();
        device.ScreenResolution.Should().BeNull();
        device.Timezone.Should().BeNull();
        device.Language.Should().BeNull();
        device.UserAgent.Should().BeNull();
        device.IsMobile.Should().BeFalse();
        device.IsBot.Should().BeFalse();
    }

    [Fact]
    public void DeviceId_ShouldReturnFingerprint()
    {
        var device = new DeviceInfo { Fingerprint = "fp-abc123" };

        device.DeviceId.Should().Be("fp-abc123");
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var device = new DeviceInfo
        {
            Fingerprint = "fp-123",
            IpAddress = "192.168.1.1",
            DeviceName = "John's iPhone",
            DeviceType = "Mobile",
            OperatingSystem = "iOS",
            OsVersion = "17.0",
            Browser = "Safari",
            BrowserVersion = "17.0",
            ScreenResolution = "1170x2532",
            Timezone = "America/New_York",
            Language = "en-US",
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)",
            IsMobile = true,
            IsBot = false
        };

        device.Fingerprint.Should().Be("fp-123");
        device.DeviceId.Should().Be("fp-123");
        device.IpAddress.Should().Be("192.168.1.1");
        device.DeviceName.Should().Be("John's iPhone");
        device.DeviceType.Should().Be("Mobile");
        device.OperatingSystem.Should().Be("iOS");
        device.OsVersion.Should().Be("17.0");
        device.Browser.Should().Be("Safari");
        device.BrowserVersion.Should().Be("17.0");
        device.ScreenResolution.Should().Be("1170x2532");
        device.Timezone.Should().Be("America/New_York");
        device.Language.Should().Be("en-US");
        device.UserAgent.Should().Contain("iPhone");
        device.IsMobile.Should().BeTrue();
        device.IsBot.Should().BeFalse();
    }
}

public class AuthenticationModuleOptionsTests
{
    [Fact]
    public void SectionName_ShouldBeAuthentication()
    {
        AuthenticationModuleOptions.SectionName.Should().Be("Authentication");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var options = new AuthenticationModuleOptions();

        options.EnablePermissionCaching.Should().BeTrue();
        options.PermissionCacheExpirationMinutes.Should().Be(30);
        options.EnableAbacPolicies.Should().BeTrue();
        options.EnableConditionalPolicies.Should().BeTrue();
        options.EnableAccessReviews.Should().BeTrue();
        options.MaxPoliciesPerEvaluation.Should().Be(100);
        options.EnableDetailedAuditLogging.Should().BeTrue();
        options.EnablePerformanceMetrics.Should().BeTrue();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var options = new AuthenticationModuleOptions
        {
            EnablePermissionCaching = false,
            PermissionCacheExpirationMinutes = 60,
            EnableAbacPolicies = false,
            EnableConditionalPolicies = false,
            EnableAccessReviews = false,
            MaxPoliciesPerEvaluation = 50,
            EnableDetailedAuditLogging = false,
            EnablePerformanceMetrics = false
        };

        options.EnablePermissionCaching.Should().BeFalse();
        options.PermissionCacheExpirationMinutes.Should().Be(60);
        options.EnableAbacPolicies.Should().BeFalse();
        options.EnableConditionalPolicies.Should().BeFalse();
        options.EnableAccessReviews.Should().BeFalse();
        options.MaxPoliciesPerEvaluation.Should().Be(50);
        options.EnableDetailedAuditLogging.Should().BeFalse();
        options.EnablePerformanceMetrics.Should().BeFalse();
    }
}

public class OAuthUserProfileTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var profile = new OAuthUserProfile();

        profile.ProviderId.Should().BeEmpty();
        profile.Provider.Should().BeEmpty();
        profile.Email.Should().BeNull();
        profile.EmailVerified.Should().BeFalse();
        profile.Name.Should().BeNull();
        profile.FirstName.Should().BeNull();
        profile.LastName.Should().BeNull();
        profile.Username.Should().BeNull();
        profile.AvatarUrl.Should().BeNull();
        profile.Locale.Should().BeNull();
        profile.AccessToken.Should().BeNull();
        profile.RefreshToken.Should().BeNull();
        profile.TokenExpiresAt.Should().BeNull();
        profile.AdditionalClaims.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var expires = DateTime.UtcNow.AddHours(1);
        var claims = new Dictionary<string, object> { { "role", "admin" } };

        var profile = new OAuthUserProfile
        {
            ProviderId = "google-123",
            Provider = "Google",
            Email = "user@example.com",
            EmailVerified = true,
            Name = "John Doe",
            FirstName = "John",
            LastName = "Doe",
            Username = "johndoe",
            AvatarUrl = "https://example.com/avatar.jpg",
            Locale = "en-US",
            AccessToken = "access-token-123",
            RefreshToken = "refresh-token-456",
            TokenExpiresAt = expires,
            AdditionalClaims = claims
        };

        profile.ProviderId.Should().Be("google-123");
        profile.Provider.Should().Be("Google");
        profile.Email.Should().Be("user@example.com");
        profile.EmailVerified.Should().BeTrue();
        profile.Name.Should().Be("John Doe");
        profile.FirstName.Should().Be("John");
        profile.LastName.Should().Be("Doe");
        profile.Username.Should().Be("johndoe");
        profile.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        profile.Locale.Should().Be("en-US");
        profile.AccessToken.Should().Be("access-token-123");
        profile.RefreshToken.Should().Be("refresh-token-456");
        profile.TokenExpiresAt.Should().Be(expires);
        profile.AdditionalClaims.Should().ContainKey("role");
    }
}

public class SiemEventTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var evt = new SiemEvent { EventType = "test", Description = "test event" };

        evt.EventId.Should().NotBe(Guid.Empty);
        evt.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        evt.EventType.Should().Be("test");
        evt.Severity.Should().Be(SiemSeverity.Info);
        evt.Source.Should().Be("GameGuild.Authentication");
        evt.UserId.Should().BeNull();
        evt.IpAddress.Should().BeNull();
        evt.UserAgent.Should().BeNull();
        evt.Description.Should().Be("test event");
        evt.Metadata.Should().BeNull();
        evt.RiskScore.Should().BeNull();
        evt.TenantId.Should().BeNull();
        evt.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var metadata = new Dictionary<string, object> { { "attempt", 3 } };

        var evt = new SiemEvent
        {
            EventType = "LOGIN_FAILED",
            Severity = SiemSeverity.High,
            Source = "AuthService",
            UserId = userId,
            IpAddress = "10.0.0.1",
            UserAgent = "CustomClient/1.0",
            Description = "Multiple failed login attempts",
            Metadata = metadata,
            RiskScore = 85,
            TenantId = tenantId,
            CorrelationId = correlationId
        };

        evt.EventType.Should().Be("LOGIN_FAILED");
        evt.Severity.Should().Be(SiemSeverity.High);
        evt.Source.Should().Be("AuthService");
        evt.UserId.Should().Be(userId);
        evt.IpAddress.Should().Be("10.0.0.1");
        evt.UserAgent.Should().Be("CustomClient/1.0");
        evt.Description.Should().Be("Multiple failed login attempts");
        evt.Metadata.Should().ContainKey("attempt");
        evt.RiskScore.Should().Be(85);
        evt.TenantId.Should().Be(tenantId);
        evt.CorrelationId.Should().Be(correlationId);
    }

    [Theory]
    [InlineData(SiemSeverity.Info, 0)]
    [InlineData(SiemSeverity.Low, 1)]
    [InlineData(SiemSeverity.Medium, 2)]
    [InlineData(SiemSeverity.High, 3)]
    [InlineData(SiemSeverity.Critical, 4)]
    public void SiemSeverity_ShouldHaveCorrectValues(SiemSeverity severity, int expected)
    {
        ((int)severity).Should().Be(expected);
    }
}

public class PasswordStrengthResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new PasswordStrengthResult();

        result.IsValid.Should().BeFalse();
        result.StrengthScore.Should().Be(0);
        result.StrengthLevel.Should().BeEmpty();
        result.ValidationFailures.Should().BeEmpty();
        result.Suggestions.Should().BeEmpty();
        result.IsCompromised.Should().BeFalse();
        result.EstimatedCrackTime.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var result = new PasswordStrengthResult
        {
            IsValid = true,
            StrengthScore = 85,
            StrengthLevel = "Strong",
            ValidationFailures = new List<string> { "Too short" },
            Suggestions = new List<string> { "Add special characters" },
            IsCompromised = false,
            EstimatedCrackTime = "centuries"
        };

        result.IsValid.Should().BeTrue();
        result.StrengthScore.Should().Be(85);
        result.StrengthLevel.Should().Be("Strong");
        result.ValidationFailures.Should().ContainSingle();
        result.Suggestions.Should().ContainSingle();
        result.IsCompromised.Should().BeFalse();
        result.EstimatedCrackTime.Should().Be("centuries");
    }
}

public class UserMfaConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new UserMfaConfiguration();

        config.IsEnabled.Should().BeFalse();
        config.TotpSecretKey.Should().BeNull();
        config.BackupCodes.Should().BeNull();
        config.EnabledAt.Should().BeNull();
        config.LastUsedAt.Should().BeNull();
        config.FailedAttempts.Should().Be(0);
        config.LockedOutUntil.Should().BeNull();
        config.PreferredMethod.Should().Be(MfaMethod.Totp);
        config.QrCodeSetupData.Should().BeNull();
        config.IsSetupComplete.Should().BeFalse();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var config = new UserMfaConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IsEnabled = true,
            TotpSecretKey = "encrypted-secret",
            BackupCodes = "[\"code1\",\"code2\"]",
            EnabledAt = now,
            LastUsedAt = now,
            FailedAttempts = 3,
            LockedOutUntil = now.AddMinutes(30),
            PreferredMethod = MfaMethod.Totp,
            QrCodeSetupData = "otpauth://...",
            IsSetupComplete = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        config.IsEnabled.Should().BeTrue();
        config.TotpSecretKey.Should().Be("encrypted-secret");
        config.BackupCodes.Should().Contain("code1");
        config.EnabledAt.Should().Be(now);
        config.FailedAttempts.Should().Be(3);
        config.LockedOutUntil.Should().NotBeNull();
        config.IsSetupComplete.Should().BeTrue();
    }
}

public class UserWebAuthnCredentialTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var cred = new UserWebAuthnCredential();

        cred.CredentialId.Should().BeEmpty();
        cred.PublicKey.Should().BeEmpty();
        cred.AaGuid.Should().BeNull();
        cred.SignatureCounter.Should().Be(0u);
        cred.FriendlyName.Should().BeNull();
        cred.CredentialType.Should().Be("public-key");
        cred.IsPasswordless.Should().BeFalse();
        cred.IsDefault.Should().BeFalse();
        cred.UserVerified.Should().BeFalse();
        cred.BackedUp.Should().BeFalse();
        cred.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        cred.LastUsedAt.Should().BeNull();
        cred.RegisteredFromIp.Should().BeNull();
        cred.RegisteredUserAgent.Should().BeNull();
        cred.IsActive.Should().BeTrue();
        cred.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var now = DateTime.UtcNow;
        var cred = new UserWebAuthnCredential
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CredentialId = "cred-abc123",
            PublicKey = "pubkey-base64",
            AaGuid = "00000000-0000-0000-0000-000000000000",
            SignatureCounter = 42,
            FriendlyName = "MacBook Touch ID",
            CredentialType = "public-key",
            AuthenticatorType = WebAuthnAuthenticatorType.Platform,
            Transports = "internal",
            IsPasswordless = true,
            IsDefault = true,
            UserVerified = true,
            BackedUp = true,
            CreatedAt = now,
            LastUsedAt = now,
            RegisteredFromIp = "192.168.1.1",
            RegisteredUserAgent = "Chrome/120",
            IsActive = true,
            RevokedAt = null
        };

        cred.CredentialId.Should().Be("cred-abc123");
        cred.FriendlyName.Should().Be("MacBook Touch ID");
        cred.AuthenticatorType.Should().Be(WebAuthnAuthenticatorType.Platform);
        cred.SignatureCounter.Should().Be(42u);
        cred.IsPasswordless.Should().BeTrue();
        cred.IsDefault.Should().BeTrue();
        cred.UserVerified.Should().BeTrue();
        cred.BackedUp.Should().BeTrue();
    }

    [Theory]
    [InlineData(WebAuthnAuthenticatorType.Platform, 1)]
    [InlineData(WebAuthnAuthenticatorType.CrossPlatform, 2)]
    public void WebAuthnAuthenticatorType_ShouldHaveCorrectValues(
        WebAuthnAuthenticatorType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}
