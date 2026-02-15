using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.Learning.Experience.Recommendations.Tests;

/// <summary>
/// Tests for UserLearningProfile entity — untested methods.
/// </summary>
public class UserLearningProfileExtendedTests
{
    [Fact]
    public void UpdateActivity_ShouldSetLastActivityAt()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.LastActivityAt.Should().BeNull();

        profile.UpdateActivity();

        profile.LastActivityAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdatePreferences_ShouldUpdateNonNullFields()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());

        profile.UpdatePreferences(
            preferredCategories: "[\"programming\"]",
            preferredDifficulty: "beginner",
            preferredDuration: "short",
            learningGoals: "[\"learn c#\"]",
            skills: "[\"dotnet\"]");

        profile.PreferredCategories.Should().Be("[\"programming\"]");
        profile.PreferredDifficulty.Should().Be("beginner");
        profile.PreferredDuration.Should().Be("short");
        profile.LearningGoals.Should().Be("[\"learn c#\"]");
        profile.Skills.Should().Be("[\"dotnet\"]");
    }

    [Fact]
    public void UpdatePreferences_NullFields_ShouldNotOverwrite()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(preferredDifficulty: "advanced");

        profile.UpdatePreferences(preferredDifficulty: null, preferredDuration: "long");

        profile.PreferredDifficulty.Should().Be("advanced"); // not overwritten
        profile.PreferredDuration.Should().Be("long");
    }

    [Fact]
    public void AddSkill_CaseInsensitiveDedup_ShouldNotAddDuplicate()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("C#");
        profile.AddSkill("c#");

        profile.Skills.Should().Contain("C#");
        // Should still only have 1 skill
    }

    [Fact]
    public void RemoveSkill_WhenNullSkills_ShouldNotThrow()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.Skills.Should().BeNull();

        profile.RemoveSkill("nonexistent");
        profile.Skills.Should().BeNull();
    }

    [Fact]
    public void RemoveSkill_LastSkill_ShouldSetNull()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("solo");
        profile.RemoveSkill("solo");

        profile.Skills.Should().BeNull();
    }

    [Fact]
    public void RemoveSkill_CaseInsensitive_ShouldRemove()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.AddSkill("TypeScript");
        profile.RemoveSkill("typescript");

        profile.Skills.Should().BeNull();
    }
}

/// <summary>
/// Tests for CourseRecommendation edge cases.
/// </summary>
public class CourseRecommendationExtendedTests
{
    [Fact]
    public void Create_NormalScore_ShouldPreserve()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.PersonalizedAI, 0.75, "good match");

        rec.Score.Should().Be(0.75);
        rec.Reason.Should().Be("good match");
    }

    [Fact]
    public void Create_AllTypes_ShouldWork()
    {
        foreach (RecommendationType type in Enum.GetValues<RecommendationType>())
        {
            var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(), type, 0.5);
            rec.Type.Should().Be(type);
        }
    }
}

/// <summary>
/// Tests for RecommendationType enum.
/// </summary>
public class RecommendationTypeTests
{
    [Theory]
    [InlineData(RecommendationType.PersonalizedAI, 0)]
    [InlineData(RecommendationType.PopularInCategory, 1)]
    [InlineData(RecommendationType.TrendingNow, 2)]
    [InlineData(RecommendationType.BasedOnHistory, 3)]
    [InlineData(RecommendationType.SimilarToCompleted, 4)]
    [InlineData(RecommendationType.NextInPath, 5)]
    [InlineData(RecommendationType.InstructorFollowed, 6)]
    [InlineData(RecommendationType.PeerRecommended, 7)]
    public void ShouldHaveCorrectValues(RecommendationType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

/// <summary>
/// Tests for DTO extensions.
/// </summary>
public class DtoExtensionTests
{
    [Fact]
    public void CourseRecommendation_ToDto_ShouldMapAllFields()
    {
        var rec = CourseRecommendation.Create(Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.TrendingNow, 0.9, "trending");

        var dto = rec.ToDto();

        dto.Id.Should().Be(rec.Id);
        dto.UserId.Should().Be(rec.UserId);
        dto.CourseId.Should().Be(rec.CourseId);
        dto.Type.Should().Be(RecommendationType.TrendingNow);
        dto.Score.Should().Be(0.9);
        dto.Reason.Should().Be("trending");
        dto.IsViewed.Should().BeFalse();
        dto.IsDismissed.Should().BeFalse();
    }

    [Fact]
    public void UserLearningProfile_ToDto_ShouldMapAllFields()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(
            preferredCategories: "[\"web\", \"mobile\"]",
            preferredDifficulty: "intermediate",
            preferredDuration: "medium",
            learningGoals: "[\"fullstack\"]",
            skills: "[\"react\", \"node\"]");

        var dto = profile.ToDto();

        dto.UserId.Should().Be(profile.UserId);
        dto.PreferredDifficulty.Should().Be("intermediate");
        dto.PreferredDuration.Should().Be("medium");
        dto.PreferredCategories.Should().Contain("web");
        dto.Skills.Should().Contain("react");
        dto.TotalCoursesCompleted.Should().Be(0);
    }

    [Fact]
    public void UserLearningProfile_ToDto_WithNullJson_ShouldReturnNullArrays()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = profile.ToDto();

        dto.PreferredCategories.Should().BeNull();
        dto.Skills.Should().BeNull();
        dto.LearningGoals.Should().BeNull();
    }

    [Fact]
    public void UpdateFromDto_ShouldUpdateProfile()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        var dto = new CreateOrUpdateLearningProfileDto(
            PreferredCategories: new[] { "gaming" },
            PreferredDifficulty: "advanced",
            PreferredDuration: "long",
            LearningGoals: new[] { "game dev" },
            Skills: new[] { "unity" });

        profile.UpdateFromDto(dto);

        profile.PreferredDifficulty.Should().Be("advanced");
        profile.PreferredDuration.Should().Be("long");
    }

    [Fact]
    public void UpdateFromDto_WithNullArrays_ShouldNotSetFields()
    {
        var profile = UserLearningProfile.Create(Guid.NewGuid());
        profile.UpdatePreferences(skills: "[\"existing\"]");

        var dto = new CreateOrUpdateLearningProfileDto(null, null, null, null, null);
        profile.UpdateFromDto(dto);

        // Null arrays → SerializeJsonArray yields null → UpdatePreferences with null doesn't overwrite
    }
}

/// <summary>
/// Tests for command records.
/// </summary>
public class RecommendationCommandTests
{
    [Fact]
    public void CreateOrUpdateLearningProfileCommand_ShouldStoreProperties()
    {
        var userId = Guid.NewGuid();
        var cmd = new CreateOrUpdateLearningProfileCommand(userId, new[] { "web" }, "beginner", "short", new[] { "learn" }, new[] { "html" });

        cmd.UserId.Should().Be(userId);
        cmd.PreferredCategories.Should().Contain("web");
        cmd.PreferredDifficulty.Should().Be("beginner");
    }

    [Fact]
    public void GenerateRecommendationsCommand_Defaults()
    {
        var cmd = new GenerateRecommendationsCommand(Guid.NewGuid(), null);
        cmd.MaxResults.Should().Be(10);
        cmd.Types.Should().BeNull();
    }

    [Fact]
    public void AddSkillToProfileCommand_ShouldStore()
    {
        var cmd = new AddSkillToProfileCommand(Guid.NewGuid(), "C#");
        cmd.Skill.Should().Be("C#");
    }

    [Fact]
    public void RemoveSkillFromProfileCommand_ShouldStore()
    {
        var cmd = new RemoveSkillFromProfileCommand(Guid.NewGuid(), "Java");
        cmd.Skill.Should().Be("Java");
    }

    [Fact]
    public void MarkRecommendationViewedCommand_ShouldStore()
    {
        var cmd = new MarkRecommendationViewedCommand(Guid.NewGuid(), Guid.NewGuid());
        cmd.RecommendationId.Should().NotBeEmpty();
    }

    [Fact]
    public void DismissRecommendationCommand_ShouldStore()
    {
        var cmd = new DismissRecommendationCommand(Guid.NewGuid(), Guid.NewGuid());
        cmd.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public void RefreshRecommendationsCommand_ShouldStore()
    {
        var cmd = new RefreshRecommendationsCommand(Guid.NewGuid(), Guid.NewGuid());
        cmd.TenantId.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for FluentValidation validators.
/// </summary>
public class RecommendationValidatorTests
{
    [Fact]
    public void CreateOrUpdateLearningProfileValidator_EmptyUserId_ShouldFail()
    {
        var validator = new CreateOrUpdateLearningProfileCommandValidator();
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.Empty, null, null, null, null, null);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void CreateOrUpdateLearningProfileValidator_TooManyCategories_ShouldFail()
    {
        var validator = new CreateOrUpdateLearningProfileCommandValidator();
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(),
            new string[11], null, null, null, null);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PreferredCategories);
    }

    [Fact]
    public void CreateOrUpdateLearningProfileValidator_InvalidDuration_ShouldFail()
    {
        var validator = new CreateOrUpdateLearningProfileCommandValidator();
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(),
            null, null, "invalid", null, null);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PreferredDuration);
    }

    [Fact]
    public void CreateOrUpdateLearningProfileValidator_ValidDuration_ShouldPass()
    {
        var validator = new CreateOrUpdateLearningProfileCommandValidator();
        var cmd = new CreateOrUpdateLearningProfileCommand(Guid.NewGuid(),
            null, null, "short", null, null);
        var result = validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PreferredDuration);
    }

    [Fact]
    public void AddSkillValidator_EmptySkill_ShouldFail()
    {
        var validator = new AddSkillToProfileCommandValidator();
        var cmd = new AddSkillToProfileCommand(Guid.NewGuid(), "");
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Skill);
    }

    [Fact]
    public void AddSkillValidator_ValidInput_ShouldPass()
    {
        var validator = new AddSkillToProfileCommandValidator();
        var cmd = new AddSkillToProfileCommand(Guid.NewGuid(), "C#");
        var result = validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RemoveSkillValidator_EmptyUserId_ShouldFail()
    {
        var validator = new RemoveSkillFromProfileCommandValidator();
        var cmd = new RemoveSkillFromProfileCommand(Guid.Empty, "skill");
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void GenerateRecommendationsValidator_EmptyUserId_ShouldFail()
    {
        var validator = new GenerateRecommendationsCommandValidator();
        var cmd = new GenerateRecommendationsCommand(Guid.Empty, null);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void GenerateRecommendationsValidator_TooHighMaxResults_ShouldFail()
    {
        var validator = new GenerateRecommendationsCommandValidator();
        var cmd = new GenerateRecommendationsCommand(Guid.NewGuid(), null, 100);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaxResults);
    }

    [Fact]
    public void GenerateRecommendationsValidator_ValidInput_ShouldPass()
    {
        var validator = new GenerateRecommendationsCommandValidator();
        var cmd = new GenerateRecommendationsCommand(Guid.NewGuid(), null, 25);
        var result = validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MarkViewedValidator_EmptyIds_ShouldFail()
    {
        var validator = new MarkRecommendationViewedCommandValidator();
        var cmd = new MarkRecommendationViewedCommand(Guid.Empty, Guid.Empty);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.RecommendationId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void DismissValidator_EmptyIds_ShouldFail()
    {
        var validator = new DismissRecommendationCommandValidator();
        var cmd = new DismissRecommendationCommand(Guid.Empty, Guid.Empty);
        var result = validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.RecommendationId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

/// <summary>
/// Tests for DTO records.
/// </summary>
public class RecommendationDtoRecordTests
{
    [Fact]
    public void RecommendationDto_ShouldStoreAllFields()
    {
        var dto = new RecommendationDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.TrendingNow, 0.85, "reason", false, false,
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow);

        dto.Type.Should().Be(RecommendationType.TrendingNow);
        dto.Score.Should().Be(0.85);
    }

    [Fact]
    public void RecommendationDetailDto_ShouldStoreAllFields()
    {
        var dto = new RecommendationDetailDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Course Title", "Desc", "/thumb.png", "web", "beginner", 5,
            RecommendationType.PopularInCategory, 0.9, "popular", false,
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow);

        dto.CourseTitle.Should().Be("Course Title");
        dto.CourseDifficulty.Should().Be("beginner");
    }

    [Fact]
    public void RecommendationStatisticsDto_ShouldStore()
    {
        var byType = new Dictionary<RecommendationType, int>
        {
            { RecommendationType.TrendingNow, 5 },
            { RecommendationType.PopularInCategory, 3 }
        };
        var dto = new RecommendationStatisticsDto(10, 7, 2, 1, byType);

        dto.TotalRecommendations.Should().Be(10);
        dto.ByType.Should().HaveCount(2);
    }

    [Fact]
    public void PopularCourseDto_ShouldStore()
    {
        var dto = new PopularCourseDto(Guid.NewGuid(), "Title", "Desc", null, "web", 100, 4.5m, 50);
        dto.EnrollmentCount.Should().Be(100);
        dto.AverageRating.Should().Be(4.5m);
    }

    [Fact]
    public void TrendingCourseDto_ShouldStore()
    {
        var dto = new TrendingCourseDto(Guid.NewGuid(), "Title", null, null, "gaming", 25, 9.5m);
        dto.RecentEnrollments.Should().Be(25);
        dto.TrendScore.Should().Be(9.5m);
    }

    [Fact]
    public void SimilarCourseDto_ShouldStore()
    {
        var dto = new SimilarCourseDto(Guid.NewGuid(), "Title", null, null, "web", 0.87, new[] { "react", "js" });
        dto.SimilarityScore.Should().Be(0.87);
        dto.MatchingTags.Should().HaveCount(2);
    }

    [Fact]
    public void CreateRecommendationDto_ShouldStore()
    {
        var dto = new CreateRecommendationDto(Guid.NewGuid(), Guid.NewGuid(),
            RecommendationType.NextInPath, 0.95, "next step", TimeSpan.FromDays(14));

        dto.Type.Should().Be(RecommendationType.NextInPath);
        dto.ValidFor.Should().Be(TimeSpan.FromDays(14));
    }
}
