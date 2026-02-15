using FluentAssertions;
using GameGuild.Learning.Experience.Recommendations;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.UnitTests;

public class CourseRecommendationTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var rec = CourseRecommendation.Create(
            userId, courseId,
            RecommendationType.PersonalizedAI,
            0.92,
            reason: "Matches your skill profile",
            validFor: TimeSpan.FromDays(14));

        rec.Id.Should().NotBeEmpty();
        rec.UserId.Should().Be(userId);
        rec.CourseId.Should().Be(courseId);
        rec.Type.Should().Be(RecommendationType.PersonalizedAI);
        rec.Score.Should().BeApproximately(0.92, 0.001);
        rec.Reason.Should().Be("Matches your skill profile");
        rec.IsViewed.Should().BeFalse();
        rec.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldClampScore()
    {
        var high = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.TrendingNow, 5.0);
        var low = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.TrendingNow, -1.0);

        high.Score.Should().Be(1.0);
        low.Score.Should().Be(0.0);
    }

    [Fact]
    public void Create_WithDefaultValidFor_ShouldExpireIn30Days()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.PopularInCategory, 0.5);

        rec.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void MarkViewed_ShouldSetFlag()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.BasedOnHistory, 0.7);

        rec.MarkViewed();

        rec.IsViewed.Should().BeTrue();
    }

    [Fact]
    public void Dismiss_ShouldSetFlag()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.SimilarToCompleted, 0.6);

        rec.Dismiss();

        rec.IsDismissed.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenNotDismissedAndNotExpired_ShouldBeTrue()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.NextInPath, 0.8);

        rec.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenDismissed_ShouldBeFalse()
    {
        var rec = CourseRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.NextInPath, 0.8);
        rec.Dismiss();

        rec.IsValid().Should().BeFalse();
    }
}

public class UserLearningProfileTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var userId = Guid.NewGuid();

        var profile = UserLearningProfile.Create(userId);

        profile.Id.Should().NotBeEmpty();
        profile.UserId.Should().Be(userId);
        profile.TotalCoursesCompleted.Should().Be(0);
        profile.TotalHoursLearned.Should().Be(0);
        profile.LastActivityAt.Should().BeNull();
        profile.PreferredCategories.Should().BeNull();
        profile.Skills.Should().BeNull();
    }

    [Fact]
    public void UpdateActivity_ShouldSetTimestamp()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.UpdateActivity();

        profile.LastActivityAt.Should().NotBeNull();
    }

    [Fact]
    public void IncrementCoursesCompleted_ShouldTrackStats()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.IncrementCoursesCompleted(10);
        profile.IncrementCoursesCompleted(5);

        profile.TotalCoursesCompleted.Should().Be(2);
        profile.TotalHoursLearned.Should().Be(15);
    }

    [Fact]
    public void UpdatePreferences_ShouldSetOnlyProvided()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.UpdatePreferences(
            preferredDifficulty: "intermediate",
            preferredDuration: "medium");

        profile.PreferredDifficulty.Should().Be("intermediate");
        profile.PreferredDuration.Should().Be("medium");
        profile.PreferredCategories.Should().BeNull(); // not set
    }

    [Fact]
    public void AddSkill_ShouldAddToJsonArray()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.AddSkill("C#");
        profile.AddSkill("Unity");

        profile.Skills.Should().Contain("C#");
        profile.Skills.Should().Contain("Unity");
    }

    [Fact]
    public void AddSkill_Duplicate_ShouldNotAddTwice()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.AddSkill("C#");
        profile.AddSkill("c#"); // case-insensitive duplicate

        // Should only contain one entry
        var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(profile.Skills!);
        skills.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveSkill_ShouldRemoveFromJsonArray()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.AddSkill("Unity");

        profile.RemoveSkill("C#");

        profile.Skills.Should().Contain("Unity");
        profile.Skills.Should().NotContain("C#");
    }

    [Fact]
    public void RemoveSkill_LastSkill_ShouldSetNull()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");

        profile.RemoveSkill("C#");

        profile.Skills.Should().BeNull();
    }

    [Fact]
    public void RemoveSkill_WhenEmpty_ShouldNotThrow()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        var act = () => profile.RemoveSkill("Nonexistent");

        act.Should().NotThrow();
    }
}

public class RecommendationTypeEnumTests
{
    [Fact]
    public void ShouldHave8Values()
    {
        Enum.GetValues<RecommendationType>().Should().HaveCount(8);
    }

    [Theory]
    [InlineData(RecommendationType.PersonalizedAI, 0)]
    [InlineData(RecommendationType.PeerRecommended, 7)]
    public void ExtremeValues(RecommendationType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

// ===== VALIDATOR TESTS =====

public class CreateOrUpdateLearningProfileCommandValidatorTests
{
    private readonly CreateOrUpdateLearningProfileCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, null, null, null);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.Empty, null, null, null, null, null);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TooManyCategories_ShouldFail()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(),
            Enumerable.Range(0, 11).Select(i => $"cat{i}").ToArray(), null, null, null, null);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidDuration_ShouldFail()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, "huge", null, null);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidDuration_Short_ShouldPass()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, "short", null, null);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidDuration_Medium_ShouldPass()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, "medium", null, null);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidDuration_Long_ShouldPass()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, "long", null, null);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void TooManyGoals_ShouldFail()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, null,
            Enumerable.Range(0, 21).Select(i => $"goal{i}").ToArray(), null);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TooManySkills_ShouldFail()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, null, null, null,
            Enumerable.Range(0, 51).Select(i => $"skill{i}").ToArray());
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PreferredDifficultyTooLong_ShouldFail()
    {
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(), null, new string('x', 51), null, null, null);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class AddSkillToProfileCommandValidatorTests
{
    private readonly AddSkillToProfileCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new AddSkillToProfileCommand(Guid.NewGuid(), "C#");
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new AddSkillToProfileCommand(Guid.Empty, "C#");
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptySkill_ShouldFail()
    {
        var cmd = new AddSkillToProfileCommand(Guid.NewGuid(), "");
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class GenerateRecommendationsCommandValidatorTests
{
    private readonly GenerateRecommendationsCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new GenerateRecommendationsCommand(Guid.NewGuid(), null, 10);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new GenerateRecommendationsCommand(Guid.Empty, null);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MaxResultsTooLow_ShouldFail()
    {
        var cmd = new GenerateRecommendationsCommand(Guid.NewGuid(), null, 0);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MaxResultsTooHigh_ShouldFail()
    {
        var cmd = new GenerateRecommendationsCommand(Guid.NewGuid(), null, 51);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class MarkRecommendationViewedCommandValidatorTests
{
    private readonly MarkRecommendationViewedCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new MarkRecommendationViewedCommand(Guid.NewGuid(), Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyRecommendationId_ShouldFail()
    {
        var cmd = new MarkRecommendationViewedCommand(Guid.Empty, Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new MarkRecommendationViewedCommand(Guid.NewGuid(), Guid.Empty);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class DismissRecommendationCommandValidatorTests
{
    private readonly DismissRecommendationCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new DismissRecommendationCommand(Guid.NewGuid(), Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyRecommendationId_ShouldFail()
    {
        var cmd = new DismissRecommendationCommand(Guid.Empty, Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new DismissRecommendationCommand(Guid.NewGuid(), Guid.Empty);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
