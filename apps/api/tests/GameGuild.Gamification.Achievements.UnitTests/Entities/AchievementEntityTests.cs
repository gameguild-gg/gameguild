using FluentAssertions;
using Xunit;

namespace GameGuild.Gamification.Achievements.Tests;

/// <summary>
/// Unit tests for Achievement and UserAchievement entity domain logic.
/// </summary>
public class AchievementEntityTests
{
    [Fact]
    public void Create_ShouldInitializeWithCorrectDefaults()
    {
        var achievement = Achievement.Create("First Post", "social", "badge", 10, "Made your first post");

        achievement.Name.Should().Be("First Post");
        achievement.Category.Should().Be("social");
        achievement.Type.Should().Be("badge");
        achievement.Points.Should().Be(10);
        achievement.Description.Should().Be("Made your first post");
        achievement.IsActive.Should().BeTrue();
        achievement.IsRepeatable.Should().BeFalse();
        achievement.IsSecret.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var achievement = Achievement.Create("Test", "test");
        achievement.Deactivate();

        achievement.Activate();

        achievement.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var achievement = Achievement.Create("Test", "test");

        achievement.Deactivate();

        achievement.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdatePoints_ShouldClampToZero()
    {
        var achievement = Achievement.Create("Test", "test", points: 50);

        achievement.UpdatePoints(-10);

        achievement.Points.Should().Be(0); // Clamped
    }

    [Fact]
    public void UpdatePoints_ShouldUpdatePositiveValues()
    {
        var achievement = Achievement.Create("Test", "test", points: 0);

        achievement.UpdatePoints(100);

        achievement.Points.Should().Be(100);
    }
}

/// <summary>
/// Unit tests for UserAchievement entity domain logic.
/// </summary>
public class UserAchievementEntityTests
{
    [Fact]
    public void Create_ShouldSetCompletedAndPoints()
    {
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var ua = UserAchievement.Create(userId, achievementId, 25, "{\"trigger\": \"post\"}");

        ua.UserId.Should().Be(userId);
        ua.AchievementId.Should().Be(achievementId);
        ua.PointsEarned.Should().Be(25);
        ua.IsCompleted.Should().BeTrue();
        ua.IsNotified.Should().BeFalse();
        ua.EarnCount.Should().Be(1);
        ua.Context.Should().Be("{\"trigger\": \"post\"}");
    }

    [Fact]
    public void MarkAsNotified_ShouldSetNotifiedTrue()
    {
        var ua = UserAchievement.Create(Guid.NewGuid(), Guid.NewGuid(), 10);

        ua.MarkAsNotified();

        ua.IsNotified.Should().BeTrue();
    }

    [Fact]
    public void IncrementProgress_ShouldClampToMaxProgress()
    {
        var ua = UserAchievement.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        ua.MaxProgress = 5;
        ua.Progress = 0;

        ua.IncrementProgress(10); // Way over max

        ua.Progress.Should().Be(5); // Clamped to max
        ua.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void IncrementProgress_ShouldAutoCompleteWhenReachingTarget()
    {
        var ua = UserAchievement.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        ua.MaxProgress = 3;
        ua.Progress = 0;
        ua.IsCompleted = false;

        ua.IncrementProgress(3);

        ua.Progress.Should().Be(3);
        ua.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ProgressPercentage_ShouldCalculateCorrectly()
    {
        var ua = UserAchievement.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        ua.MaxProgress = 10;
        ua.Progress = 7;

        ua.ProgressPercentage.Should().Be(70.0);
    }

    [Fact]
    public void ProgressPercentage_WhenZeroMax_ShouldReturnZero()
    {
        var ua = UserAchievement.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        ua.MaxProgress = 0;

        ua.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void IncrementEarnCount_ShouldIncrement()
    {
        var ua = UserAchievement.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        ua.IncrementEarnCount();
        ua.IncrementEarnCount();

        ua.EarnCount.Should().Be(3); // Started at 1 + 2 increments
    }
}
