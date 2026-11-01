using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Configuration;

/// <summary>
///     Unit tests for the opt-in raw-delivery retention policy (issue #408: capture raw
///     payload and all headers): disabled by default, bounded retention window.
/// </summary>
public class WebhookRetentionPolicyTests
{
    [Fact]
    public void Defaults_Are_Off_Thirty_Days_And_Pii_Redacted()
    {
        var policy = new WebhookRetentionPolicy();

        policy.Enabled.Should().BeFalse("retention must be opt-in: nothing changes in production until explicitly enabled");
        policy.RetentionDays.Should().Be(30);
        policy.RedactPii.Should().BeTrue();
    }

    [Fact]
    public void WebhookSettings_Exposes_The_Retention_Policy()
    {
        var settings = new WebhookSettings();

        settings.Retention.Should().NotBeNull();
        settings.Retention.Enabled.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(366, false)]
    [InlineData(1000, false)]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(365, true)]
    public void BillingConfiguration_Validates_The_Retention_Window_Bounds(int retentionDays, bool expectedValid)
    {
        var configuration = new BillingConfiguration();
        configuration.Webhook.Retention.RetentionDays = retentionDays;

        var results = configuration.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(configuration)).ToList();

        var retentionErrors = results
            .Where(result => result.MemberNames.Contains("Webhook.Retention.RetentionDays"))
            .ToList();

        if (expectedValid)
        {
            retentionErrors.Should().BeEmpty();
        }
        else
        {
            retentionErrors.Should().HaveCount(1);
            retentionErrors[0].ErrorMessage.Should().Contain("between 1 and 365");
        }
    }
}
