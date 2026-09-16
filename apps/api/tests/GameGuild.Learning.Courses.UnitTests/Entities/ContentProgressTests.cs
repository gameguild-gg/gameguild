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
        progress.ProgressPercentage.Should().Be(100m);
        progress.CompletedAt.Should().NotBeNull();
        progress.Score.Should().BeNull();
        progress.MaxScore.Should().BeNull();
        progress.FirstAccessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsCompleted_WithScores_PersistsBothValues()
    {
        var progress = new ContentProgress();

        progress.MarkAsCompleted(84m, 100m);

        progress.Score.Should().Be(84m);
        progress.MaxScore.Should().Be(100m);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(25, 25)]
    [InlineData(150, 100)]
    public void UpdateProgress_ClampsPercentageToSupportedRange(decimal requested, decimal expected)
    {
        var progress = new ContentProgress();

        progress.UpdateProgress(requested);

        progress.ProgressPercentage.Should().Be(expected);
    }

    [Fact]
    public void UpdateProgress_WhenPositive_MarksNotStartedContentInProgress()
    {
        var progress = new ContentProgress();

        progress.UpdateProgress(50m);

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.InProgress);
        progress.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateProgress_WhenComplete_CompletesOnlyOnce()
    {
        var progress = new ContentProgress();

        progress.UpdateProgress(100m);
        var completedAt = progress.CompletedAt;
        progress.UpdateProgress(100m);

        progress.CompletionStatus.Should().Be(ContentCompletionStatus.Completed);
        progress.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void UpdateProgress_WhenStatusAlreadyAdvanced_DoesNotRegressIt()
    {
        var progress = new ContentProgress { CompletionStatus = ContentCompletionStatus.RequiresReview };

        progress.UpdateProgress(50m);

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
