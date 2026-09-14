using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Contracts;

public sealed class QueriesAndValidatorCoverageTests
{
    [Fact]
    public void GetAllProgramsQuery_PreservesEveryFilterAndDefault()
    {
        var defaults = new GetAllProgramsQuery();

        Assert.Equal(0, defaults.Skip);
        Assert.Equal(50, defaults.Take);
        Assert.Null(defaults.Search);
        Assert.Null(defaults.Category);
        Assert.Null(defaults.Difficulty);
        Assert.Null(defaults.Status);
        Assert.Null(defaults.Visibility);
        Assert.Null(defaults.EnrollmentStatus);
        Assert.Null(defaults.CreatorId);
        Assert.False(defaults.IncludeArchived);
        Assert.Equal("CreatedAt", defaults.SortBy);
        Assert.True(defaults.SortDescending);

        var query = new GetAllProgramsQuery(
            10,
            25,
            "engine",
            (ProgramCategory)1,
            (ProgramDifficulty)1,
            (ContentStatus)1,
            (ContentVisibility)1,
            (EnrollmentStatus)1,
            "creator",
            true,
            "Title",
            false);

        Assert.Equal(10, query.Skip);
        Assert.Equal(25, query.Take);
        Assert.Equal("engine", query.Search);
        Assert.Equal((ProgramCategory)1, query.Category);
        Assert.Equal((ProgramDifficulty)1, query.Difficulty);
        Assert.Equal((ContentStatus)1, query.Status);
        Assert.Equal((ContentVisibility)1, query.Visibility);
        Assert.Equal((EnrollmentStatus)1, query.EnrollmentStatus);
        Assert.Equal("creator", query.CreatorId);
        Assert.True(query.IncludeArchived);
        Assert.Equal("Title", query.SortBy);
        Assert.False(query.SortDescending);
    }

    [Fact]
    public void SearchProgramsQuery_PreservesEveryFilterAndDefault()
    {
        var defaults = new SearchProgramsQuery("game");

        Assert.Equal("game", defaults.SearchTerm);
        Assert.Null(defaults.Category);
        Assert.Null(defaults.Difficulty);
        Assert.Null(defaults.MinEstimatedHours);
        Assert.Null(defaults.MaxEstimatedHours);
        Assert.Null(defaults.MinRating);
        Assert.False(defaults.AvailableForEnrollment);
        Assert.Equal(0, defaults.Skip);
        Assert.Equal(50, defaults.Take);

        var query = new SearchProgramsQuery(
            "rendering",
            (ProgramCategory)1,
            (ProgramDifficulty)1,
            2.5f,
            12.5f,
            4.2m,
            true,
            5,
            15);

        Assert.Equal("rendering", query.SearchTerm);
        Assert.Equal((ProgramCategory)1, query.Category);
        Assert.Equal((ProgramDifficulty)1, query.Difficulty);
        Assert.Equal(2.5f, query.MinEstimatedHours);
        Assert.Equal(12.5f, query.MaxEstimatedHours);
        Assert.Equal(4.2m, query.MinRating);
        Assert.True(query.AvailableForEnrollment);
        Assert.Equal(5, query.Skip);
        Assert.Equal(15, query.Take);
    }

    [Fact]
    public void AddProgramContentValidator_CoversRequiredBoundsAndOptionalReward()
    {
        var validator = new AddProgramContentCommandValidator();

        var invalid = validator.Validate(new AddProgramContentCommand(Guid.Empty, Guid.Empty, -1, PointsReward: -1));
        var tooLarge = validator.Validate(new AddProgramContentCommand(Guid.NewGuid(), Guid.NewGuid(), 0, PointsReward: 1001));
        var withoutReward = validator.Validate(new AddProgramContentCommand(Guid.NewGuid(), Guid.NewGuid(), 0));
        var valid = validator.Validate(new AddProgramContentCommand(Guid.NewGuid(), Guid.NewGuid(), 2, PointsReward: 1000));

        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(AddProgramContentCommand.ProgramId));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(AddProgramContentCommand.ContentId));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(AddProgramContentCommand.Order));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(AddProgramContentCommand.PointsReward));
        Assert.Contains(tooLarge.Errors, error => error.PropertyName == nameof(AddProgramContentCommand.PointsReward));
        Assert.True(withoutReward.IsValid);
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void UpdateEnrollmentStatusValidator_CoversEnumBoundsDeadlineAndOptionalValues()
    {
        var validator = new UpdateEnrollmentStatusCommandValidator();
        var now = SystemClock.UtcNow;

        var invalid = validator.Validate(new UpdateEnrollmentStatusCommand(
            Guid.Empty,
            (EnrollmentStatus)999,
            0,
            now.AddMinutes(-1)));
        var tooLarge = validator.Validate(new UpdateEnrollmentStatusCommand(
            Guid.NewGuid(),
            EnrollmentStatus.Open,
            10001,
            now.AddDays(1)));
        var withoutOptionals = validator.Validate(new UpdateEnrollmentStatusCommand(
            Guid.NewGuid(),
            EnrollmentStatus.Open));
        var valid = validator.Validate(new UpdateEnrollmentStatusCommand(
            Guid.NewGuid(),
            EnrollmentStatus.Open,
            10000,
            now.AddDays(1)));

        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateEnrollmentStatusCommand.ProgramId));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateEnrollmentStatusCommand.Status));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateEnrollmentStatusCommand.MaxEnrollments));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateEnrollmentStatusCommand.EnrollmentDeadline));
        Assert.Contains(tooLarge.Errors, error => error.PropertyName == nameof(UpdateEnrollmentStatusCommand.MaxEnrollments));
        Assert.True(withoutOptionals.IsValid);
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void UpdateProgramRatingValidator_CoversRequiredRangeAndOptionalReview()
    {
        var validator = new UpdateProgramRatingCommandValidator();

        var invalid = validator.Validate(new UpdateProgramRatingCommand(Guid.Empty, string.Empty, 0m, "short"));
        var tooLong = validator.Validate(new UpdateProgramRatingCommand(Guid.NewGuid(), "user", 5m, new string('x', 1001)));
        var withoutReview = validator.Validate(new UpdateProgramRatingCommand(Guid.NewGuid(), "user", 1m));
        var valid = validator.Validate(new UpdateProgramRatingCommand(Guid.NewGuid(), "user", 5m, "Great course"));

        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateProgramRatingCommand.ProgramId));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateProgramRatingCommand.UserId));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateProgramRatingCommand.Rating));
        Assert.Contains(invalid.Errors, error => error.PropertyName == nameof(UpdateProgramRatingCommand.Review));
        Assert.Contains(tooLong.Errors, error => error.PropertyName == nameof(UpdateProgramRatingCommand.Review));
        Assert.True(withoutReview.IsValid);
        Assert.True(valid.IsValid);
    }
}
