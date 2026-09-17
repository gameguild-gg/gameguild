using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ActivityGradeCoverageTests
{
    [Fact]
    public void ComputedProperties_RepresentMissingAndTypedGrades()
    {
        var grade = new ActivityGrade { GradedAt = SystemClock.UtcNow.AddDays(-4) };

        grade.Points.Should().BeNull();
        grade.PercentageScore.Should().BeNull();
        grade.IsPassing.Should().BeNull();
        grade.IsAutomaticGrade.Should().BeFalse();
        grade.IsPeerReview.Should().BeFalse();
        grade.IsGlobal.Should().BeTrue();
        grade.DaysSinceGrading.Should().BeInRange(3, 4);

        grade.Points = Score(50);
        grade.MaxPoints = null;
        grade.PercentageScore.Should().BeNull();
        grade.MaxPoints = ScoreValue.Zero;
        grade.PercentageScore.Should().BeNull();

        grade.MaxPoints = Score(100);
        grade.IsPassing.Should().BeFalse();
        grade.GradeType = GradeType.Automatic;
        grade.IsAutomaticGrade.Should().BeTrue();
        grade.GradeType = GradeType.PeerReview;
        grade.IsPeerReview.Should().BeTrue();
        grade.TenantId = Guid.NewGuid();
        grade.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void AssignPoints_UsesExistingOrDefaultMaximumWhenNotProvided()
    {
        var grade = new ActivityGrade();

        grade.AssignPoints(Score(75));
        grade.Points.Should().Be(Score(75));
        grade.MaxPoints.Should().Be(Score(100));

        grade.MaxPoints = Score(80);
        grade.AssignPoints(Score(70));
        grade.Points.Should().Be(Score(70));
        grade.MaxPoints.Should().Be(Score(80));
    }

    [Fact]
    public void Mutators_PersistFeedbackRubricTimeAndNullableLetter()
    {
        var grade = new ActivityGrade();

        grade.SetLetterGrade(null!);
        grade.UpdateFeedback("Detailed feedback");
        grade.RecordGradingTime(12);
        grade.SetRubricData("{\"criteria\":[]}");

        grade.GradeLetter.Should().BeNull();
        grade.Feedback.Should().Be("Detailed feedback");
        grade.GradingTimeMinutes.Should().Be(12);
        grade.RubricData.Should().Be("{\"criteria\":[]}");
        grade.UpdatedAt.Should().NotBe(default);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(77, "C+")]
    [InlineData(70, "C-")]
    [InlineData(67, "D+")]
    [InlineData(63, "D")]
    public void CalculateLetterGrade_CoversRemainingBoundaries(int? points, string? expected)
    {
        var grade = new ActivityGrade
        {
            Points = points.HasValue ? Score(points.Value) : null,
            MaxPoints = points.HasValue ? Score(100) : null
        };

        grade.CalculateLetterGrade().Should().Be(expected);
    }

    [Fact]
    public void IsValid_CoversOptionalAndInvalidMaximum()
    {
        new ActivityGrade().IsValid().Should().BeTrue();
        new ActivityGrade { Points = ScoreValue.Zero, MaxPoints = ScoreValue.Zero }.IsValid().Should().BeFalse();
        new ActivityGrade { Points = null, MaxPoints = Score(100) }.IsValid().Should().BeTrue();
    }

    [Fact]
    public void CreateRevision_WithoutReason_CopiesIdentityAndOriginalFeedback()
    {
        var tenantId = Guid.NewGuid();
        var grade = new ActivityGrade
        {
            StudentId = Guid.NewGuid(),
            GraderId = Guid.NewGuid(),
            ContentInteractionId = Guid.NewGuid(),
            ProgramUserId = Guid.NewGuid(),
            Points = Score(93),
            MaxPoints = Score(100),
            Feedback = "Original feedback",
            GradeType = GradeType.PeerReview,
            AttemptNumber = 2,
            TenantId = tenantId
        };

        var revision = grade.CreateRevision(Score(95));

        revision.Should().BeEquivalentTo(new
        {
            grade.StudentId,
            grade.GraderId,
            grade.ContentInteractionId,
            grade.ProgramUserId,
            Points = Score(95),
            MaxPoints = Score(100),
            GradeLetter = "A",
            Feedback = "Original feedback",
            IsFinalized = false,
            grade.GradeType,
            AttemptNumber = 3,
            TenantId = (Guid?)tenantId
        });
    }
}
