using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ContentProgressTests
{
    [Fact]
    public void MarkAsAccessed_FirstAndSubsequentAccessesPreserveTheFirstTimestamp()
    {
        var progress = new ContentProgress();

        progress.MarkAsAccessed();
        var firstAccess = progress.FirstAccessedAt;
        progress.MarkAsAccessed();

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.InProgress);
        progress.FirstAccessedAt.Should().Be(firstAccess);
        progress.LastAccessedAt.Should().NotBeNull();
        progress.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void MarkAsAccessed_WhenStatusWasAlreadySet_DoesNotReplaceIt()
    {
        var progress = new ContentProgress { CompletionStatus = ContentCompletionStatus.Skipped };

        progress.MarkAsAccessed();

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.Skipped);
    }

    [Fact]
    public void MarkAsCompleted_WithoutScores_CompletesAndAccessesContent()
    {
        var progress = new ContentProgress();

        progress.MarkAsCompleted();

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.Completed);
        progress.ProgressPercentage.Should().Be(PercentValue.Hundred);
        progress.CompletedAt.Should().NotBeNull();
        progress.Score.Should().BeNull();
        progress.MaxScore.Should().BeNull();
        progress.FirstAccessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsCompleted_WithScores_PersistsBothValues()
    {
        var progress = new ContentProgress();

        progress.MarkAsCompleted(Score(84), Score(100));

        progress.Score.Should().Be(Score(84));
        progress.MaxScore.Should().Be(Score(100));
    }

    [Theory]
    [InlineData(25, 25)]
    public void UpdateProgress_PersistsSupportedPercentage(decimal requested, decimal expected)
    {
        var progress = new ContentProgress();

        progress.UpdateProgress(Percent(requested));

        progress.ProgressPercentage.Should().Be(Percent(expected));
    }

    [Fact]
    public void UpdateProgress_WhenPositive_MarksNotStartedContentInProgress()
    {
        var progress = new ContentProgress();

        progress.UpdateProgress(Percent(50));

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.InProgress);
        progress.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateProgress_WhenComplete_CompletesOnlyOnce()
    {
        var progress = new ContentProgress();

        progress.UpdateProgress(Percent(100));
        var completedAt = progress.CompletedAt;
        progress.UpdateProgress(Percent(100));

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.Completed);
        progress.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void UpdateProgress_WhenStatusAlreadyAdvanced_DoesNotRegressIt()
    {
        var progress = new ContentProgress { CompletionStatus = ContentCompletionStatus.RequiresReview };

        progress.UpdateProgress(Percent(50));

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.RequiresReview);
    }

    [Fact]
    public void TimeAndAttemptTracking_AccumulatesAndMarksAccess()
    {
        var progress = new ContentProgress { TimeSpentSeconds = 10, Attempts = 1 };

        progress.AddTimeSpent(15);
        progress.IncrementAttempts();

        progress.TimeSpentSeconds.Should().Be(25);
        progress.Attempts.Should().Be(2);
        progress.FirstAccessedAt.Should().NotBeNull();
        progress.LastAccessedAt.Should().NotBeNull();
    }
}
