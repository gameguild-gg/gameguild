using FluentAssertions;
using GameGuild.Learning.Experience.LearningPaths;
using Xunit;

namespace GameGuild.Learning.Experience.LearningPaths.UnitTests;

public class LearningPathTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var creatorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var path = LearningPath.Create(
            creatorId, "C# Mastery", "csharp-mastery",
            LearningPathDifficulty.Intermediate, tenantId);

        path.Id.Should().NotBeEmpty();
        path.CreatorId.Should().Be(creatorId);
        path.Title.Should().Be("C# Mastery");
        path.Slug.Should().Be("csharp-mastery");
        path.Difficulty.Should().Be(LearningPathDifficulty.Intermediate);
        path.TenantId.Should().Be(tenantId);
        path.IsPublished.Should().BeFalse();
        path.IsFeatured.Should().BeFalse();
        path.EnrollmentCount.Should().Be(0);
        path.CompletionCount.Should().Be(0);
        path.Courses.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseBeginner()
    {
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");

        path.Difficulty.Should().Be(LearningPathDifficulty.Beginner);
        path.TenantId.Should().BeNull();
    }

    [Fact]
    public void AddCourse_ShouldAddToCollection()
    {
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        var courseId = Guid.NewGuid();

        path.AddCourse(courseId, 1, isRequired: true);

        path.Courses.Should().HaveCount(1);
        var course = path.Courses.First();
        course.LearningPathId.Should().Be(path.Id);
        course.CourseId.Should().Be(courseId);
        course.Order.Should().Be(1);
        course.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void AddCourse_MultipleCourses_ShouldMaintainAll()
    {
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");

        path.AddCourse(Guid.NewGuid(), 1);
        path.AddCourse(Guid.NewGuid(), 2);
        path.AddCourse(Guid.NewGuid(), 3, isRequired: false);

        path.Courses.Should().HaveCount(3);
    }

    [Fact]
    public void Publish_ShouldSetIsPublished()
    {
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");

        path.Publish();

        path.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Unpublish_ShouldClearIsPublished()
    {
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        path.Publish();

        path.Unpublish();

        path.IsPublished.Should().BeFalse();
    }
}

public class LearningPathCourseTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var pathId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var lpc = new LearningPathCourse(pathId, courseId, 2, false);

        lpc.LearningPathId.Should().Be(pathId);
        lpc.CourseId.Should().Be(courseId);
        lpc.Order.Should().Be(2);
        lpc.IsRequired.Should().BeFalse();
    }
}

public class LearningPathEnrollmentTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var pathId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var enrollment = LearningPathEnrollment.Create(pathId, userId, 5);

        enrollment.Id.Should().NotBeEmpty();
        enrollment.LearningPathId.Should().Be(pathId);
        enrollment.UserId.Should().Be(userId);
        enrollment.Progress.Should().Be(0);
        enrollment.CoursesCompleted.Should().Be(0);
        enrollment.TotalCourses.Should().Be(5);
        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.InProgress);
        enrollment.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateProgress_ShouldCalculatePercentage()
    {
        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 10);

        enrollment.UpdateProgress(3);

        enrollment.CoursesCompleted.Should().Be(3);
        enrollment.Progress.Should().Be(30); // 3/10 * 100 = 30
    }

    [Fact]
    public void UpdateProgress_WhenAllCompleted_ShouldAutoComplete()
    {
        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        enrollment.UpdateProgress(5);

        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.Completed);
        enrollment.Progress.Should().Be(100);
        enrollment.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProgress_WithZeroTotalCourses_ShouldAutoComplete()
    {
        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 0);

        enrollment.UpdateProgress(0);

        // 0 >= 0 triggers auto-complete
        enrollment.Progress.Should().Be(100);
        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.Completed);
    }

    [Fact]
    public void Complete_ShouldSetStatusAndProgress()
    {
        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        enrollment.Complete();

        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.Completed);
        enrollment.Progress.Should().Be(100);
        enrollment.CompletedAt.Should().NotBeNull();
    }
}

public class LearningPathDifficultyEnumTests
{
    [Theory]
    [InlineData(LearningPathDifficulty.Beginner, 0)]
    [InlineData(LearningPathDifficulty.Intermediate, 1)]
    [InlineData(LearningPathDifficulty.Advanced, 2)]
    [InlineData(LearningPathDifficulty.Expert, 3)]
    public void Values(LearningPathDifficulty difficulty, int expected)
    {
        ((int)difficulty).Should().Be(expected);
    }
}

public class LearningPathEnrollmentStatusEnumTests
{
    [Fact]
    public void ShouldHave3Values()
    {
        Enum.GetValues<LearningPathEnrollmentStatus>().Should().HaveCount(3);
    }
}

// ===== VALIDATOR TESTS =====

public class CreateLearningPathCommandValidatorTests
{
    private readonly CreateLearningPathCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), "My Path");
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyCreatorId_ShouldFail()
    {
        var cmd = new CreateLearningPathCommand(Guid.Empty, "My Path");
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyTitle_ShouldFail()
    {
        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), "");
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), new string('x', 201));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DescriptionTooLong_ShouldFail()
    {
        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), "Path", Description: new string('x', 5001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ImageUrlTooLong_ShouldFail()
    {
        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), "Path", ImageUrl: new string('x', 2001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeEstimatedHours_ShouldFail()
    {
        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), "Path", EstimatedHours: -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class UpdateLearningPathCommandValidatorTests
{
    private readonly UpdateLearningPathCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new UpdateLearningPathCommand(Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyId_ShouldFail()
    {
        var cmd = new UpdateLearningPathCommand(Guid.Empty);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var cmd = new UpdateLearningPathCommand(Guid.NewGuid(), Title: new string('x', 201));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DescriptionTooLong_ShouldFail()
    {
        var cmd = new UpdateLearningPathCommand(Guid.NewGuid(), Description: new string('x', 5001));
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeEstimatedHours_ShouldFail()
    {
        var cmd = new UpdateLearningPathCommand(Guid.NewGuid(), EstimatedHours: -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class AddCourseToPathCommandValidatorTests
{
    private readonly AddCourseToPathCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new AddCourseToPathCommand(Guid.NewGuid(), Guid.NewGuid(), 0);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyLearningPathId_ShouldFail()
    {
        var cmd = new AddCourseToPathCommand(Guid.Empty, Guid.NewGuid(), 0);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyCourseId_ShouldFail()
    {
        var cmd = new AddCourseToPathCommand(Guid.NewGuid(), Guid.Empty, 0);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeOrder_ShouldFail()
    {
        var cmd = new AddCourseToPathCommand(Guid.NewGuid(), Guid.NewGuid(), -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class EnrollInPathCommandValidatorTests
{
    private readonly EnrollInPathCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new EnrollInPathCommand(Guid.NewGuid(), Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyLearningPathId_ShouldFail()
    {
        var cmd = new EnrollInPathCommand(Guid.Empty, Guid.NewGuid());
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new EnrollInPathCommand(Guid.NewGuid(), Guid.Empty);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class UpdatePathProgressCommandValidatorTests
{
    private readonly UpdatePathProgressCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new UpdatePathProgressCommand(Guid.NewGuid(), Guid.NewGuid(), 5);
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void NegativeCoursesCompleted_ShouldFail()
    {
        var cmd = new UpdatePathProgressCommand(Guid.NewGuid(), Guid.NewGuid(), -1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var cmd = new UpdatePathProgressCommand(Guid.NewGuid(), Guid.Empty, 1);
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class ReorderPathCoursesCommandValidatorTests
{
    private readonly ReorderPathCoursesCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new ReorderPathCoursesCommand(Guid.NewGuid(),
            new[] { new CourseOrderDto(Guid.NewGuid(), 0) });
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyLearningPathId_ShouldFail()
    {
        var cmd = new ReorderPathCoursesCommand(Guid.Empty,
            new[] { new CourseOrderDto(Guid.NewGuid(), 0) });
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyCoursesList_ShouldFail()
    {
        var cmd = new ReorderPathCoursesCommand(Guid.NewGuid(),
            Array.Empty<CourseOrderDto>());
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
