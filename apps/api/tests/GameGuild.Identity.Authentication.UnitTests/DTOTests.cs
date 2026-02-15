using FluentAssertions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests;

#region ApplyPermissionTemplateResult Tests

public class ApplyPermissionTemplateResultTests
{
    [Fact]
    public void SuccessResult_ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var permissions = new List<string> { "read", "write", "delete" };

        var result = ApplyPermissionTemplateResult.SuccessResult(
            userId, tenantId, templateId, "AdminTemplate", permissions, "admin@test.com");

        result.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
        result.TemplateId.Should().Be(templateId);
        result.TemplateName.Should().Be("AdminTemplate");
        result.PermissionsGranted.Should().Be(3);
        result.GrantedPermissions.Should().BeEquivalentTo(permissions);
        result.Success.Should().BeTrue();
        result.AppliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.AppliedBy.Should().Be("admin@test.com");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SuccessResult_WithNullTenantAndApplier_ShouldWork()
    {
        var result = ApplyPermissionTemplateResult.SuccessResult(
            Guid.NewGuid(), null, Guid.NewGuid(), "Template", new List<string>(), null);

        result.TenantId.Should().BeNull();
        result.AppliedBy.Should().BeNull();
        result.PermissionsGranted.Should().Be(0);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Failure_ShouldSetErrorProperties()
    {
        var userId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        var result = ApplyPermissionTemplateResult.Failure(userId, templateId, "Permission denied");

        result.UserId.Should().Be(userId);
        result.TemplateId.Should().Be(templateId);
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Permission denied");
        result.AppliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new ApplyPermissionTemplateResult();

        result.UserId.Should().Be(Guid.Empty);
        result.TenantId.Should().BeNull();
        result.TemplateId.Should().Be(Guid.Empty);
        result.TemplateName.Should().BeEmpty();
        result.PermissionsGranted.Should().Be(0);
        result.GrantedPermissions.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeFalse();
        result.AppliedBy.Should().BeNull();
    }
}

#endregion

#region MfaSetupResponse Tests

public class MfaSetupResponseTests
{
    [Fact]
    public void Success_ShouldSetProperties()
    {
        var result = MfaSetupResponse.Success("secret123", "otpauth://totp/test");

        result.IsSuccess.Should().BeTrue();
        result.SecretKey.Should().Be("secret123");
        result.QrCodeData.Should().Be("otpauth://totp/test");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldSetErrorMessage()
    {
        var result = MfaSetupResponse.Failure("MFA already enabled");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("MFA already enabled");
        result.SecretKey.Should().BeNull();
        result.QrCodeData.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var response = new MfaSetupResponse();

        response.IsSuccess.Should().BeFalse();
        response.ErrorMessage.Should().BeNull();
        response.SecretKey.Should().BeNull();
        response.QrCodeData.Should().BeNull();
        response.QrCodeUri.Should().BeNull();
        response.BackupCodes.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var response = new MfaSetupResponse
        {
            IsSuccess = true,
            SecretKey = "key",
            QrCodeData = "data",
            QrCodeUri = "uri",
            BackupCodes = new[] { "code1", "code2" }
        };

        response.QrCodeUri.Should().Be("uri");
        response.BackupCodes.Should().HaveCount(2);
    }
}

#endregion

#region MfaSetupResult Tests

public class MfaSetupResultTests
{
    [Fact]
    public void QrCodeUri_ShouldAliasQrCodeUrl()
    {
        var result = new MfaSetupResult { QrCodeUrl = "https://example.com/qr" };

        result.QrCodeUri.Should().Be("https://example.com/qr");
    }

    [Fact]
    public void QrCodeUri_Set_ShouldUpdateQrCodeUrl()
    {
        var result = new MfaSetupResult();
        result.QrCodeUri = "https://example.com/qr2";

        result.QrCodeUrl.Should().Be("https://example.com/qr2");
    }

    [Fact]
    public void SecretKey_ShouldAliasSecret()
    {
        var result = new MfaSetupResult { Secret = "mysecret" };

        result.SecretKey.Should().Be("mysecret");
    }

    [Fact]
    public void SecretKey_Set_ShouldUpdateSecret()
    {
        var result = new MfaSetupResult();
        result.SecretKey = "newsecret";

        result.Secret.Should().Be("newsecret");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new MfaSetupResult();

        result.Success.Should().BeFalse();
        result.Message.Should().BeEmpty();
        result.QrCodeUrl.Should().BeNull();
        result.Secret.Should().BeNull();
        result.BackupCodes.Should().BeNull();
    }
}

#endregion

#region BehavioralAnalysisResult Tests

public class BehavioralAnalysisResultTests
{
    [Fact]
    public void MatchesTypicalBehavior_ShouldAliasMatchesTypicalPattern()
    {
        var result = new BehavioralAnalysisResult { MatchesTypicalPattern = true };
        result.MatchesTypicalBehavior.Should().BeTrue();

        result.MatchesTypicalBehavior = false;
        result.MatchesTypicalPattern.Should().BeFalse();
    }

    [Fact]
    public void DetectedAnomalies_ShouldAliasDeviations()
    {
        var anomalies = new List<string> { "unusual_location", "new_device" };
        var result = new BehavioralAnalysisResult { Deviations = anomalies };

        result.DetectedAnomalies.Should().BeEquivalentTo(anomalies);
    }

    [Fact]
    public void DetectedAnomalies_Set_ShouldUpdateDeviations()
    {
        var result = new BehavioralAnalysisResult();
        result.DetectedAnomalies = new List<string> { "test" };

        result.Deviations.Should().ContainSingle().Which.Should().Be("test");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new BehavioralAnalysisResult();

        result.MatchesTypicalPattern.Should().BeFalse();
        result.RiskScore.Should().Be(0);
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.Confidence.Should().Be(0.5);
        result.Deviations.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var result = new BehavioralAnalysisResult
        {
            MatchesTypicalPattern = true,
            RiskScore = 75,
            RiskLevel = RiskLevel.High,
            Confidence = 0.95,
            Deviations = new List<string> { "deviation1" }
        };

        result.RiskScore.Should().Be(75);
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.Confidence.Should().Be(0.95);
    }
}

#endregion

#region AuthenticationAnomalyResult Tests

public class AuthenticationAnomalyResultTests
{
    [Fact]
    public void IsAnomalous_ShouldAliasIsSuspicious()
    {
        var result = new AuthenticationAnomalyResult { IsSuspicious = true };
        result.IsAnomalous.Should().BeTrue();

        result.IsAnomalous = false;
        result.IsSuspicious.Should().BeFalse();
    }

    [Fact]
    public void RiskFactors_ShouldAliasDetectedAnomalies()
    {
        var factors = new List<string> { "impossible_travel", "tor_exit_node" };
        var result = new AuthenticationAnomalyResult { DetectedAnomalies = factors };

        result.RiskFactors.Should().BeEquivalentTo(factors);
    }

    [Fact]
    public void RiskFactors_Set_ShouldUpdateDetectedAnomalies()
    {
        var result = new AuthenticationAnomalyResult();
        result.RiskFactors = new List<string> { "test_factor" };

        result.DetectedAnomalies.Should().ContainSingle().Which.Should().Be("test_factor");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new AuthenticationAnomalyResult();

        result.IsSuspicious.Should().BeFalse();
        result.RiskScore.Should().Be(0);
        result.RiskLevel.Should().Be(RiskLevel.Low);
        result.DetectedAnomalies.Should().BeEmpty();
    }
}

#endregion

#region SuspiciousActivity Tests

public class SuspiciousActivityTests
{
    [Fact]
    public void OccurredAt_ShouldAliasDetectedAt()
    {
        var now = DateTime.UtcNow;
        var activity = new SuspiciousActivity { DetectedAt = now };

        activity.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void OccurredAt_Set_ShouldUpdateDetectedAt()
    {
        var now = DateTime.UtcNow;
        var activity = new SuspiciousActivity();
        activity.OccurredAt = now;

        activity.DetectedAt.Should().Be(now);
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var activity = new SuspiciousActivity();

        activity.Id.Should().Be(Guid.Empty);
        activity.UserId.Should().BeNull();
        activity.Identifier.Should().BeNull();
        activity.ActivityType.Should().BeEmpty();
        activity.Description.Should().BeEmpty();
        activity.IpAddress.Should().BeEmpty();
        activity.UserAgent.Should().BeNull();
        activity.RiskScore.Should().Be(0);
        activity.RiskLevel.Should().Be(RiskLevel.Low);
        activity.IsConfirmedMalicious.Should().BeNull();
        activity.ActionsTaken.Should().BeEmpty();
        activity.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var activity = new SuspiciousActivity
        {
            Id = id,
            UserId = userId,
            Identifier = "user@test.com",
            ActivityType = "brute_force",
            Description = "Multiple failed logins",
            IpAddress = "1.2.3.4",
            UserAgent = "Mozilla/5.0",
            RiskScore = 90,
            RiskLevel = RiskLevel.Critical,
            DetectedAt = now,
            IsConfirmedMalicious = true,
            ActionsTaken = new List<string> { "account_locked" },
            Metadata = new Dictionary<string, string> { { "attempts", "50" } }
        };

        activity.Id.Should().Be(id);
        activity.UserId.Should().Be(userId);
        activity.ActivityType.Should().Be("brute_force");
        activity.IsConfirmedMalicious.Should().BeTrue();
        activity.ActionsTaken.Should().HaveCount(1);
        activity.Metadata.Should().ContainKey("attempts");
    }
}

#endregion

#region SessionSecurityAnalysis Tests

public class SessionSecurityAnalysisTests
{
    [Fact]
    public void UnusualActivityDetected_ShouldAliasIsSuspicious()
    {
        var analysis = new SessionSecurityAnalysis { IsSuspicious = true };
        analysis.UnusualActivityDetected.Should().BeTrue();

        analysis.UnusualActivityDetected = false;
        analysis.IsSuspicious.Should().BeFalse();
    }

    [Fact]
    public void RiskFactors_ShouldAliasSecurityFlags()
    {
        var flags = new List<string> { "new_ip", "unusual_time" };
        var analysis = new SessionSecurityAnalysis { SecurityFlags = flags };

        analysis.RiskFactors.Should().BeEquivalentTo(flags);
    }

    [Fact]
    public void RiskFactors_Set_ShouldUpdateSecurityFlags()
    {
        var analysis = new SessionSecurityAnalysis();
        analysis.RiskFactors = new List<string> { "flag1" };

        analysis.SecurityFlags.Should().ContainSingle().Which.Should().Be("flag1");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var analysis = new SessionSecurityAnalysis();

        analysis.SessionId.Should().Be(Guid.Empty);
        analysis.UserId.Should().Be(Guid.Empty);
        analysis.IsSuspicious.Should().BeFalse();
        analysis.RiskScore.Should().Be(0);
        analysis.ActiveSessionCount.Should().Be(0);
        analysis.TotalDeviceCount.Should().Be(0);
        analysis.RiskLevel.Should().Be(RiskLevel.Low);
        analysis.SecurityFlags.Should().BeEmpty();
        analysis.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var analysis = new SessionSecurityAnalysis
        {
            SessionId = sessionId,
            UserId = userId,
            IsSuspicious = true,
            RiskScore = 85,
            ActiveSessionCount = 3,
            TotalDeviceCount = 5,
            RiskLevel = RiskLevel.High,
            SecurityFlags = new List<string> { "multi_region" },
            Metadata = new Dictionary<string, string> { { "region", "US" } },
            AnalyzedAt = now
        };

        analysis.SessionId.Should().Be(sessionId);
        analysis.UserId.Should().Be(userId);
        analysis.ActiveSessionCount.Should().Be(3);
        analysis.TotalDeviceCount.Should().Be(5);
        analysis.AnalyzedAt.Should().Be(now);
    }
}

#endregion

#region EmailCredentialData Tests

public class EmailCredentialDataTests
{
    [Fact]
    public void Type_ShouldReturnEmail()
    {
        var cred = new EmailCredentialData();

        cred.Type.Should().Be("email");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var cred = new EmailCredentialData();

        cred.Email.Should().BeEmpty();
        cred.Password.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var cred = new EmailCredentialData
        {
            Email = "user@example.com",
            Password = "P@ssw0rd"
        };

        cred.Email.Should().Be("user@example.com");
        cred.Password.Should().Be("P@ssw0rd");
        cred.Type.Should().Be("email");
    }
}

#endregion

#region LocalSignInRequest Tests

public class LocalSignInRequestTests
{
    [Fact]
    public void EmailOrUsername_ShouldReturnEmail()
    {
        var request = new LocalSignInRequest { Email = "user@test.com" };

        request.EmailOrUsername.Should().Be("user@test.com");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var request = new LocalSignInRequest();

        request.Username.Should().BeNull();
        request.Email.Should().BeEmpty();
        request.Password.Should().BeEmpty();
        request.TenantId.Should().BeNull();
        request.DeviceFingerprint.Should().BeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        var tenantId = Guid.NewGuid();
        var request = new LocalSignInRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            TenantId = tenantId,
            DeviceFingerprint = "fp-abc123"
        };

        request.Username.Should().Be("testuser");
        request.Email.Should().Be("test@example.com");
        request.Password.Should().Be("password123");
        request.TenantId.Should().Be(tenantId);
        request.DeviceFingerprint.Should().Be("fp-abc123");
        request.EmailOrUsername.Should().Be("test@example.com");
    }
}

#endregion

#region MfaVerificationResult Tests

public class MfaVerificationResultTests
{
    [Fact]
    public void Successful_ShouldSetSuccess()
    {
        var result = MfaVerificationResult.Successful("Verified");

        result.IsSuccess.Should().BeTrue();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Verified");
        result.BackupCodes.Should().BeNull();
    }

    [Fact]
    public void Successful_WithBackupCodes_ShouldIncludeCodes()
    {
        var codes = new[] { "abc", "def" };
        var result = MfaVerificationResult.Successful("Enabled", codes);

        result.IsSuccess.Should().BeTrue();
        result.BackupCodes.Should().BeEquivalentTo(codes);
    }

    [Fact]
    public void Failure_ShouldSetFailure()
    {
        var result = MfaVerificationResult.Failure("Invalid code");

        result.IsSuccess.Should().BeFalse();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid code");
    }

    [Fact]
    public void Success_Alias_ShouldSyncWithIsSuccess()
    {
        var result = new MfaVerificationResult();
        result.Success = true;
        result.IsSuccess.Should().BeTrue();

        result.IsSuccess = false;
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new MfaVerificationResult();

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().BeNull();
        result.BackupCodes.Should().BeNull();
        result.RequiresAdditionalVerification.Should().BeFalse();
    }
}

#endregion

#region GenericResourcePermission Tests

public class GenericResourcePermissionTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var perm = new GenericResourcePermission(userId, tenantId, resourceId, "Course");

        perm.ResourceId.Should().Be(resourceId);
        perm.ResourceType.Should().Be("Course");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenResourceTypeNameIsNull()
    {
        var act = () => new GenericResourcePermission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DefaultConstructor_ShouldWork()
    {
        var perm = new GenericResourcePermission();

        perm.ResourceId.Should().Be(Guid.Empty);
        perm.ResourceType.Should().BeEmpty();
    }

    [Fact]
    public void UpdateResource_ShouldSetResourceIdAndTitle()
    {
        var perm = new GenericResourcePermission(Guid.NewGuid(), null, Guid.NewGuid(), "Doc");
        var newId = Guid.NewGuid();

        perm.UpdateResource(newId, "My Document");

        perm.ResourceId.Should().Be(newId);
        perm.ResourceTitle.Should().Be("My Document");
    }

    [Fact]
    public void UpdateResource_WithoutTitle_ShouldSetNullTitle()
    {
        var perm = new GenericResourcePermission(Guid.NewGuid(), null, Guid.NewGuid(), "Doc");

        perm.UpdateResource(Guid.NewGuid());

        perm.ResourceTitle.Should().BeNull();
    }

    [Fact]
    public void UpdateResourceTitle_ShouldSetTitle()
    {
        var perm = new GenericResourcePermission(Guid.NewGuid(), null, Guid.NewGuid(), "Doc");

        perm.UpdateResourceTitle("Updated Title");

        perm.ResourceTitle.Should().Be("Updated Title");
    }

    [Fact]
    public void AppliesToResource_ShouldReturnTrue_WhenMatching()
    {
        var resourceId = Guid.NewGuid();
        var perm = new GenericResourcePermission(Guid.NewGuid(), null, resourceId, "Item");

        perm.AppliesToResource(resourceId).Should().BeTrue();
    }

    [Fact]
    public void AppliesToResource_ShouldReturnFalse_WhenNotMatching()
    {
        var perm = new GenericResourcePermission(Guid.NewGuid(), null, Guid.NewGuid(), "Item");

        perm.AppliesToResource(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsForUserAndResource_ShouldReturnTrue_WhenBothMatch()
    {
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var perm = new GenericResourcePermission(userId, null, resourceId, "Item");

        perm.IsForUserAndResource(userId, resourceId).Should().BeTrue();
    }

    [Fact]
    public void IsForUserAndResource_ShouldReturnFalse_WhenUserDoesntMatch()
    {
        var resourceId = Guid.NewGuid();
        var perm = new GenericResourcePermission(Guid.NewGuid(), null, resourceId, "Item");

        perm.IsForUserAndResource(Guid.NewGuid(), resourceId).Should().BeFalse();
    }
}

#endregion

#region AuthenticationModuleOptions Tests

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
        options.MaxPoliciesPerEvaluation.Should().Be(50);
    }
}

#endregion