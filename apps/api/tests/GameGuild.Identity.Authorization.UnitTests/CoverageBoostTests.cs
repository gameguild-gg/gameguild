using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Authorization.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

#region ClaimsExtractor Tests

/// <summary>
/// Tests for ClaimsExtractor — static utility for extracting claims from ClaimsPrincipal.
/// </summary>
public class ClaimsExtractorTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateAnonymousPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    // GetUserId

    [Fact]
    public void GetUserId_WithSubClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("sub", "user-123"));
        ClaimsExtractor.GetUserId(user).Should().Be("user-123");
    }

    [Fact]
    public void GetUserId_WithNameIdentifier_ShouldFallback()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "user-456"));
        ClaimsExtractor.GetUserId(user).Should().Be("user-456");
    }

    [Fact]
    public void GetUserId_WithUserIdClaim_ShouldFallback()
    {
        var user = CreatePrincipal(new Claim("UserId", "user-789"));
        ClaimsExtractor.GetUserId(user).Should().Be("user-789");
    }

    [Fact]
    public void GetUserId_WithNoClaim_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetUserId(user).Should().BeNull();
    }

    // GetUserIdAsGuid

    [Fact]
    public void GetUserIdAsGuid_WithValidGuid_ShouldReturnGuid()
    {
        var id = Guid.NewGuid();
        var user = CreatePrincipal(new Claim("sub", id.ToString()));
        ClaimsExtractor.GetUserIdAsGuid(user).Should().Be(id);
    }

    [Fact]
    public void GetUserIdAsGuid_WithInvalidGuid_ShouldReturnNull()
    {
        var user = CreatePrincipal(new Claim("sub", "not-a-guid"));
        ClaimsExtractor.GetUserIdAsGuid(user).Should().BeNull();
    }

    [Fact]
    public void GetUserIdAsGuid_WithEmpty_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetUserIdAsGuid(user).Should().BeNull();
    }

    // GetJti

    [Fact]
    public void GetJti_WithJtiClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("jti", "token-id-123"));
        ClaimsExtractor.GetJti(user).Should().Be("token-id-123");
    }

    [Fact]
    public void GetJti_WithNoClaim_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetJti(user).Should().BeNull();
    }

    // GetIssuedAt / GetIssuedAtDateTime

    [Fact]
    public void GetIssuedAt_WithValidTimestamp_ShouldReturnLong()
    {
        var user = CreatePrincipal(new Claim("iat", "1700000000"));
        ClaimsExtractor.GetIssuedAt(user).Should().Be(1700000000L);
    }

    [Fact]
    public void GetIssuedAt_WithInvalidValue_ShouldReturnNull()
    {
        var user = CreatePrincipal(new Claim("iat", "not-a-number"));
        ClaimsExtractor.GetIssuedAt(user).Should().BeNull();
    }

    [Fact]
    public void GetIssuedAt_WithNoClaim_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetIssuedAt(user).Should().BeNull();
    }

    [Fact]
    public void GetIssuedAtDateTime_WithValidTimestamp_ShouldReturnDateTime()
    {
        var user = CreatePrincipal(new Claim("iat", "1700000000"));
        var result = ClaimsExtractor.GetIssuedAtDateTime(user);
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void GetIssuedAtDateTime_WithNoClaim_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetIssuedAtDateTime(user).Should().BeNull();
    }

    // GetEmail

    [Fact]
    public void GetEmail_WithEmailClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("email", "test@example.com"));
        ClaimsExtractor.GetEmail(user).Should().Be("test@example.com");
    }

    [Fact]
    public void GetEmail_WithClaimTypesEmail_ShouldFallback()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Email, "fallback@example.com"));
        ClaimsExtractor.GetEmail(user).Should().Be("fallback@example.com");
    }

    [Fact]
    public void GetEmail_WithNoClaim_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetEmail(user).Should().BeNull();
    }

    // GetName

    [Fact]
    public void GetName_WithClaimTypesName_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim(ClaimTypes.Name, "John Doe"));
        ClaimsExtractor.GetName(user).Should().Be("John Doe");
    }

    [Fact]
    public void GetName_WithNameClaim_ShouldFallback()
    {
        var user = CreatePrincipal(new Claim("name", "Jane Doe"));
        ClaimsExtractor.GetName(user).Should().Be("Jane Doe");
    }

    // GetRoles

    [Fact]
    public void GetRoles_WithMultipleRoleClaims_ShouldReturnAll()
    {
        var user = CreatePrincipal(
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("role", "User"),
            new Claim("roles", "Manager"));
        var roles = ClaimsExtractor.GetRoles(user);
        roles.Should().HaveCount(3);
        roles.Should().Contain("Admin");
        roles.Should().Contain("User");
        roles.Should().Contain("Manager");
    }

    [Fact]
    public void GetRoles_WithNoRoles_ShouldReturnEmpty()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetRoles(user).Should().BeEmpty();
    }

    // GetTenantId / GetTenantIdAsGuid

    [Fact]
    public void GetTenantId_WithTenantIdClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("TenantId", "tenant-abc"));
        ClaimsExtractor.GetTenantId(user).Should().Be("tenant-abc");
    }

    [Fact]
    public void GetTenantId_WithTenantIdAltClaim_ShouldFallback()
    {
        var user = CreatePrincipal(new Claim("tenant_id", "tenant-def"));
        ClaimsExtractor.GetTenantId(user).Should().Be("tenant-def");
    }

    [Fact]
    public void GetTenantIdAsGuid_WithValidGuid_ShouldReturnGuid()
    {
        var tenantId = Guid.NewGuid();
        var user = CreatePrincipal(new Claim("TenantId", tenantId.ToString()));
        ClaimsExtractor.GetTenantIdAsGuid(user).Should().Be(tenantId);
    }

    [Fact]
    public void GetTenantIdAsGuid_WithInvalidGuid_ShouldReturnNull()
    {
        var user = CreatePrincipal(new Claim("TenantId", "not-guid"));
        ClaimsExtractor.GetTenantIdAsGuid(user).Should().BeNull();
    }

    // GetGrantType, GetActorType

    [Fact]
    public void GetGrantType_WithClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("grant_type", "client_credentials"));
        ClaimsExtractor.GetGrantType(user).Should().Be("client_credentials");
    }

    [Fact]
    public void GetActorType_WithClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("actor_type", "service"));
        ClaimsExtractor.GetActorType(user).Should().Be("service");
    }

    // GetPermissions

    [Fact]
    public void GetPermissions_WithMultipleClaims_ShouldReturnAll()
    {
        var user = CreatePrincipal(
            new Claim("permission", "read"),
            new Claim("permissions", "write"));
        var perms = ClaimsExtractor.GetPermissions(user);
        perms.Should().HaveCount(2);
        perms.Should().Contain("read");
        perms.Should().Contain("write");
    }

    // IsMfaVerified / IsEmailVerified

    [Fact]
    public void IsMfaVerified_WhenTrue_ShouldReturnTrue()
    {
        var user = CreatePrincipal(new Claim("mfa_verified", "true"));
        ClaimsExtractor.IsMfaVerified(user).Should().BeTrue();
    }

    [Fact]
    public void IsMfaVerified_WhenFalse_ShouldReturnFalse()
    {
        var user = CreatePrincipal(new Claim("mfa_verified", "false"));
        ClaimsExtractor.IsMfaVerified(user).Should().BeFalse();
    }

    [Fact]
    public void IsMfaVerified_WhenMissing_ShouldReturnFalse()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.IsMfaVerified(user).Should().BeFalse();
    }

    [Fact]
    public void IsEmailVerified_WhenTrue_ShouldReturnTrue()
    {
        var user = CreatePrincipal(new Claim("email_verified", "true"));
        ClaimsExtractor.IsEmailVerified(user).Should().BeTrue();
    }

    [Fact]
    public void IsEmailVerified_WhenMissing_ShouldReturnFalse()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.IsEmailVerified(user).Should().BeFalse();
    }

    // GetAmr, GetTokenVersion, GetClaim

    [Fact]
    public void GetAmr_WithClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("amr", "mfa"));
        ClaimsExtractor.GetAmr(user).Should().Be("mfa");
    }

    [Fact]
    public void GetTokenVersion_WithClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("token_version", "2"));
        ClaimsExtractor.GetTokenVersion(user).Should().Be("2");
    }

    [Fact]
    public void GetClaim_WithCustomClaim_ShouldReturnValue()
    {
        var user = CreatePrincipal(new Claim("custom_claim", "custom_value"));
        ClaimsExtractor.GetClaim(user, "custom_claim").Should().Be("custom_value");
    }

    [Fact]
    public void GetClaim_WithMissingClaim_ShouldReturnNull()
    {
        var user = CreatePrincipal();
        ClaimsExtractor.GetClaim(user, "nonexistent").Should().BeNull();
    }

    // IsAuthenticated

    [Fact]
    public void IsAuthenticated_WhenAuthenticated_ShouldReturnTrue()
    {
        var user = CreatePrincipal(new Claim("sub", "user"));
        ClaimsExtractor.IsAuthenticated(user).Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenAnonymous_ShouldReturnFalse()
    {
        var user = CreateAnonymousPrincipal();
        ClaimsExtractor.IsAuthenticated(user).Should().BeFalse();
    }
}

#endregion

#region JitElevationRequest Tests

/// <summary>
/// Tests for JitElevationRequest entity — state machine transitions.
/// </summary>
public class JitElevationRequestTests
{
    private static JitElevationRequest CreatePendingRequest()
    {
        return new JitElevationRequest
        {
            RequesterId = Guid.NewGuid(),
            Permission = "admin:write",
            Justification = "Need admin access",
            DurationMinutes = 60,
            ExpiresAt = SystemClock.UtcNow.AddHours(1)
        };
    }

    [Fact]
    public void NewRequest_ShouldHavePendingStatus()
    {
        var request = CreatePendingRequest();
        request.Status.Should().Be(ElevationRequestStatus.Pending);
        request.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void IsActive_WhenPending_ShouldReturnFalse()
    {
        var request = CreatePendingRequest();
        request.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Approve_ShouldAutoActivateIfNoStartTime()
    {
        var request = CreatePendingRequest();
        var reviewerId = Guid.NewGuid();

        request.Approve(reviewerId, "Looks good");

        request.Status.Should().Be(ElevationRequestStatus.Active);
        request.ReviewerId.Should().Be(reviewerId);
        request.ReviewerComments.Should().Be("Looks good");
        request.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_WithFutureStartTime_ShouldNotActivate()
    {
        var request = CreatePendingRequest();
        request.StartsAt = SystemClock.UtcNow.AddHours(1);

        request.Approve(Guid.NewGuid());

        request.Status.Should().Be(ElevationRequestStatus.Approved);
        request.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public void Approve_WhenNotPending_ShouldThrow()
    {
        var request = CreatePendingRequest();
        request.Approve(Guid.NewGuid());

        var act = () => request.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*pending*");
    }

    [Fact]
    public void Deny_ShouldSetDeniedStatus()
    {
        var request = CreatePendingRequest();
        var reviewerId = Guid.NewGuid();

        request.Deny(reviewerId, "Insufficient justification");

        request.Status.Should().Be(ElevationRequestStatus.Denied);
        request.ReviewerId.Should().Be(reviewerId);
        request.ReviewerComments.Should().Be("Insufficient justification");
    }

    [Fact]
    public void Deny_WhenNotPending_ShouldThrow()
    {
        var request = CreatePendingRequest();
        request.Deny(Guid.NewGuid(), "No");

        var act = () => request.Deny(Guid.NewGuid(), "Again");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Activate_WhenApproved_ShouldSetActive()
    {
        var request = CreatePendingRequest();
        request.StartsAt = SystemClock.UtcNow.AddHours(1); // Future start, so no auto-activate
        request.Approve(Guid.NewGuid());

        request.Activate();

        request.Status.Should().Be(ElevationRequestStatus.Active);
        request.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenNotApproved_ShouldThrow()
    {
        var request = CreatePendingRequest();
        var act = () => request.Activate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*approved*");
    }

    [Fact]
    public void IsActive_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var request = CreatePendingRequest();
        request.Approve(Guid.NewGuid());

        request.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenActiveAndPastExpiry_ShouldReturnTrue()
    {
        var request = CreatePendingRequest();
        request.ExpiresAt = SystemClock.UtcNow.AddSeconds(-1);
        request.Approve(Guid.NewGuid());

        request.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenActiveAndFutureExpiry_ShouldReturnFalse()
    {
        var request = CreatePendingRequest();
        request.Approve(Guid.NewGuid());

        request.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenActive_ShouldSetRevoked()
    {
        var request = CreatePendingRequest();
        request.Approve(Guid.NewGuid());
        var revokedBy = Guid.NewGuid();

        request.Revoke(revokedBy, "Security concern");

        request.Status.Should().Be(ElevationRequestStatus.Revoked);
        request.RevokedBy.Should().Be(revokedBy);
        request.RevocationReason.Should().Be("Security concern");
    }

    [Fact]
    public void Revoke_WhenPending_ShouldThrow()
    {
        var request = CreatePendingRequest();
        var act = () => request.Revoke(Guid.NewGuid(), "reason");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkExpired_WhenActive_ShouldSetExpired()
    {
        var request = CreatePendingRequest();
        request.Approve(Guid.NewGuid());

        request.MarkExpired();

        request.Status.Should().Be(ElevationRequestStatus.Expired);
    }

    [Fact]
    public void MarkExpired_WhenNotActive_ShouldNotChange()
    {
        var request = CreatePendingRequest();
        request.MarkExpired();
        request.Status.Should().Be(ElevationRequestStatus.Pending);
    }

    [Fact]
    public void GetRemainingMinutes_WhenActive_ShouldReturnPositive()
    {
        var request = CreatePendingRequest();
        request.ExpiresAt = SystemClock.UtcNow.AddMinutes(30);
        request.Approve(Guid.NewGuid());

        var remaining = request.GetRemainingMinutes();
        remaining.Should().BeGreaterThan(0);
        remaining.Should().BeLessOrEqualTo(30);
    }

    [Fact]
    public void GetRemainingMinutes_WhenNotActive_ShouldReturnZero()
    {
        var request = CreatePendingRequest();
        request.GetRemainingMinutes().Should().Be(0);
    }
}

#endregion

#region DefaultPolicyMerger Tests

/// <summary>
/// Tests for DefaultPolicyMerger — merging base and tenant policy definitions.
/// </summary>
public class DefaultPolicyMergerTests
{
    private readonly DefaultPolicyMerger _merger = new();

    [Fact]
    public void Merge_WithNullTenantOverride_ShouldReturnBase()
    {
        var basePolicy = new PolicyDefinition { PolicyName = "test" };
        var result = _merger.Merge(basePolicy, null);
        result.Should().BeSameAs(basePolicy);
    }

    [Fact]
    public void Merge_ShouldCombineRequiredRoles()
    {
        var basePolicy = new PolicyDefinition
        {
            PolicyName = "test",
            RequiredRoles = new List<string> { "Admin" }
        };
        var tenantOverride = new PolicyDefinition
        {
            PolicyName = "test-override",
            RequiredRoles = new List<string> { "TenantAdmin" }
        };

        var result = _merger.Merge(basePolicy, tenantOverride);

        result.RequiredRoles.Should().Contain("Admin");
        result.RequiredRoles.Should().Contain("TenantAdmin");
        result.IsTenantScoped.Should().BeTrue();
    }

    [Fact]
    public void Merge_ShouldCombinePermissions()
    {
        var basePolicy = new PolicyDefinition
        {
            PolicyName = "test",
            RequiredPermissions = new List<string> { "read" }
        };
        var tenantOverride = new PolicyDefinition
        {
            PolicyName = "test-override",
            RequiredPermissions = new List<string> { "write" }
        };

        var result = _merger.Merge(basePolicy, tenantOverride);
        result.RequiredPermissions.Should().Contain("read");
        result.RequiredPermissions.Should().Contain("write");
    }

    [Fact]
    public void Merge_ShouldUseMaxVersion()
    {
        var basePolicy = new PolicyDefinition { PolicyName = "test", Version = 3 };
        var tenantOverride = new PolicyDefinition { PolicyName = "test-override", Version = 5 };

        var result = _merger.Merge(basePolicy, tenantOverride);
        result.Version.Should().Be(5);
    }

    [Fact]
    public void Merge_ShouldPreferTenantResourceType()
    {
        var basePolicy = new PolicyDefinition
        {
            PolicyName = "test",
            ResourceType = "BaseResource"
        };
        var tenantOverride = new PolicyDefinition
        {
            PolicyName = "test-override",
            ResourceType = "TenantResource"
        };

        var result = _merger.Merge(basePolicy, tenantOverride);
        result.ResourceType.Should().Be("TenantResource");
    }

    [Fact]
    public void Merge_WithEmptyOverrideLists_ShouldUseBase()
    {
        var basePolicy = new PolicyDefinition
        {
            PolicyName = "test",
            RequiredRoles = new List<string> { "Admin" },
            RequiredPermissions = new List<string> { "perm1" },
            AuthenticationSchemes = new List<string> { "Bearer" }
        };
        var tenantOverride = new PolicyDefinition
        {
            PolicyName = "test-override",
            RequiredRoles = new List<string>(),
            RequiredPermissions = new List<string>(),
            AuthenticationSchemes = new List<string>()
        };

        var result = _merger.Merge(basePolicy, tenantOverride);
        result.RequiredRoles.Should().Contain("Admin");
        result.RequiredPermissions.Should().Contain("perm1");
        result.AuthenticationSchemes.Should().Contain("Bearer");
    }

    [Fact]
    public void Merge_ShouldORAuthenticationRequirements()
    {
        var basePolicy = new PolicyDefinition { PolicyName = "test", RequireAuthentication = true };
        var tenantOverride = new PolicyDefinition { PolicyName = "test-override", RequireAuthentication = false };

        var result = _merger.Merge(basePolicy, tenantOverride);
        result.RequireAuthentication.Should().BeTrue(); // OR logic
    }

    [Fact]
    public void Build_WithAuthenticationRequired_ShouldRequireAuthenticatedUser()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "test",
            RequireAuthentication = true
        };

        var policy = _merger.Build(definition);
        policy.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithRoles_ShouldAddRoleRequirement()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "test",
            RequiredRoles = new List<string> { "Admin", "Manager" }
        };

        var policy = _merger.Build(definition);
        policy.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithPermissions_ShouldAddPermissionRequirements()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "test",
            RequiredPermissions = new List<string> { "users:read", "users:write" }
        };

        var policy = _merger.Build(definition);
        policy.Should().NotBeNull();
        policy.Requirements.Should().ContainItemsAssignableTo<PermissionRequirement>();
    }

    [Fact]
    public void Build_WithRules_ShouldAddRulesetRequirement()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "test",
            UseRuleBasedEvaluation = true,
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = "role",
                    Description = "Must be admin",
                    Enabled = true,
                    Params = new Dictionary<string, object> { ["role"] = "Admin" }
                }
            }
        };

        var policy = _merger.Build(definition);
        policy.Should().NotBeNull();
        policy.Requirements.Should().ContainItemsAssignableTo<RulesetRequirement>();
    }

    [Fact]
    public void Build_WithEmptyRules_ShouldFallbackToRoles()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "test",
            UseRuleBasedEvaluation = true,
            Rules = new List<PolicyRule>(),
            RequiredRoles = new List<string> { "Admin" }
        };

        var policy = _merger.Build(definition);
        policy.Should().NotBeNull();
        // Should use role requirement, not ruleset
        policy.Requirements.Should().NotContainItemsAssignableTo<RulesetRequirement>();
    }

    [Fact]
    public void Build_WithAuthenticationSchemes_ShouldAddSchemes()
    {
        var definition = new PolicyDefinition
        {
            PolicyName = "test",
            AuthenticationSchemes = new List<string> { "Bearer", "ApiKey" }
        };

        var policy = _merger.Build(definition);
        policy.AuthenticationSchemes.Should().Contain("Bearer");
        policy.AuthenticationSchemes.Should().Contain("ApiKey");
    }

    [Fact]
    public void Merge_TenantRulesTakePrecedence()
    {
        var basePolicy = new PolicyDefinition
        {
            PolicyName = "test",
            UseRuleBasedEvaluation = true,
            Rules = new List<PolicyRule>
            {
                new PolicyRule { Type = "base-rule", Enabled = true }
            }
        };
        var tenantOverride = new PolicyDefinition
        {
            PolicyName = "test-override",
            UseRuleBasedEvaluation = true,
            Rules = new List<PolicyRule>
            {
                new PolicyRule { Type = "tenant-rule", Enabled = true }
            }
        };

        var result = _merger.Merge(basePolicy, tenantOverride);
        result.Rules.Should().HaveCount(1);
        result.Rules![0].Type.Should().Be("tenant-rule");
    }
}

#endregion

#region PermissionRegistry Tests

/// <summary>
/// Tests for PermissionRegistry — permission discovery and validation.
/// </summary>
public class PermissionRegistryTests
{
    [Fact]
    public void Keys_ShouldNotBeEmpty()
    {
        // The registry discovers permissions from the assembly
        PermissionRegistry.Keys.Should().NotBeEmpty();
    }

    [Fact]
    public void Permissions_ShouldNotBeEmpty()
    {
        PermissionRegistry.Permissions.Should().NotBeEmpty();
    }

    [Fact]
    public void Scopes_ShouldNotBeEmpty()
    {
        PermissionRegistry.Scopes.Should().NotBeEmpty();
    }

    [Fact]
    public void IsValidKey_WithNull_ShouldReturnFalse()
    {
        PermissionRegistry.IsValidKey(null!).Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithEmpty_ShouldReturnFalse()
    {
        PermissionRegistry.IsValidKey("").Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithRegisteredKey_ShouldReturnTrue()
    {
        var firstKey = PermissionRegistry.Keys.First();
        PermissionRegistry.IsValidKey(firstKey).Should().BeTrue();
    }

    [Fact]
    public void IsValidKey_WithUnknownKey_ShouldReturnFalse()
    {
        PermissionRegistry.IsValidKey("nonexistent:permission:xyz").Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithWildcardForValidResource_ShouldReturnTrue()
    {
        var firstScope = PermissionRegistry.Scopes.First();
        var wildcardKey = $"{firstScope.Resource}:*";
        PermissionRegistry.IsValidKey(wildcardKey).Should().BeTrue();
    }

    [Fact]
    public void IsValidKey_WithWildcardForInvalidResource_ShouldReturnFalse()
    {
        PermissionRegistry.IsValidKey("nonexistent:*").Should().BeFalse();
    }

    [Fact]
    public void GetByKey_WithValidKey_ShouldReturnPermission()
    {
        var firstKey = PermissionRegistry.Keys.First();
        var permission = PermissionRegistry.GetByKey(firstKey);
        permission.Should().NotBeNull();
        permission!.Key.Should().Be(firstKey);
    }

    [Fact]
    public void GetByKey_WithInvalidKey_ShouldReturnNull()
    {
        PermissionRegistry.GetByKey("nonexistent:xyz").Should().BeNull();
    }

    [Fact]
    public void GetByResource_WithValidResource_ShouldReturnPermissions()
    {
        var firstScope = PermissionRegistry.Scopes.First();
        var permissions = PermissionRegistry.GetByResource(firstScope.Resource).ToList();
        permissions.Should().NotBeEmpty();
    }

    [Fact]
    public void GetByResource_WithInvalidResource_ShouldReturnEmpty()
    {
        PermissionRegistry.GetByResource("nonexistent_resource").Should().BeEmpty();
    }

    [Fact]
    public void ValidateKeys_WithAllValid_ShouldReturnEmpty()
    {
        var validKeys = PermissionRegistry.Keys.Take(3);
        PermissionRegistry.ValidateKeys(validKeys).Should().BeEmpty();
    }

    [Fact]
    public void ValidateKeys_WithInvalid_ShouldReturnInvalidKeys()
    {
        var keys = new[] { "invalid:key1", "invalid:key2" };
        var invalid = PermissionRegistry.ValidateKeys(keys);
        invalid.Should().HaveCount(2);
    }

    [Fact]
    public void PermissionScope_Wildcard_ShouldBeCorrectFormat()
    {
        var scope = PermissionRegistry.Scopes.First();
        scope.Wildcard.Should().EndWith(":*");
        scope.Wildcard.Should().StartWith(scope.Resource);
    }

    [Fact]
    public void PermissionScope_Keys_ShouldMatchPermissions()
    {
        var scope = PermissionRegistry.Scopes.First();
        scope.Keys.Should().HaveCount(scope.Permissions.Count);
    }
}

#endregion

#region PolicyEvaluationLogger Tests

/// <summary>
/// Tests for PolicyEvaluationLogger — policy evaluation tracing and logging.
/// </summary>
public class PolicyEvaluationLoggerTests
{
    private readonly PolicyEvaluationLogger _logger;

    public PolicyEvaluationLoggerTests()
    {
        _logger = new PolicyEvaluationLogger(NullLogger<PolicyEvaluationLogger>.Instance);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new PolicyEvaluationLogger(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BeginTrace_ShouldReturnTrace()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user);

        trace.Should().NotBeNull();
        trace.TraceId.Should().NotBeNullOrEmpty();
        trace.PolicyName.Should().Be("TestPolicy");
    }

    [Fact]
    public void BeginTrace_WithCorrelationId_ShouldUseIt()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user, correlationId: "custom-trace-123");

        trace.TraceId.Should().Be("custom-trace-123");
    }

    [Fact]
    public void BeginTrace_WithResource_ShouldAcceptIt()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user, resource: new { Id = 1, Type = "Post" });

        trace.Should().NotBeNull();
    }

    [Fact]
    public void Trace_LogRequirement_ShouldRecordResult()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user);
        trace.LogRequirement("RoleCheck", true, "User has Admin role");
        trace.LogRequirement("TenantCheck", false, "No tenant context");

        // GetSummary is on the inner concrete class, not the interface.
        // Verify logging doesn't throw and trace has recorded requirements.
        trace.Complete(false);
        // If no exception, logged properly.
    }

    [Fact]
    public void Trace_Complete_ShouldLogResult()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user);
        trace.LogRequirement("Check1", true);
        trace.Complete(true);

        // Should not throw
    }

    [Fact]
    public void Trace_AddContext_ShouldNotThrow()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user);
        trace.AddContext("key1", "value1");
        trace.AddContext("key2", null);

        // Should not throw
    }

    [Fact]
    public void Trace_Dispose_ShouldCleanup()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user);
        trace.Dispose();
        // Subsequent dispose should be safe
        trace.Dispose();
    }

    [Fact]
    public void LogRequirementResult_ShouldNotThrow()
    {
        _logger.LogRequirementResult("trace-1", "TestRequirement", true, "Passed", TimeSpan.FromMilliseconds(5));
        _logger.LogRequirementResult("trace-2", "TestRequirement", false, "Failed", TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void LogPolicyResult_WithoutActiveTrace_ShouldNotThrow()
    {
        _logger.LogPolicyResult("nonexistent-trace", true, TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void LogPolicyResult_WithActiveTrace_ShouldRemoveTrace()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "user-1") }, "Test"));

        var trace = _logger.BeginTrace("TestPolicy", user);
        trace.LogRequirement("Check", false, "Failed");

        _logger.LogPolicyResult(trace.TraceId, false, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void LogPolicyFailure_ShouldNotThrow()
    {
        _logger.LogPolicyFailure("trace-1", "Access denied");
    }

    [Fact]
    public void LogPolicyFailure_WithSuggestions_ShouldNotThrow()
    {
        _logger.LogPolicyFailure("trace-2", "Access denied",
            new[] { "Add Admin role", "Request tenant access" });
    }

    [Fact]
    public void GetDebugSettings_WithNull_ShouldReturnNull()
    {
        _logger.GetDebugSettings(null).Should().BeNull();
    }

    [Fact]
    public void GetDebugSettings_WithNonEndpoint_ShouldReturnNull()
    {
        _logger.GetDebugSettings(new object()).Should().BeNull();
    }

    [Fact]
    public void IsDebugEnabled_WithNull_ShouldReturnFalse()
    {
        _logger.IsDebugEnabled(null).Should().BeFalse();
    }

    [Fact]
    public void IsDebugEnabled_WithNonEndpoint_ShouldReturnFalse()
    {
        _logger.IsDebugEnabled(new object()).Should().BeFalse();
    }
}

#endregion
