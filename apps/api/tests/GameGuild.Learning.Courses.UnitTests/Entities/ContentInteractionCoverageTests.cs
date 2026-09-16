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
        untouched.CompletionPercentage.Should().Be(0m);
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
        var interaction = new ContentInteraction { IsCompleted = true, ProgressPercentage = 10m };

        interaction.UpdateProgress(20m);

        interaction.Status.Should().Be(ProgressStatus.Completed);
        interaction.ProgressPercentage.Should().Be(100m);
        interaction.LastAccessedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(60, 60, 0, null, 20)]
    [InlineData(30, 100, 0, null, 10)]
    [InlineData(10, 100, 0, null, 0)]
    [InlineData(null, 100, 0, null, 0)]
    [InlineData(60, null, 0, null, 0)]
    [InlineData(null, null, 2, 100, 10)]
    [InlineData(null, null, 1, 150, 20)]
    public void CalculateEngagementScore_AppliesTimeAndAttemptContributions(
        int? timeSpent,
        int? estimatedMinutes,
        int attempts,
        int? bestScore,
        int expected)
    {
        var interaction = new ContentInteraction
        {
            ProgressPercentage = 0,
            TimeSpentMinutes = timeSpent,
            Content = estimatedMinutes.HasValue
                ? new ProgramContent { EstimatedMinutes = estimatedMinutes }
                : null!,
            AttemptCount = attempts,
            BestScore = bestScore
        };

        interaction.CalculateEngagementScore().Should().Be((decimal)expected);
    }

    [Fact]
    public void CalculateEngagementScore_CapsCombinedSignalsAtOneHundred()
    {
        var interaction = new ContentInteraction
        {
            ProgressPercentage = 200,
            IsCompleted = true,
            TimeSpentMinutes = 60,
            Content = new ProgramContent { EstimatedMinutes = 60 },
            AttemptCount = 1,
            BestScore = 150
        };

        interaction.CalculateEngagementScore().Should().Be(100m);
    }
}
