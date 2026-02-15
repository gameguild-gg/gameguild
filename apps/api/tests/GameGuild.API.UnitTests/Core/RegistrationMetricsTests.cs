using FluentAssertions;

namespace GameGuild.API.UnitTests.Core;

public class RegistrationMetricsTests
{
    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        var metrics = new RegistrationMetrics();

        metrics.TotalHandlersRegistered.Should().Be(0);
        metrics.TotalValidatorsRegistered.Should().Be(0);
        metrics.RegistrationDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var metrics = new RegistrationMetrics
        {
            TotalHandlersRegistered = 42,
            TotalValidatorsRegistered = 15,
            RegistrationDuration = TimeSpan.FromMilliseconds(250)
        };

        metrics.TotalHandlersRegistered.Should().Be(42);
        metrics.TotalValidatorsRegistered.Should().Be(15);
        metrics.RegistrationDuration.Should().Be(TimeSpan.FromMilliseconds(250));
    }
}
