using GameGuild.Configuration.ApplicationLayer;
using GameGuild.Identity.Authentication;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Progressive sign-in delay schedule (issue #1084): the exponential backoff must
///     produce 1s, 2s, 4s, 8s on the default settings, saturate at the configured
///     maximum, and stay at zero for the first attempt.
/// </summary>
public class ProgressiveDelayScheduleTests
{
    [Fact]
    public void NoRecentFailures_ProducesNoDelay()
    {
        var delay = LocalAuthService.ComputeProgressiveDelay(0, new AuthenticationSecurityOptions());

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    [InlineData(5, 8)]
    [InlineData(40, 8)]
    public void DefaultSettings_FollowOneTwoFourEightSchedule(int recentFailures, int expectedSeconds)
    {
        var delay = LocalAuthService.ComputeProgressiveDelay(recentFailures, new AuthenticationSecurityOptions());

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void CustomBase_DoublesPerFailure()
    {
        var options = new AuthenticationSecurityOptions
        {
            ProgressiveDelayBaseSeconds = 2,
            ProgressiveDelayMaxSeconds = 16
        };

        Assert.Equal(TimeSpan.FromSeconds(2), LocalAuthService.ComputeProgressiveDelay(1, options));
        Assert.Equal(TimeSpan.FromSeconds(4), LocalAuthService.ComputeProgressiveDelay(2, options));
        Assert.Equal(TimeSpan.FromSeconds(8), LocalAuthService.ComputeProgressiveDelay(3, options));
        Assert.Equal(TimeSpan.FromSeconds(16), LocalAuthService.ComputeProgressiveDelay(4, options));
        Assert.Equal(TimeSpan.FromSeconds(16), LocalAuthService.ComputeProgressiveDelay(9, options));
    }

    [Fact]
    public void ZeroBase_DisablesTheDelay()
    {
        var options = new AuthenticationSecurityOptions { ProgressiveDelayBaseSeconds = 0 };

        Assert.Equal(TimeSpan.Zero, LocalAuthService.ComputeProgressiveDelay(3, options));
    }
}
