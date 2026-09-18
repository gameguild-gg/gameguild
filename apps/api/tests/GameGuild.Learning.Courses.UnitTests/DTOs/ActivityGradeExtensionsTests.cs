using FluentAssertions;
using GameGuild.Identity.Users;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.DTOs;

public sealed class ActivityGradeExtensionsTests
{
    [Fact]
    public void ToDto_WhenGradeBelongsToSurvey_RejectsIdentityBearingGrade()
    {
        var grade = new ActivityGrade
        {
            ContentInteraction = new ContentInteraction
            {
                Content = new ProgramContent { Type = ProgramContentType.Survey }
            }
        };

        var act = () => grade.ToDto();

        act.Should().Throw<InvalidOperationException>().WithMessage("Survey grades are not available.");
    }

    [Fact]
    public void ToDto_WhenNavigationsAreUnavailable_MapsScalarFieldsAndNullSummaries()
    {
        var grade = CreateGrade();
        grade.ContentInteraction = null!;
        grade.GraderProgramUser = null;

        var dto = grade.ToDto();

        dto.Id.Should().Be(grade.Id);
        dto.ContentInteractionId.Should().Be(grade.ContentInteractionId);
        dto.GraderProgramUserId.Should().Be(grade.GraderProgramUserId);
        dto.Points.Should().Be(grade.Points);
        dto.MaxPoints.Should().Be(grade.MaxPoints);
        dto.Feedback.Should().Be(grade.Feedback);
        dto.GradingDetails.Should().Be(grade.GradingDetails);
        dto.GradedAt.Should().Be(grade.GradedAt);
        dto.CreatedAt.Should().Be(grade.CreatedAt);
        dto.UpdatedAt.Should().Be(grade.UpdatedAt);
        dto.ContentInteraction.Should().BeNull();
        dto.Grader.Should().BeNull();
    }

    [Fact]
    public void ToDto_WhenNestedEntitiesExist_MapsContentStudentAndGraderSummaries()
    {
        var student = new User { Id = Guid.NewGuid(), Name = "Student", Email = "student@example.com" };
        var grader = new User { Id = Guid.NewGuid(), Name = "Grader", Email = "grader@example.com" };
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            Title = "Capstone",
            Type = ProgramContentType.Assignment,
            EstimatedMinutes = 120
        };
        var interaction = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            ProgramUserId = Guid.NewGuid(),
            ContentId = content.Id,
            Status = ProgressStatus.Submitted,
            SubmittedAt = SystemClock.UtcNow,
            Content = content,
            ProgramUser = new ProgramUser { User = student }
        };
        var grade = CreateGrade();
        grade.ContentInteraction = interaction;
        grade.GraderProgramUser = new ProgramUser { User = grader };

        var dto = grade.ToDto();

        dto.ContentInteraction.Should().NotBeNull();
        dto.ContentInteraction!.Id.Should().Be(interaction.Id);
        dto.ContentInteraction.Status.Should().Be("Submitted");
        dto.ContentInteraction.Content.Should().BeEquivalentTo(new
        {
            content.Id,
            content.Title,
            ContentType = "Assignment",
            content.EstimatedMinutes
        });
        dto.ContentInteraction.Student.Should().BeEquivalentTo(new
        {
            student.Id,
            UserDisplayName = student.Name,
            UserEmail = student.Email
        });
        dto.Grader.Should().BeEquivalentTo(new
        {
            grader.Id,
            UserDisplayName = grader.Name,
            UserEmail = grader.Email,
            Role = "Grader"
        });
    }

    [Fact]
    public void ToDto_WhenNestedContentAndUsersAreMissing_LeavesTheirSummariesNull()
    {
        var grade = CreateGrade();
        grade.ContentInteraction = new ContentInteraction
        {
            Content = null!,
            ProgramUser = null!
        };
        grade.GraderProgramUser = new ProgramUser { User = null! };

        var dto = grade.ToDto();

        dto.ContentInteraction.Should().NotBeNull();
        dto.ContentInteraction!.Content.Should().BeNull();
        dto.ContentInteraction.Student.Should().BeNull();
        dto.Grader.Should().BeNull();
    }

    [Fact]
    public void ToDto_ForSequence_MapsEveryGradeLazily()
    {
        ActivityGrade[] grades = [CreateGrade(), CreateGrade()];

        var dtos = grades.ToDto().ToArray();

        dtos.Select(dto => dto.Id).Should().Equal(grades.Select(grade => grade.Id));
    }

    [Fact]
    public void ToDto_ForStatistics_MapsEveryMetric()
    {
        var statistics = new GradeStatistics
        {
            TotalGrades = 12,
            AverageGrade = Percent(82.5m),
            MinGrade = Percent(45),
            MaxGrade = Percent(100),
            PassingRate = Percent(75)
        };

        var dto = statistics.ToDto();

        dto.Should().BeEquivalentTo(statistics);
    }

    private static ActivityGrade CreateGrade() => new()
    {
        Id = Guid.NewGuid(),
        ContentInteractionId = Guid.NewGuid(),
        GraderProgramUserId = Guid.NewGuid(),
        Points = Score(88),
        MaxPoints = Score(100),
        Feedback = "Strong submission",
        GradingDetails = "{\"rubric\":true}",
        GradedAt = SystemClock.UtcNow
    };
}
