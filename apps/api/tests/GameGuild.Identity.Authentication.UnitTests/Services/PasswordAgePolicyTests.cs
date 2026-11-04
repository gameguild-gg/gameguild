using FluentAssertions;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Password age / rotation policy unit tests (issue #1088): disabled by default,
///     per-tenant maximum overrides the platform baseline, and legacy passwords without
///     a change timestamp are never retro-expired so enabling the policy cannot
///     mass-lock existing accounts.
/// </summary>
public sealed class PasswordAgePolicyTests
{
    private readonly PasswordAgePolicyOptions _options = new();

    [Fact]
    public void Disabled_By_Default()
    {
        _options.Enabled.Should().BeFalse();
        PasswordAgePolicy.EffectiveMaxAgeDays(tenantId: null, _options).Should().BeNull();
    }

    [Fact]
    public void Disabled_Policy_Never_Expires_Even_Ancient_Passwords()
    {
        var user = User.CreateWithPassword("aged@example.test", "aged", "hash");

        PasswordAgePolicy.IsExpired(user, tenantId: null, _options, DateTime.UtcNow.AddDays(3650))
            .Should().BeFalse("the policy is inert until explicitly enabled");
    }

    [Fact]
    public void Password_Older_Than_The_Maximum_Expires()
    {
        _options.Enabled = true;
        _options.MaxAgeDays = 90;
        var user = User.CreateWithPassword("aged@example.test", "aged", "hash");

        var atDay89 = PasswordAgePolicy.IsExpired(user, tenantId: null, _options, DateTime.UtcNow.AddDays(89));
        var atDay91 = PasswordAgePolicy.IsExpired(user, tenantId: null, _options, DateTime.UtcNow.AddDays(91));

        atDay89.Should().BeFalse();
        atDay91.Should().BeTrue("sign-in past the maximum age must route through the forced change flow");
    }

    [Fact]
    public void Tenant_Maximum_Overrides_The_Platform_Baseline()
    {
        _options.Enabled = true;
        _options.MaxAgeDays = 90;
        var tenantId = Guid.NewGuid();
        _options.TenantMaxAgeDays[tenantId.ToString()] = 30;
        var user = User.CreateWithPassword("aged@example.test", "aged", "hash");

        PasswordAgePolicy.EffectiveMaxAgeDays(tenantId, _options).Should().Be(30);
        PasswordAgePolicy.IsExpired(user, tenantId, _options, DateTime.UtcNow.AddDays(40)).Should().BeTrue();
        PasswordAgePolicy.IsExpired(user, tenantId: Guid.NewGuid(), _options, DateTime.UtcNow.AddDays(40))
            .Should().BeFalse("other tenants keep the 90-day platform baseline");
    }

    [Fact]
    public void Legacy_Password_Without_Timestamp_Is_Never_Retro_Expired()
    {
        _options.Enabled = true;
        _options.MaxAgeDays = 90;
        var legacy = User.Create("legacy@example.test", "Legacy owner"); // OAuth-era row: no password ever set.

        PasswordAgePolicy.IsExpired(legacy, tenantId: null, _options, DateTime.UtcNow.AddDays(3650))
            .Should().BeFalse("rotation is enforced from the next password set onward");
    }

    [Fact]
    public void Misconfigured_Zero_Maximum_Is_Treated_As_Disabled()
    {
        _options.Enabled = true;
        _options.MaxAgeDays = 0;
        var user = User.CreateWithPassword("aged@example.test", "aged", "hash");

        PasswordAgePolicy.EffectiveMaxAgeDays(tenantId: null, _options).Should().BeNull();
        PasswordAgePolicy.IsExpired(user, tenantId: null, _options, DateTime.UtcNow.AddDays(3650))
            .Should().BeFalse();
    }
}
