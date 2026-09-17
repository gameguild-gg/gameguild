using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ContentInteractionCoverageTests
{
    [Fact]
    public void ComputedState_ReflectsTenantTimelineAndCompletion()
    {
        var startedAt = SystemClock.UtcNow.AddDays(-4);
        var completedAt = startedAt.AddDays(2);
        var interaction = new ContentInteraction
        {
            TenantId = null,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            LastAccessedAt = SystemClock.UtcNow.AddDays(-3),
            IsCompleted = true
        };

        interaction.IsGlobal.Should().BeTrue();
        interaction.IsStarted.Should().BeTrue();
        interaction.IsInProgress.Should().BeFalse();
        interaction.DaysSinceLastAccess.Should().Be(3);
        interaction.EngagementDuration.Should().Be(TimeSpan.FromDays(2));

        interaction.TenantId = Guid.NewGuid();
        interaction.IsGlobal.Should().BeFalse();

        var untouched = new ContentInteraction();
        untouched.IsStarted.Should().BeFalse();
        untouched.IsInProgress.Should().BeFalse();
        untouched.EngagementDuration.Should().BeNull();
        untouched.ProgressPercentage = null;
        untouched.CompletionPercentage.Should().Be(PercentValue.Zero);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTimeSpent_WhenMinutesAreNotPositive_RejectsInput(int minutes)
    {
        var interaction = new ContentInteraction();

        var action = () => interaction.AddTimeSpent(minutes);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(minutes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTimeSpentSeconds_WhenSecondsAreNotPositive_RejectsInput(int seconds)
    {
        var interaction = new ContentInteraction();

        var action = () => interaction.AddTimeSpentSeconds(seconds);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(seconds));
    }

    [Fact]
    public void UpdateProgress_AfterCompletion_PreservesCompletedState()
    {
        var interaction = new ContentInteraction { IsCompleted = true, ProgressPercentage = Percent(10) };

        interaction.UpdateProgress(Percent(20));

        interaction.Status.Should().Be(ProgressStatus.Completed);
        interaction.ProgressPercentage.Should().Be(PercentValue.Hundred);
        interaction.LastAccessedAt.Should().NotBeNull();
    }

}
