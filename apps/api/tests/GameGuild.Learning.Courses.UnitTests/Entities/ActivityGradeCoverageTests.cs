using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ActivityGradeCoverageTests
{
    [Fact]
    public void ComputedProperties_RepresentMissingAndTypedGrades()
    {
        var grade = new ActivityGrade { GradedAt = SystemClock.UtcNow.AddDays(-4) };

        grade.Grade.Should().Be(0m);
        grade.PercentageScore.Should().BeNull();
        grade.IsPassing.Should().BeNull();
        grade.IsAutomaticGrade.Should().BeFalse();
        grade.IsPeerReview.Should().BeFalse();
        grade.IsGlobal.Should().BeTrue();
        grade.DaysSinceGrading.Should().BeInRange(3, 4);

        grade.Points = 50m;
        grade.MaxPoints = null;
        grade.PercentageScore.Should().BeNull();
        grade.MaxPoints = 0m;
        grade.PercentageScore.Should().BeNull();

        grade.MaxPoints = 100m;
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

        grade.AssignPoints(75m);
        grade.Points.Should().Be(75m);
        grade.MaxPoints.Should().Be(100m);

        grade.MaxPoints = 80m;
        grade.AssignPoints(70m);
        grade.Points.Should().Be(70m);
        grade.MaxPoints.Should().Be(80m);
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
        var grade = new ActivityGrade { Points = points, MaxPoints = points.HasValue ? 100m : null };

        grade.CalculateLetterGrade().Should().Be(expected);
    }

    [Fact]
    public void IsValid_CoversOptionalAndInvalidMaximum()
    {
        new ActivityGrade().IsValid().Should().BeTrue();
        new ActivityGrade { Points = 0m, MaxPoints = 0m }.IsValid().Should().BeFalse();
        new ActivityGrade { Points = null, MaxPoints = 100m }.IsValid().Should().BeTrue();
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
            Points = 93m,
            MaxPoints = 100m,
            Feedback = "Original feedback",
            GradeType = GradeType.PeerReview,
            AttemptNumber = 2,
            TenantId = tenantId
        };

        var revision = grade.CreateRevision(95m);

        revision.Should().BeEquivalentTo(new
        {
            grade.StudentId,
            grade.GraderId,
            grade.ContentInteractionId,
            grade.ProgramUserId,
            Points = 95m,
            MaxPoints = 100m,
            GradeLetter = "A",
            Feedback = "Original feedback",
            IsFinalized = false,
            grade.GradeType,
            AttemptNumber = 3,
            TenantId = (Guid?)tenantId
        });
    }
}
