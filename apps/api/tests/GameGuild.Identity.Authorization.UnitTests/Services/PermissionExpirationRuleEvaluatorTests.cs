using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class PermissionExpirationRuleEvaluatorTests
{
    private static readonly DateTime Now = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    private readonly PermissionExpirationOptions _options = new();
    private PermissionExpirationRuleEvaluator CreateSut() => new(Options.Create(_options));

    // ── Describe / composition ─────────────────────────────────

    [Fact]
    public void Describe_NoRulesConfigured_ReturnsEmptyRuleWithoutKeys()
    {
        var effective = CreateSut().Describe(new[] { "courses:read" });

        effective.Rule.IsEmpty.Should().BeTrue();
        effective.MatchedRuleKeys.Should().BeEmpty();
    }

    [Fact]
    public void Describe_ExactKeyMatches_AndWildcardComposes_StrictestWins()
    {
        _options.GlobalRule = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(90) };
        _options.Rules["billing:*"] = new PermissionExpirationRule
        {
            RequireExpiration = true,
            MaxDuration = TimeSpan.FromDays(60)
        };
        _options.Rules["billing:refunds:approve"] = new PermissionExpirationRule
        {
            MaxDuration = TimeSpan.FromDays(30),
            MinDuration = TimeSpan.FromHours(1)
        };

        var effective = CreateSut().Describe(new[] { "courses:read", "billing:refunds:approve" });

        effective.Rule.RequireExpiration.Should().BeTrue();
        effective.Rule.MinDuration.Should().Be(TimeSpan.FromHours(1));
        effective.Rule.MaxDuration.Should().Be(TimeSpan.FromDays(30));
        effective.MatchedRuleKeys.Should().BeEquivalentTo(new[] { "(global)", "billing:*", "billing:refunds:approve" });
    }

    [Fact]
    public void Describe_WildcardDoesNotMatchUnrelatedPrefix()
    {
        _options.Rules["billing:*"] = new PermissionExpirationRule { RequireExpiration = true };

        var effective = CreateSut().Describe(new[] { "courses:read" });

        effective.Rule.IsEmpty.Should().BeTrue();
        effective.MatchedRuleKeys.Should().BeEmpty();
    }

    [Fact]
    public void MergeWith_FalseDoesNotOverrideTrue()
    {
        var left = new PermissionExpirationRule { RequireExpiration = false };
        var right = new PermissionExpirationRule { RequireExpiration = true };

        left.MergeWith(right);

        left.RequireExpiration.Should().BeTrue();
    }

    // ── ValidateSetExpiration ──────────────────────────────────

    [Fact]
    public void ValidateSetExpiration_NullExpiration_WhenRequired_Violates()
    {
        _options.Rules["economy:*"] = new PermissionExpirationRule { RequireExpiration = true };

        var violations = CreateSut().ValidateSetExpiration(new[] { "economy:payouts:approve" }, null, Now);

        violations.Should().ContainSingle(v => v.Constraint == "RequireExpiration");
        violations.Single().MatchedRuleKeys.Should().Contain("economy:*");
    }

    [Fact]
    public void ValidateSetExpiration_NullExpiration_WhenNotRequired_IsAllowed()
    {
        _options.Rules["economy:*"] = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(30) };

        var violations = CreateSut().ValidateSetExpiration(new[] { "economy:payouts:approve" }, null, Now);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSetExpiration_BeyondMaxDuration_Violates()
    {
        _options.GlobalRule = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(30) };

        var violations = CreateSut().ValidateSetExpiration(
            new[] { "courses:read" }, Now.AddDays(31), Now);

        violations.Should().ContainSingle(v => v.Constraint == "MaxDuration");
    }

    [Fact]
    public void ValidateSetExpiration_BelowMinDuration_Violates()
    {
        _options.GlobalRule = new PermissionExpirationRule { MinDuration = TimeSpan.FromHours(1) };

        var violations = CreateSut().ValidateSetExpiration(
            new[] { "courses:read" }, Now.AddMinutes(30), Now);

        violations.Should().ContainSingle(v => v.Constraint == "MinDuration");
    }

    [Fact]
    public void ValidateSetExpiration_WithinBounds_IsAllowed()
    {
        _options.GlobalRule = new PermissionExpirationRule
        {
            MinDuration = TimeSpan.FromHours(1),
            MaxDuration = TimeSpan.FromDays(30)
        };

        var violations = CreateSut().ValidateSetExpiration(
            new[] { "courses:read" }, Now.AddDays(7), Now);

        violations.Should().BeEmpty();
    }

    // ── ValidateExtension ──────────────────────────────────────

    [Fact]
    public void ValidateExtension_SingleExtensionBeyondCap_Violates()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule { MaxExtension = TimeSpan.FromDays(7) };

        var violations = CreateSut().ValidateExtension(
            new[] { "courses:read" }, Now.AddDays(2), TimeSpan.FromDays(14), Now);

        violations.Should().ContainSingle(v => v.Constraint == "MaxExtension");
    }

    [Fact]
    public void ValidateExtension_ResultingExpirationBeyondCap_Violates()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(30) };

        // Current expiration in 20 days; extending by 15 would land 35 days out.
        var violations = CreateSut().ValidateExtension(
            new[] { "courses:read" }, Now.AddDays(20), TimeSpan.FromDays(15), Now);

        violations.Should().ContainSingle(v => v.Constraint == "MaxDuration");
    }

    [Fact]
    public void ValidateExtension_ExpiredBaselineExtendsFromNow()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(30) };

        // Grant already expired a day ago; extending by 10 days lands 10 days out (not 9 or 11).
        var violations = CreateSut().ValidateExtension(
            new[] { "courses:read" }, Now.AddDays(-1), TimeSpan.FromDays(10), Now);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateExtension_CompliantExtension_IsAllowed()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule
        {
            MaxExtension = TimeSpan.FromDays(7),
            MaxDuration = TimeSpan.FromDays(30)
        };

        var violations = CreateSut().ValidateExtension(
            new[] { "courses:read" }, Now.AddDays(10), TimeSpan.FromDays(5), Now);

        violations.Should().BeEmpty();
    }

    // ── Options validation ─────────────────────────────────────

    [Fact]
    public void Validate_RejectsNonPositiveRuleDurations()
    {
        _options.Rules["bad:*"] = new PermissionExpirationRule { MaxDuration = TimeSpan.Zero };

        _options.Validate().Should().Contain("Rules['bad:*'].MaxDuration must be positive.");
    }

    [Fact]
    public void Validate_RejectsMinAboveMax()
    {
        _options.GlobalRule = new PermissionExpirationRule
        {
            MinDuration = TimeSpan.FromDays(10),
            MaxDuration = TimeSpan.FromDays(5)
        };

        _options.Validate().Should().Contain("GlobalRule.MinDuration must not exceed GlobalRule.MaxDuration.");
    }

    [Fact]
    public void Validate_AcceptsWellFormedRules()
    {
        _options.GlobalRule = new PermissionExpirationRule { RequireExpiration = true };
        _options.Rules["ok:*"] = new PermissionExpirationRule
        {
            MinDuration = TimeSpan.FromHours(1),
            MaxDuration = TimeSpan.FromDays(30),
            MaxExtension = TimeSpan.FromDays(7)
        };

        _options.Validate().Should().BeEmpty();
    }
}
