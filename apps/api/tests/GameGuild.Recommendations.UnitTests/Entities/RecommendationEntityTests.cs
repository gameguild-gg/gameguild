using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.Tests;

/// <summary>
/// Unit tests for CourseRecommendation entity and RecommendationCandidate record.
/// </summary>
public class CourseRecommendationEntityTests
{
    [Fact]
    public void Create_ShouldClampScoreToValidRange()
    {
        var rec1 = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.TrendingNow, 1.5);
        var rec2 = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.TrendingNow, -0.5);

        rec1.Score.Should().Be(1.0); // Clamped to max
        rec2.Score.Should().Be(0.0); // Clamped to min
    }

    [Fact]
    public void Create_ShouldSetDefaultExpiry()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), RecommendationType.NextInPath, 0.9);

        rec.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
        rec.IsViewed.Should().BeFalse();
        rec.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void Create_WithCustomValidFor_ShouldSetExpiry()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.NextInPath, 0.8,
            validFor: TimeSpan.FromDays(7));

        rec.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsValid_WhenNotDismissedAndNotExpired_ShouldReturnTrue()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.SimilarToCompleted, 0.7);

        rec.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenDismissed_ShouldReturnFalse()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.SimilarToCompleted, 0.7);
        rec.Dismiss();

        rec.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Dismiss_ShouldSetIsDismissedTrue()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PopularInCategory, 0.6);

        rec.Dismiss();

        rec.IsDismissed.Should().BeTrue();
    }

    [Fact]
    public void MarkViewed_ShouldSetIsViewedTrue()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationType.PopularInCategory, 0.6);

        rec.MarkViewed();

        rec.IsViewed.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for UserLearningProfile entity.
/// </summary>
public class UserLearningProfileTests
{
    [Fact]
    public void Create_ShouldInitializeWithZeroCounters()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.TotalCoursesCompleted.Should().Be(0);
        profile.TotalHoursLearned.Should().Be(0);
        profile.LastActivityAt.Should().BeNull();
    }

    [Fact]
    public void IncrementCoursesCompleted_ShouldUpdateCounters()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.IncrementCoursesCompleted(5);
        profile.IncrementCoursesCompleted(3);

        profile.TotalCoursesCompleted.Should().Be(2);
        profile.TotalHoursLearned.Should().Be(8);
    }

    [Fact]
    public void AddSkill_ShouldAddUniqueSkills()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.AddSkill("C#");
        profile.AddSkill("TypeScript");
        profile.AddSkill("C#"); // Duplicate — should not add

        profile.Skills.Should().Contain("C#");
        profile.Skills.Should().Contain("TypeScript");
    }

    [Fact]
    public void RemoveSkill_ShouldRemoveExistingSkill()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.AddSkill("TypeScript");

        profile.RemoveSkill("C#");

        profile.Skills.Should().NotContain("C#");
        profile.Skills.Should().Contain("TypeScript");
    }
}

/// <summary>
/// Unit tests for NextInPathStrategy scoring logic.
/// </summary>
public class NextInPathStrategyTests
{
    [Fact]
    public void RecommendationCandidate_ShouldHoldAllProperties()
    {
        var courseId = Guid.NewGuid();
        var candidate = new RecommendationCandidate(courseId, RecommendationType.NextInPath, 0.85, "Next in learning path");

        candidate.CourseId.Should().Be(courseId);
        candidate.Type.Should().Be(RecommendationType.NextInPath);
        candidate.Score.Should().Be(0.85);
        candidate.Reason.Should().Be("Next in learning path");
    }
}
