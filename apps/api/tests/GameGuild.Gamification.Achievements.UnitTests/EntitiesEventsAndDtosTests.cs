using FluentAssertions;
using Xunit;

namespace GameGuild.Gamification.Achievements.Tests;

/// <summary>
/// Tests for AchievementLevel entity.
/// </summary>
public class AchievementLevelTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var achievementId = Guid.NewGuid();
        var level = AchievementLevel.Create(achievementId, 2, "Silver", 50, 100);

        level.Id.Should().NotBeEmpty();
        level.AchievementId.Should().Be(achievementId);
        level.Level.Should().Be(2);
        level.Name.Should().Be("Silver");
        level.RequiredProgress.Should().Be(50);
        level.Points.Should().Be(100);
    }
}

/// <summary>
/// Tests for AchievementProgress entity.
/// </summary>
public class AchievementProgressTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var progress = AchievementProgress.Create(userId, achievementId, 10);

        progress.Id.Should().NotBeEmpty();
        progress.UserId.Should().Be(userId);
        progress.AchievementId.Should().Be(achievementId);
        progress.TargetProgress.Should().Be(10);
        progress.CurrentProgress.Should().Be(0);
        progress.IsCompleted.Should().BeFalse();
        progress.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var progress = AchievementProgress.Create(Guid.NewGuid(), Guid.NewGuid(), 5, tenantId);
        progress.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void IncrementProgress_ShouldIncrement()
    {
        var progress = AchievementProgress.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        progress.IncrementProgress(3);

        progress.CurrentProgress.Should().Be(3);
        progress.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void IncrementProgress_ShouldClampToTarget()
    {
        var progress = AchievementProgress.Create(Guid.NewGuid(), Guid.NewGuid(), 5);
        progress.IncrementProgress(10);

        progress.CurrentProgress.Should().Be(5);
        progress.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void IncrementProgress_AtTarget_ShouldAutoComplete()
    {
        var progress = AchievementProgress.Create(Guid.NewGuid(), Guid.NewGuid(), 3);
        progress.IncrementProgress(3);

        progress.IsCompleted.Should().BeTrue();
        progress.CurrentProgress.Should().Be(3);
    }

    [Fact]
    public void ProgressPercentage_ShouldCalculateCorrectly()
    {
        var progress = AchievementProgress.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        progress.IncrementProgress(5);

        progress.ProgressPercentage.Should().Be(50.0);
    }

    [Fact]
    public void ProgressPercentage_ZeroTarget_ShouldReturnZero()
    {
        var progress = AchievementProgress.Create(Guid.NewGuid(), Guid.NewGuid(), 0);
        progress.ProgressPercentage.Should().Be(0);
    }
}

/// <summary>
/// Tests for achievement events.
/// </summary>
public class AchievementEventsTests
{
    [Fact]
    public void AchievementEarnedEvent_ShouldStoreProperties()
    {
        var evt = new AchievementEarnedEvent(Guid.NewGuid(), Guid.NewGuid(), "First Post", 10, 1, DateTime.UtcNow, Guid.NewGuid());

        evt.AchievementName.Should().Be("First Post");
        evt.PointsEarned.Should().Be(10);
        evt.Level.Should().Be(1);
        evt.TenantId.Should().NotBeNull();
    }

    [Fact]
    public void AchievementProgressUpdatedEvent_ShouldStoreProperties()
    {
        var evt = new AchievementProgressUpdatedEvent(Guid.NewGuid(), Guid.NewGuid(), 5, 10, false);

        evt.CurrentProgress.Should().Be(5);
        evt.TargetProgress.Should().Be(10);
        evt.IsCompleted.Should().BeFalse();
        evt.TenantId.Should().BeNull();
    }

    [Fact]
    public void AchievementLevelUpEvent_ShouldStoreProperties()
    {
        var evt = new AchievementLevelUpEvent(Guid.NewGuid(), Guid.NewGuid(), "Expert", 1, 2, "Silver", 50, Guid.NewGuid());

        evt.PreviousLevel.Should().Be(1);
        evt.NewLevel.Should().Be(2);
        evt.LevelName.Should().Be("Silver");
        evt.BonusPoints.Should().Be(50);
    }

    [Fact]
    public void AchievementPointsEarnedEvent_ShouldStoreProperties()
    {
        var evt = new AchievementPointsEarnedEvent(Guid.NewGuid(), 25, 100, "quiz_completion");

        evt.PointsEarned.Should().Be(25);
        evt.TotalPoints.Should().Be(100);
        evt.Source.Should().Be("quiz_completion");
    }

    [Fact]
    public void AchievementUnlockedEvent_ShouldStoreProperties()
    {
        var evt = new AchievementUnlockedEvent(Guid.NewGuid(), Guid.NewGuid(), "Unlock", "Prereqs met");
        evt.Reason.Should().Be("Prereqs met");
    }

    [Fact]
    public void AchievementCreatedEvent_ShouldStoreProperties()
    {
        var evt = new AchievementCreatedEvent(Guid.NewGuid(), "NewAch", "learning", 50, Guid.NewGuid());

        evt.Name.Should().Be("NewAch");
        evt.Category.Should().Be("learning");
        evt.Points.Should().Be(50);
    }

    [Fact]
    public void AchievementModifiedEvent_ShouldStoreProperties()
    {
        var evt = new AchievementModifiedEvent(Guid.NewGuid(), "Modified", "Changed points", Guid.NewGuid());
        evt.ChangeDescription.Should().Be("Changed points");
    }

    [Fact]
    public void MilestoneAchievementEvent_ShouldStoreProperties()
    {
        var evt = new MilestoneAchievementEvent(Guid.NewGuid(), Guid.NewGuid(), "First100", 42, "42nd user to complete");

        evt.MilestoneRank.Should().Be(42);
        evt.MilestoneDescription.Should().Be("42nd user to complete");
    }
}

/// <summary>
/// Tests for achievement DTOs.
/// </summary>
public class AchievementDtoTests
{
    [Fact]
    public void AchievementDto_ShouldStoreProperties()
    {
        var dto = new AchievementDto
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Category = "cat",
            Type = "badge",
            Points = 10,
            IsActive = true,
            IsSecret = false,
            IsRepeatable = true,
            DisplayOrder = 1,
            Levels = new List<AchievementLevelDto>()
        };

        dto.Name.Should().Be("Test");
        dto.Points.Should().Be(10);
        dto.IsRepeatable.Should().BeTrue();
        dto.Levels.Should().BeEmpty();
    }

    [Fact]
    public void AchievementLevelDto_ShouldStoreProperties()
    {
        var dto = new AchievementLevelDto
        {
            Id = Guid.NewGuid(),
            Level = 3,
            Name = "Gold",
            RequiredProgress = 100,
            PointsAwarded = 500,
            IconUrl = "/gold.png"
        };

        dto.Level.Should().Be(3);
        dto.Name.Should().Be("Gold");
        dto.PointsAwarded.Should().Be(500);
    }

    [Fact]
    public void UserAchievementDto_ShouldStoreProperties()
    {
        var dto = new UserAchievementDto
        {
            Id = Guid.NewGuid(),
            AchievementId = Guid.NewGuid(),
            AchievementName = "First Comment",
            Category = "social",
            EarnedAt = DateTime.UtcNow,
            Level = 1,
            Progress = 5,
            MaxProgress = 10,
            ProgressPercentage = 50.0m,
            IsCompleted = false,
            PointsEarned = 25
        };

        dto.AchievementName.Should().Be("First Comment");
        dto.ProgressPercentage.Should().Be(50.0m);
    }

    [Fact]
    public void CreateAchievementRequest_ShouldStoreProperties()
    {
        var req = new CreateAchievementRequest
        {
            Name = "New Achievement",
            Description = "Desc",
            Category = "cat",
            Type = "badge",
            Points = 50,
            IsSecret = true,
            IsRepeatable = false,
            DisplayOrder = 5
        };

        req.Name.Should().Be("New Achievement");
        req.Points.Should().Be(50);
        req.IsSecret.Should().BeTrue();
    }

    [Fact]
    public void UpdateAchievementRequest_ShouldStoreProperties()
    {
        var req = new UpdateAchievementRequest
        {
            Name = "Updated",
            Points = 100,
            IsActive = false
        };

        req.Name.Should().Be("Updated");
        req.Points.Should().Be(100);
        req.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AwardAchievementRequest_ShouldStoreProperties()
    {
        var userId = Guid.NewGuid();
        var req = new AwardAchievementRequest
        {
            UserId = userId,
            Context = "manual award"
        };

        req.UserId.Should().Be(userId);
        req.Context.Should().Be("manual award");
    }
}
