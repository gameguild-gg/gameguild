using Xunit;
using GameGuild.Tags;

namespace GameGuild.Learning.Courses.UnitTests.Contracts;

public sealed class QueriesAndValidatorCoverageTests
{
    [Fact]
    public void ProgramTagQueries_PreserveFiltersPaginationAndSkillProjection()
    {
        var tagId = Guid.NewGuid();
        var secondTagId = Guid.NewGuid();
        var program = new Program { Id = Guid.NewGuid(), Title = "Rendering", Slug = "rendering" };
        var tag = new ProgramTagDto(
            Guid.NewGuid(),
            program.Id,
            tagId,
            "C++",
            "Skill",
            SkillProficiencyLevel.Advanced,
            true,
            1);

        var byTag = new GetProgramsByTagQuery(tagId, 5, 10);
        var bySkill = new GetProgramsBySkillQuery(tagId, SkillProficiencyLevel.Intermediate, 10, 15);
        var bySkills = new GetProgramsBySkillsQuery([tagId, secondTagId], true, 20, 25);
        var search = new SearchProgramsByTagNameQuery("render", 30, 35);
        var projected = new ProgramWithSkillDto(program, tag);

        Assert.Equal(tagId, byTag.TagId);
        Assert.Equal(5, byTag.Skip);
        Assert.Equal(10, byTag.Take);
        Assert.Equal(tagId, bySkill.SkillTagId);
        Assert.Equal(SkillProficiencyLevel.Intermediate, bySkill.MinProficiency);
        Assert.Equal(10, bySkill.Skip);
        Assert.Equal(15, bySkill.Take);
        Assert.Equal(new[] { tagId, secondTagId }, bySkills.SkillTagIds);
        Assert.True(bySkills.RequireAll);
        Assert.Equal(20, bySkills.Skip);
        Assert.Equal(25, bySkills.Take);
        Assert.Equal("render", search.TagName);
        Assert.Equal(30, search.Skip);
        Assert.Equal(35, search.Take);
        Assert.Same(program, projected.Program);
        Assert.Same(tag, projected.SkillTag);
    }

    [Fact]
    public void ProgramTagRequests_ExposeDefaultsAndExplicitUpdates()
    {
        var tagId = Guid.NewGuid();
        var defaults = new GetProgramsBySkillQuery(tagId);
        var multipleDefaults = new GetProgramsBySkillsQuery([tagId]);
        var update = new UpdateProgramTagDto(SkillProficiencyLevel.Expert, true, 4);

        Assert.Equal(SkillProficiencyLevel.Beginner, defaults.MinProficiency);
        Assert.Equal(0, defaults.Skip);
        Assert.Equal(20, defaults.Take);
        Assert.False(multipleDefaults.RequireAll);
        Assert.Equal(0, multipleDefaults.Skip);
        Assert.Equal(20, multipleDefaults.Take);
        Assert.Equal(SkillProficiencyLevel.Expert, update.ProficiencyLevel);
        Assert.True(update.IsPrimary);
        Assert.Equal(4, update.DisplayOrder);
    }

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

    [Fact]
    public void TwoIdentityValidators_RejectMissingValuesAndAcceptValidValues()
    {
        Assert.False(new AddToWishlistCommandValidator()
            .Validate(new AddToWishlistCommand(Guid.Empty, string.Empty)).IsValid);
        Assert.True(new AddToWishlistCommandValidator()
            .Validate(new AddToWishlistCommand(Guid.NewGuid(), "user")).IsValid);

        Assert.False(new RemoveFromWishlistCommandValidator()
            .Validate(new RemoveFromWishlistCommand(Guid.Empty, string.Empty)).IsValid);
        Assert.True(new RemoveFromWishlistCommandValidator()
            .Validate(new RemoveFromWishlistCommand(Guid.NewGuid(), "user")).IsValid);

        Assert.False(new UnenrollUserCommandValidator()
            .Validate(new UnenrollUserCommand(Guid.Empty, string.Empty)).IsValid);
        Assert.True(new UnenrollUserCommandValidator()
            .Validate(new UnenrollUserCommand(Guid.NewGuid(), "user")).IsValid);

        Assert.False(new DeleteProgramRatingCommandValidator()
            .Validate(new DeleteProgramRatingCommand(Guid.Empty, string.Empty)).IsValid);
        Assert.True(new DeleteProgramRatingCommandValidator()
            .Validate(new DeleteProgramRatingCommand(Guid.NewGuid(), "user")).IsValid);

        Assert.False(new RemoveProgramContentCommandValidator()
            .Validate(new RemoveProgramContentCommand(Guid.Empty, Guid.Empty)).IsValid);
        Assert.True(new RemoveProgramContentCommandValidator()
            .Validate(new RemoveProgramContentCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("Invalid Slug")]
    public void SlugValidators_RejectInvalidSlugs(string slug)
    {
        Assert.False(new GetProgramBySlugQueryValidator()
            .Validate(new GetProgramBySlugQuery(slug)).IsValid);
        Assert.False(new GetPublishedProgramBySlugQueryValidator()
            .Validate(new GetPublishedProgramBySlugQuery(slug)).IsValid);
    }

    [Fact]
    public void SlugValidators_AcceptCanonicalSlug()
    {
        Assert.True(new GetProgramBySlugQueryValidator()
            .Validate(new GetProgramBySlugQuery("game-programming-101")).IsValid);
        Assert.True(new GetPublishedProgramBySlugQueryValidator()
            .Validate(new GetPublishedProgramBySlugQuery("game-programming-101")).IsValid);
    }

    [Fact]
    public void SingleIdentityValidators_RejectEmptyAndAcceptValidIds()
    {
        Assert.False(new ArchiveProgramCommandValidator().Validate(new ArchiveProgramCommand(Guid.Empty)).IsValid);
        Assert.True(new ArchiveProgramCommandValidator().Validate(new ArchiveProgramCommand(Guid.NewGuid())).IsValid);
        Assert.False(new DeleteProgramCommandValidator().Validate(new DeleteProgramCommand(Guid.Empty)).IsValid);
        Assert.True(new DeleteProgramCommandValidator().Validate(new DeleteProgramCommand(Guid.NewGuid())).IsValid);
        Assert.False(new PublishProgramCommandValidator().Validate(new PublishProgramCommand(Guid.Empty)).IsValid);
        Assert.True(new PublishProgramCommandValidator().Validate(new PublishProgramCommand(Guid.NewGuid())).IsValid);
        Assert.False(new RestoreProgramCommandValidator().Validate(new RestoreProgramCommand(Guid.Empty)).IsValid);
        Assert.True(new RestoreProgramCommandValidator().Validate(new RestoreProgramCommand(Guid.NewGuid())).IsValid);
        Assert.False(new UnpublishProgramCommandValidator().Validate(new UnpublishProgramCommand(Guid.Empty)).IsValid);
        Assert.True(new UnpublishProgramCommandValidator().Validate(new UnpublishProgramCommand(Guid.NewGuid())).IsValid);
        Assert.False(new GetProgramByIdQueryValidator().Validate(new GetProgramByIdQuery(Guid.Empty)).IsValid);
        Assert.True(new GetProgramByIdQueryValidator().Validate(new GetProgramByIdQuery(Guid.NewGuid())).IsValid);
    }
}
