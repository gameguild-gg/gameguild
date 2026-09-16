using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Commands;

public sealed class UpdateProgramCommandValidatorTests
{
    private readonly UpdateProgramCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenOnlyIdIsProvided_AcceptsPartialUpdate()
    {
        var result = _validator.Validate(new UpdateProgramCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenAllFieldsAreValid_AcceptsUpdate()
    {
        var command = new UpdateProgramCommand(
            Guid.NewGuid(),
            "Production-ready course",
            "A complete description for this production-ready course.",
            "A concise course summary.",
            "http://cdn.example.com/thumbnail.png",
            "https://video.example.com/showcase",
            24,
            ProgramCategory.General,
            ProgramDifficulty.Advanced,
            EnrollmentStatus.Open,
            500,
            SystemClock.UtcNow.AddDays(7));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIdAndLengthsAreInvalid_ReturnsEveryRelevantError()
    {
        var command = new UpdateProgramCommand(
            Guid.Empty,
            "ab",
            "too short",
            "brief");

        var result = _validator.Validate(command);

        result.Errors.Select(error => error.ErrorMessage).Should().Contain([
            "Program ID is required",
            "Program title must be between 3 and 255 characters",
            "Program description must be between 10 and 2000 characters",
            "Program summary must be between 10 and 500 characters"
        ]);
    }

    [Theory]
    [InlineData("relative/image.png")]
    [InlineData("ftp://example.com/image.png")]
    public void Validate_WhenThumbnailUrlIsUnsupported_ReturnsUrlError(string url)
    {
        var result = _validator.Validate(new UpdateProgramCommand(Guid.NewGuid(), Thumbnail: url));

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Thumbnail must be a valid URL");
    }

    [Theory]
    [InlineData("relative/video")]
    [InlineData("file:///tmp/video.mp4")]
    public void Validate_WhenShowcaseUrlIsUnsupported_ReturnsUrlError(string url)
    {
        var result = _validator.Validate(new UpdateProgramCommand(Guid.NewGuid(), VideoShowcaseUrl: url));

        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Video showcase URL must be a valid URL");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Validate_WhenEstimatedHoursAreOutsideRange_ReturnsRangeError(float hours)
    {
        var result = _validator.Validate(new UpdateProgramCommand(Guid.NewGuid(), EstimatedHours: hours));

        result.Errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public void Validate_WhenMaximumEnrollmentsAreOutsideRange_ReturnsRangeError(int maximum)
    {
        var result = _validator.Validate(new UpdateProgramCommand(Guid.NewGuid(), MaxEnrollments: maximum));

        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_WhenEnumsAndDeadlineAreInvalid_ReturnsEveryRelevantError()
    {
        var command = new UpdateProgramCommand(
            Guid.NewGuid(),
            Category: (ProgramCategory)int.MaxValue,
            Difficulty: (ProgramDifficulty)int.MaxValue,
            EnrollmentStatus: (EnrollmentStatus)int.MaxValue,
            EnrollmentDeadline: SystemClock.UtcNow.AddMinutes(-1));

        var result = _validator.Validate(command);

        result.Errors.Select(error => error.ErrorMessage).Should().Contain([
            "Invalid program category",
            "Invalid program difficulty",
            "Invalid enrollment status",
            "Enrollment deadline must be in the future"
        ]);
    }
}
