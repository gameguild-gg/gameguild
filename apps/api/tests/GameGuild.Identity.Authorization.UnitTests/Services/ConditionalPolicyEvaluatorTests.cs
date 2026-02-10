using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class ConditionalPolicyEvaluatorTests
{
    private readonly Mock<IConditionalPolicyRepository> _repoMock = new();
    private readonly ConditionalPolicyEvaluator _sut;

    public ConditionalPolicyEvaluatorTests()
    {
        _sut = new ConditionalPolicyEvaluator(
            _repoMock.Object,
            NullLogger<ConditionalPolicyEvaluator>.Instance
        );
    }

    private static ConditionalPolicyContext CreateContext(
        string resourceType = "document",
        string action = "read",
        string? ipAddress = "192.168.1.1",
        string? userAgent = "TestBrowser/1.0",
        string? geoCountry = null,
        string? deviceFingerprint = null,
        bool? isMfaVerified = null,
        int? riskScore = null,
        DateTime? authTime = null,
        IReadOnlyDictionary<string, string>? customAttributes = null)
    {
        return new ConditionalPolicyContext(
            UserId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            ResourceType: resourceType,
            ResourceId: null,
            Action: action,
            UserRoles: new List<string> { "User" },
            IpAddress: ipAddress,
            UserAgent: userAgent,
            DeviceFingerprint: deviceFingerprint,
            GeoCountry: geoCountry,
            IsMfaVerified: isMfaVerified,
            RiskScore: riskScore,
            AuthenticationTime: authTime,
            CustomAttributes: customAttributes);
    }

    private static ConditionalPolicy CreatePolicy(
        PolicyAction action = PolicyAction.Deny,
        string? permissionType = null,
        string? resourceType = null,
        int priority = 100,
        string? timeConditions = null,
        string? environmentConditions = null,
        string? locationConditions = null,
        string? deviceConditions = null,
        string? customConditions = null)
    {
        return new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Test Policy",
            Action = action,
            Priority = priority,
            IsEnabled = true,
            PermissionType = permissionType,
            ResourceType = resourceType,
            TimeConditions = timeConditions,
            EnvironmentConditions = environmentConditions,
            LocationConditions = locationConditions,
            DeviceConditions = deviceConditions,
            CustomConditions = customConditions
        };
    }

    private void SetupPolicies(params ConditionalPolicy[] policies)
    {
        _repoMock
            .Setup(x => x.GetActivePoliciesAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConditionalPolicy>(policies));
    }

    // ── Basic evaluation ──────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_NoPolicies_ReturnsAllowed()
    {
        SetupPolicies();
        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_AllowPolicy_NoConditions_ReturnsAllowed()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Allow));
        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_DenyPolicy_NoConditions_ReturnsDenied()
    {
        var denyPolicy = CreatePolicy(action: PolicyAction.Deny);
        SetupPolicies(denyPolicy);

        var result = await _sut.EvaluateAsync(CreateContext());

        result.IsAllowed.Should().BeFalse();
        result.DeniedByPolicyId.Should().Be(denyPolicy.Id);
        result.DeniedByPolicyName.Should().Be("Test Policy");
        result.DenialReason.Should().Contain("Test Policy");
    }

    [Fact]
    public async Task EvaluateAsync_DenyWins_OverAllowPolicy_HigherPriority()
    {
        // Deny at priority 100, Allow at priority 50 → deny evaluated first (desc order)
        var allowPolicy = CreatePolicy(action: PolicyAction.Allow, priority: 50);
        var denyPolicy = CreatePolicy(action: PolicyAction.Deny, priority: 100);
        SetupPolicies(allowPolicy, denyPolicy);

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_AllowHigherPriority_DenyLowerPriority_DenyStillBlocks()
    {
        // Allow at priority 100 first, Deny at priority 50 second → deny still blocks
        var allowPolicy = CreatePolicy(action: PolicyAction.Allow, priority: 100);
        var denyPolicy = CreatePolicy(action: PolicyAction.Deny, priority: 50);
        SetupPolicies(allowPolicy, denyPolicy);

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_DisabledPolicy_Ignored()
    {
        var disabledPolicy = CreatePolicy(action: PolicyAction.Deny);
        disabledPolicy.IsEnabled = false;
        SetupPolicies(disabledPolicy);

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_MultipleAllowPolicies_AllAllowed()
    {
        SetupPolicies(
            CreatePolicy(action: PolicyAction.Allow, priority: 100),
            CreatePolicy(action: PolicyAction.Allow, priority: 50));

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeTrue();
    }

    // ── Resource type / permission filtering ──────────────────

    [Fact]
    public async Task EvaluateAsync_PolicyForDifferentResourceType_NotApplied()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, resourceType: "other-resource"));

        var result = await _sut.EvaluateAsync(CreateContext(resourceType: "document"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_PolicyForMatchingResourceType_Applied()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, resourceType: "document"));

        var result = await _sut.EvaluateAsync(CreateContext(resourceType: "document"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_PolicyWithNullResourceType_AppliesToAll()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, resourceType: null));

        var result = await _sut.EvaluateAsync(CreateContext(resourceType: "anything"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_PolicyForDifferentPermissionType_NotApplied()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, permissionType: "delete"));

        var result = await _sut.EvaluateAsync(CreateContext(action: "read"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_PolicyForMatchingPermissionType_Applied()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, permissionType: "write"));

        var result = await _sut.EvaluateAsync(CreateContext(action: "write"));
        result.IsAllowed.Should().BeFalse();
    }

    // ── Environment conditions ────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_RequiresMfa_NotVerified_ConditionsNotMet_DenySkipped()
    {
        // RequireMfa=true + IsMfaVerified=false → EvaluateEnvironmentConditions returns false
        // → conditionsMet=false → deny NOT applied → allowed
        var envConditions = JsonSerializer.Serialize(new { RequireMfa = true });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: envConditions));

        var result = await _sut.EvaluateAsync(CreateContext(isMfaVerified: false));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_RequiresMfa_Verified_ConditionsMet_DenyApplied()
    {
        // RequireMfa=true + IsMfaVerified=true → conditions pass → deny applied
        var envConditions = JsonSerializer.Serialize(new { RequireMfa = true });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: envConditions));

        var result = await _sut.EvaluateAsync(CreateContext(isMfaVerified: true));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_MaxRiskScore_Exceeded_ConditionsNotMet_DenySkipped()
    {
        // MaxRiskScore=50, context.RiskScore=80 → 80 > 50 → returns false → deny skipped
        var envConditions = JsonSerializer.Serialize(new { MaxRiskScore = 50 });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: envConditions));

        var result = await _sut.EvaluateAsync(CreateContext(riskScore: 80));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_MaxRiskScore_WithinLimit_ConditionsMet_DenyApplied()
    {
        // MaxRiskScore=50, context.RiskScore=30 → 30 <= 50 → conditions pass → deny applied
        var envConditions = JsonSerializer.Serialize(new { MaxRiskScore = 50 });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: envConditions));

        var result = await _sut.EvaluateAsync(CreateContext(riskScore: 30));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_SessionAge_Exceeded_ConditionsNotMet()
    {
        // MaxSessionAgeMinutes=60, auth was 2 hours ago → session age 120 > 60 → returns false
        var envConditions = JsonSerializer.Serialize(new { MaxSessionAgeMinutes = 60 });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: envConditions));

        var result = await _sut.EvaluateAsync(CreateContext(authTime: DateTime.UtcNow.AddHours(-2)));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_SessionAge_WithinLimit_ConditionsMet()
    {
        // MaxSessionAgeMinutes=60, auth was 10 min ago → 10 < 60 → conditions pass → deny
        var envConditions = JsonSerializer.Serialize(new { MaxSessionAgeMinutes = 60 });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: envConditions));

        var result = await _sut.EvaluateAsync(CreateContext(authTime: DateTime.UtcNow.AddMinutes(-10)));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_InvalidEnvironmentJson_TreatedAsTrue()
    {
        // Invalid JSON → exception caught → returns true → conditionsMet=true → deny applied
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, environmentConditions: "not-valid-json"));

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeFalse();
    }

    // ── Location conditions ───────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_BlockedCountry_ConditionsNotMet_DenySkipped()
    {
        // BlockedCountries contains "CN", context.GeoCountry="CN" → returns false → deny skipped
        var loc = JsonSerializer.Serialize(new { BlockedCountries = new[] { "CN", "RU" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(geoCountry: "CN"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NotBlockedCountry_ConditionsMet_DenyApplied()
    {
        // BlockedCountries=["CN","RU"], context.GeoCountry="US" → not blocked → passes → deny applied
        var loc = JsonSerializer.Serialize(new { BlockedCountries = new[] { "CN", "RU" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(geoCountry: "US"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_AllowedCountry_InList_ConditionsMet_DenyApplied()
    {
        // AllowedCountries=["US","GB"], context.GeoCountry="US" → in list → passes → deny applied
        var loc = JsonSerializer.Serialize(new { AllowedCountries = new[] { "US", "GB" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(geoCountry: "US"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_AllowedCountry_NotInList_ConditionsNotMet()
    {
        // AllowedCountries=["US","GB"], context.GeoCountry="DE" → not in list → returns false
        var loc = JsonSerializer.Serialize(new { AllowedCountries = new[] { "US", "GB" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(geoCountry: "DE"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IpInCidrRange_ConditionsMet()
    {
        // AllowedIpRanges=["10.0.0.0/8"], context.IpAddress="10.1.2.3" → in range → passes
        var loc = JsonSerializer.Serialize(new { AllowedIpRanges = new[] { "10.0.0.0/8" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(ipAddress: "10.1.2.3"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_IpNotInCidrRange_ConditionsNotMet()
    {
        // AllowedIpRanges=["10.0.0.0/8"], context.IpAddress="192.168.1.1" → not in range
        var loc = JsonSerializer.Serialize(new { AllowedIpRanges = new[] { "10.0.0.0/8" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(ipAddress: "192.168.1.1"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_IpExactMatch_ConditionsMet()
    {
        // AllowedIpRanges=["192.168.1.1"], exact match
        var loc = JsonSerializer.Serialize(new { AllowedIpRanges = new[] { "192.168.1.1" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: loc));

        var result = await _sut.EvaluateAsync(CreateContext(ipAddress: "192.168.1.1"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_InvalidLocationJson_TreatedAsTrue()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, locationConditions: "bad-json"));

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeFalse();
    }

    // ── Device conditions ─────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_BlockedUserAgent_ConditionsNotMet_DenySkipped()
    {
        // BlockedUserAgents=["BadBot"], context.UserAgent="BadBot/1.0" → contains match → returns false
        var dev = JsonSerializer.Serialize(new { BlockedUserAgents = new[] { "BadBot", "Scraper" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, deviceConditions: dev));

        var result = await _sut.EvaluateAsync(CreateContext(userAgent: "BadBot/1.0"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NonBlockedUserAgent_ConditionsMet_DenyApplied()
    {
        var dev = JsonSerializer.Serialize(new { BlockedUserAgents = new[] { "BadBot" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, deviceConditions: dev));

        var result = await _sut.EvaluateAsync(CreateContext(userAgent: "Chrome/120.0"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_AllowedFingerprint_Match_ConditionsMet_DenyApplied()
    {
        // Fingerprint matches allowed list → conditions met → deny applied
        var dev = JsonSerializer.Serialize(new { AllowedFingerprints = new[] { "fp-123" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, deviceConditions: dev));

        var result = await _sut.EvaluateAsync(CreateContext(deviceFingerprint: "fp-123"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_AllowedFingerprint_NoMatch_ConditionsNotMet()
    {
        // Fingerprint doesn't match → returns false → deny skipped
        var dev = JsonSerializer.Serialize(new { AllowedFingerprints = new[] { "fp-123" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, deviceConditions: dev));

        var result = await _sut.EvaluateAsync(CreateContext(deviceFingerprint: "fp-999"));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_AllowedFingerprint_NullFingerprint_ConditionsNotMet()
    {
        // No fingerprint in context → returns false
        var dev = JsonSerializer.Serialize(new { AllowedFingerprints = new[] { "fp-123" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, deviceConditions: dev));

        var result = await _sut.EvaluateAsync(CreateContext(deviceFingerprint: null));
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_InvalidDeviceJson_TreatedAsTrue()
    {
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, deviceConditions: "{invalid}"));

        var result = await _sut.EvaluateAsync(CreateContext());
        result.IsAllowed.Should().BeFalse();
    }

    // ── Custom conditions ─────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_CustomConditions_Match_DenyApplied()
    {
        var custom = JsonSerializer.Serialize(new Dictionary<string, string> { { "tier", "free" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, customConditions: custom));

        var ctx = CreateContext(customAttributes: new Dictionary<string, string> { { "tier", "free" } });
        var result = await _sut.EvaluateAsync(ctx);
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_CustomConditions_ValueMismatch_DenySkipped()
    {
        var custom = JsonSerializer.Serialize(new Dictionary<string, string> { { "tier", "free" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, customConditions: custom));

        var ctx = CreateContext(customAttributes: new Dictionary<string, string> { { "tier", "premium" } });
        var result = await _sut.EvaluateAsync(ctx);
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_CustomConditions_KeyMissing_DenySkipped()
    {
        var custom = JsonSerializer.Serialize(new Dictionary<string, string> { { "tier", "free" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, customConditions: custom));

        var ctx = CreateContext(customAttributes: new Dictionary<string, string> { { "role", "admin" } });
        var result = await _sut.EvaluateAsync(ctx);
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_CustomConditions_NullAttributes_TreatedAsTrue()
    {
        // context.CustomAttributes == null → EvaluateCustomConditions returns true → deny applied
        var custom = JsonSerializer.Serialize(new Dictionary<string, string> { { "tier", "free" } });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, customConditions: custom));

        var ctx = CreateContext(customAttributes: null);
        var result = await _sut.EvaluateAsync(ctx);
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_CustomConditions_MultipleKeys_AllMustMatch()
    {
        var custom = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "tier", "free" }, { "region", "eu" }
        });
        SetupPolicies(CreatePolicy(action: PolicyAction.Deny, customConditions: custom));

        // Only one key matches → returns false → deny skipped
        var ctx = CreateContext(customAttributes: new Dictionary<string, string>
        {
            { "tier", "free" }, { "region", "us" }
        });
        var result = await _sut.EvaluateAsync(ctx);
        result.IsAllowed.Should().BeTrue();
    }

    // ── Policy details / metadata ─────────────────────────────

    [Fact]
    public async Task EvaluateAsync_ReturnsEvaluationDetails_ForAllApplicablePolicies()
    {
        var p1 = CreatePolicy(action: PolicyAction.Allow, priority: 100);
        var p2 = CreatePolicy(action: PolicyAction.Allow, priority: 50);
        SetupPolicies(p1, p2);

        var result = await _sut.EvaluateAsync(CreateContext());

        result.Details.Should().HaveCount(2);
        result.Details![0].PolicyId.Should().Be(p1.Id);
        result.Details[0].Effect.Should().Be(PolicyAction.Allow);
        result.Details[0].ConditionsMet.Should().BeTrue();
        result.Details[1].PolicyId.Should().Be(p2.Id);
    }

    [Fact]
    public async Task EvaluateAsync_DenyDetail_IncludesConditionsMet()
    {
        var policy = CreatePolicy(action: PolicyAction.Deny);
        SetupPolicies(policy);

        var result = await _sut.EvaluateAsync(CreateContext());

        result.Details.Should().ContainSingle();
        result.Details![0].ConditionsMet.Should().BeTrue();
        result.Details[0].Effect.Should().Be(PolicyAction.Deny);
    }

    // ── Combined conditions ───────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_MultipleConditionTypes_AllMustPass()
    {
        // Environment + Location conditions both present → both must pass
        var env = JsonSerializer.Serialize(new { RequireMfa = true });
        var loc = JsonSerializer.Serialize(new { AllowedCountries = new[] { "US" } });
        SetupPolicies(CreatePolicy(
            action: PolicyAction.Deny,
            environmentConditions: env,
            locationConditions: loc));

        // MFA verified + US country → both pass → deny applied
        var result = await _sut.EvaluateAsync(CreateContext(isMfaVerified: true, geoCountry: "US"));
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_MultipleConditions_OneFailsShortCircuits()
    {
        // Environment fails → rest not checked → conditionsMet=false → deny skipped
        var env = JsonSerializer.Serialize(new { RequireMfa = true });
        var loc = JsonSerializer.Serialize(new { AllowedCountries = new[] { "US" } });
        SetupPolicies(CreatePolicy(
            action: PolicyAction.Deny,
            environmentConditions: env,
            locationConditions: loc));

        // MFA NOT verified → env conditions fail → deny skipped → allowed
        var result = await _sut.EvaluateAsync(CreateContext(isMfaVerified: false, geoCountry: "US"));
        result.IsAllowed.Should().BeTrue();
    }
}
