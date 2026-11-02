using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Configuration;

public class WebhookRetryPolicyTests
{
    [Fact]
    public void CalculateDelaySeconds_Should_Return_Zero_For_NonPositive_Attempt()
    {
        var policy = new WebhookRetryPolicy();

        policy.CalculateDelaySeconds(0).Should().Be(0);
    }

    [Fact]
    public void CalculateDelaySeconds_Should_Respect_MaxDelay()
    {
        var policy = new WebhookRetryPolicy
        {
            InitialDelaySeconds = 100,
            MaxDelaySeconds = 150,
            BackoffMultiplier = 3,
            AddJitter = false
        };

        policy.CalculateDelaySeconds(3).Should().Be(150);
    }

    [Fact]
    public void CalculateDelaySeconds_Should_Skip_Jitter_When_Disabled()
    {
        var policy = new WebhookRetryPolicy
        {
            InitialDelaySeconds = 5,
            BackoffMultiplier = 2,
            AddJitter = false
        };

        policy.CalculateDelaySeconds(2).Should().Be(10);
    }

    [Fact]
    public void RetryWorkerDefaults_Should_Match_The_Issue_408_Contract()
    {
        var settings = new WebhookRetryWorkerSettings();

        settings.PollIntervalSeconds.Should().Be(30, "the worker polls every 30 seconds");
        settings.BatchSize.Should().Be(50, "each cycle processes up to 50 webhooks (issue #408)");
        settings.MaxRetryAttempts.Should().Be(5);
        settings.RetryScheduleMinutes.Should().Equal(1, 5, 15, 60, 360);
        settings.TerminalRetryScheduleMinutes.Should().Be(1440);
    }
}
