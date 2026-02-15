using FluentAssertions;
using GameGuild.Learning.Courses;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

#region Program Entity Tests

public class ProgramTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var program = new Program();

        program.Title.Should().BeEmpty();
        program.Status.Should().Be(ContentStatus.Draft);
        program.EnrollmentStatus.Should().Be(EnrollmentStatus.Open);
        program.Visibility.Should().Be(ContentVisibility.Public);
        program.Category.Should().Be(ProgramCategory.General);
        program.Difficulty.Should().Be(ProgramDifficulty.Beginner);
    }

    [Fact]
    public void OpenEnrollment_ShouldSetStatus()
    {
        var program = new Program();
        program.CloseEnrollment();

        program.OpenEnrollment();

        program.EnrollmentStatus.Should().Be(EnrollmentStatus.Open);
    }

    [Fact]
    public void CloseEnrollment_ShouldSetStatus()
    {
        var program = new Program();

        program.CloseEnrollment();

        program.EnrollmentStatus.Should().Be(EnrollmentStatus.Closed);
    }

    [Fact]
    public void Publish_ShouldSetStatusToPublished()
    {
        var program = new Program();

        program.Publish();

        program.Status.Should().Be(ContentStatus.Published);
    }

    [Fact]
    public void Archive_ShouldSetStatusAndCloseEnrollment()
    {
        var program = new Program();

        program.Archive();

        program.Status.Should().Be(ContentStatus.Archived);
        program.EnrollmentStatus.Should().Be(EnrollmentStatus.Closed);
    }

    [Fact]
    public void IsEnrollmentOpen_WhenOpenAndNoBounds_ShouldBeTrue()
    {
        var program = new Program { EnrollmentStatus = EnrollmentStatus.Open };

        program.IsEnrollmentOpen.Should().BeTrue();
    }

    [Fact]
    public void IsEnrollmentOpen_WhenClosed_ShouldBeFalse()
    {
        var program = new Program { EnrollmentStatus = EnrollmentStatus.Closed };

        program.IsEnrollmentOpen.Should().BeFalse();
    }

    [Fact]
    public void IsEnrollmentOpen_WhenPastDeadline_ShouldBeFalse()
    {
        var program = new Program
        {
            EnrollmentStatus = EnrollmentStatus.Open,
            EnrollmentDeadline = DateTime.UtcNow.AddDays(-1)
        };

        program.IsEnrollmentOpen.Should().BeFalse();
    }

    [Fact]
    public void SetMetadata_ShouldSerializeToJson()
    {
        var program = new Program();

        program.SetMetadata("key1", "value1");

        program.Metadata.Should().Contain("key1");
        program.Metadata.Should().Contain("value1");
    }

    [Fact]
    public void AverageRating_WithNoRatings_ShouldBeZero()
    {
        var program = new Program();

        program.AverageRating.Should().Be(0);
    }

    [Fact]
    public void TotalRatings_WithNoRatings_ShouldBeZero()
    {
        var program = new Program();

        program.TotalRatings.Should().Be(0);
    }

    [Fact]
    public void IsGlobal_WhenNoTenant_ShouldBeTrue()
    {
        var program = new Program();

        program.IsGlobal.Should().BeTrue();
    }
}

#endregion

#region ProgramContent Tests

public class ProgramContentTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var content = new ProgramContent();

        content.Title.Should().BeEmpty();
        content.Type.Should().Be(ProgramContentType.Lesson);
        content.IsRequired.Should().BeTrue();
        content.GradingMethod.Should().Be(GradingMethod.None);
        content.Visibility.Should().Be(Visibility.Public);
        content.SortOrder.Should().Be(0);
    }

    [Fact]
    public void MoveTo_ShouldUpdateParentAndOrder()
    {
        var content = new ProgramContent();
        var newParentId = Guid.NewGuid();

        content.MoveTo(newParentId, 5);

        content.ParentId.Should().Be(newParentId);
        content.SortOrder.Should().Be(5);
    }

    [Fact]
    public void Reorder_ShouldUpdateSortOrder()
    {
        var content = new ProgramContent();

        content.Reorder(3);

        content.SortOrder.Should().Be(3);
    }

    [Fact]
    public void MakeRequired_ShouldSetTrue()
    {
        var content = new ProgramContent { IsRequired = false };

        content.MakeRequired();

        content.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void MakeOptional_ShouldSetFalse()
    {
        var content = new ProgramContent();

        content.MakeOptional();

        content.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void SetGrading_ShouldUpdateFields()
    {
        var content = new ProgramContent();

        content.SetGrading(GradingMethod.AutomatedTests, 100);

        content.GradingMethod.Should().Be(GradingMethod.AutomatedTests);
        content.MaxPoints.Should().Be(100);
    }

    [Fact]
    public void UpdateEstimatedTime_ShouldSet()
    {
        var content = new ProgramContent();

        content.UpdateEstimatedTime(30);

        content.EstimatedMinutes.Should().Be(30);
    }

    [Fact]
    public void FullPath_WithNoParent_ShouldBeTitle()
    {
        var content = new ProgramContent { Title = "Lesson 1" };

        content.FullPath.Should().Be("Lesson 1");
    }

    [Fact]
    public void HasChildren_WithNoChildren_ShouldBeFalse()
    {
        var content = new ProgramContent();

        content.HasChildren.Should().BeFalse();
    }
}

#endregion

#region ProgramEnrollment Tests

public class ProgramEnrollmentTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var enrollment = new ProgramEnrollment();

        enrollment.EnrollmentSource.Should().Be(EnrollmentSource.Manual);
        enrollment.EnrollmentStatus.Should().Be(EnrollmentStatus.Active);
        enrollment.CompletionStatus.Should().Be(CompletionStatus.NotStarted);
        enrollment.ProgressPercentage.Should().Be(0);
        enrollment.CertificateIssued.Should().BeFalse();
    }

    [Fact]
    public void MarkAsCompleted_ShouldSetCompletionFields()
    {
        var enrollment = new ProgramEnrollment();

        enrollment.MarkAsCompleted(85m);

        enrollment.CompletionStatus.Should().Be(CompletionStatus.Completed);
        enrollment.ProgressPercentage.Should().Be(100m);
        enrollment.CompletedAt.Should().NotBeNull();
        enrollment.FinalGrade.Should().Be(85m);
    }

    [Fact]
    public void MarkAsCompleted_ShouldClampGrade()
    {
        var enrollment = new ProgramEnrollment();

        enrollment.MarkAsCompleted(150m);

        enrollment.FinalGrade.Should().Be(100m);
    }

    [Fact]
    public void MarkAsCompleted_NegativeGrade_ShouldClampToZero()
    {
        var enrollment = new ProgramEnrollment();

        enrollment.MarkAsCompleted(-10m);

        enrollment.FinalGrade.Should().Be(0m);
    }

    [Fact]
    public void MarkAsCompleted_WithoutGrade_ShouldNotSetFinalGrade()
    {
        var enrollment = new ProgramEnrollment();

        enrollment.MarkAsCompleted();

        enrollment.FinalGrade.Should().BeNull();
    }
}

#endregion

#region ProgramUser Tests

public class ProgramUserTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var user = new ProgramUser();

        user.IsActive.Should().BeTrue();
        user.CompletionPercentage.Should().Be(0);
    }

    [Fact]
    public void Start_ShouldSetStartedAtOnce()
    {
        var user = new ProgramUser();

        user.Start();
        var firstStart = user.StartedAt;
        user.Start(); // second call should not change

        user.StartedAt.Should().Be(firstStart);
        user.LastAccessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_ShouldSetCompletionFields()
    {
        var user = new ProgramUser();

        user.Complete(92m);

        user.CompletedAt.Should().NotBeNull();
        user.CompletionPercentage.Should().Be(100m);
        user.FinalGrade.Should().Be(92m);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldNotChange()
    {
        var user = new ProgramUser();
        user.Complete(80m);
        var firstCompleted = user.CompletedAt;

        user.Complete(95m); // second call

        user.CompletedAt.Should().Be(firstCompleted);
        user.FinalGrade.Should().Be(80m);
    }

    [Fact]
    public void Deactivate_ShouldSetFalse()
    {
        var user = new ProgramUser();

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_ShouldSetTrue()
    {
        var user = new ProgramUser();
        user.Deactivate();

        user.Reactivate();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_WhenCompletedAtSet_ShouldBeTrue()
    {
        var user = new ProgramUser();
        user.Complete();

        user.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsInProgress_WhenStartedButNotCompleted_ShouldBeTrue()
    {
        var user = new ProgramUser();
        user.Start();

        user.IsInProgress.Should().BeTrue();
    }
}

#endregion

#region ActivityGrade Tests

public class ActivityGradeTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var grade = new ActivityGrade();

        grade.IsFinalized.Should().BeFalse();
        grade.GradeType.Should().Be(GradeType.Manual);
        grade.AttemptNumber.Should().Be(1);
    }

    [Fact]
    public void AssignPoints_ShouldClampToMax()
    {
        var grade = new ActivityGrade();

        grade.AssignPoints(150, 100);

        grade.Points.Should().Be(100);
        grade.MaxPoints.Should().Be(100);
    }

    [Fact]
    public void AssignPoints_ShouldClampToZero()
    {
        var grade = new ActivityGrade();

        grade.AssignPoints(-5, 100);

        grade.Points.Should().Be(0);
    }

    [Fact]
    public void SetLetterGrade_ShouldUppercase()
    {
        var grade = new ActivityGrade();

        grade.SetLetterGrade("b+");

        grade.GradeLetter.Should().Be("B+");
    }

    [Fact]
    public void FinalizeGrade_ShouldSetFlag()
    {
        var grade = new ActivityGrade();

        grade.FinalizeGrade();

        grade.IsFinalized.Should().BeTrue();
    }

    [Fact]
    public void Unlock_ShouldClearFinalized()
    {
        var grade = new ActivityGrade();
        grade.FinalizeGrade();

        grade.Unlock();

        grade.IsFinalized.Should().BeFalse();
    }

    [Fact]
    public void PercentageScore_ShouldCalculateCorrectly()
    {
        var grade = new ActivityGrade { Points = 85, MaxPoints = 100 };

        grade.PercentageScore.Should().Be(85m);
    }

    [Fact]
    public void IsPassing_Above60_ShouldBeTrue()
    {
        var grade = new ActivityGrade { Points = 70, MaxPoints = 100 };

        grade.IsPassing.Should().BeTrue();
    }

    [Fact]
    public void IsPassing_Below60_ShouldBeFalse()
    {
        var grade = new ActivityGrade { Points = 50, MaxPoints = 100 };

        grade.IsPassing.Should().BeFalse();
    }

    [Theory]
    [InlineData(97, "A+")]
    [InlineData(93, "A")]
    [InlineData(90, "A-")]
    [InlineData(87, "B+")]
    [InlineData(83, "B")]
    [InlineData(80, "B-")]
    [InlineData(73, "C")]
    [InlineData(60, "D-")]
    [InlineData(55, "F")]
    public void CalculateLetterGrade_ShouldReturnCorrectLetter(int points, string expected)
    {
        var grade = new ActivityGrade { Points = points, MaxPoints = 100 };

        grade.CalculateLetterGrade().Should().Be(expected);
    }

    [Fact]
    public void IsValid_WithPointsAboveMax_ShouldBeFalse()
    {
        var grade = new ActivityGrade { Points = 110, MaxPoints = 100 };

        grade.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNegativePoints_ShouldBeFalse()
    {
        var grade = new ActivityGrade { Points = -5 };

        grade.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithValidData_ShouldBeTrue()
    {
        var grade = new ActivityGrade { Points = 80, MaxPoints = 100 };

        grade.IsValid().Should().BeTrue();
    }

    [Fact]
    public void CreateRevision_ShouldIncrementAttempt()
    {
        var grade = new ActivityGrade
        {
            StudentId = Guid.NewGuid(),
            GraderId = Guid.NewGuid(),
            ContentInteractionId = Guid.NewGuid(),
            ProgramUserId = Guid.NewGuid(),
            Points = 70,
            MaxPoints = 100,
            AttemptNumber = 1
        };

        var revision = grade.CreateRevision(85, "Regraded after appeal");

        revision.AttemptNumber.Should().Be(2);
        revision.Points.Should().Be(85);
        revision.IsFinalized.Should().BeFalse();
        revision.Feedback.Should().Contain("Revision");
    }

    [Fact]
    public void Grade_Shim_ShouldMapToPoints()
    {
        var grade = new ActivityGrade();

        grade.Grade = 75m;

        grade.Points.Should().Be(75m);
        grade.Grade.Should().Be(75m);
    }
}

#endregion

#region CoursePrerequisite Tests

public class CoursePrerequisiteTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var courseId = Guid.NewGuid();
        var prereqId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var prereq = CoursePrerequisite.Create(courseId, prereqId, tenantId,
            PrerequisiteType.Required, 70, "Must pass with C or better", 1, "Group A");

        prereq.Id.Should().NotBeEmpty();
        prereq.CourseId.Should().Be(courseId);
        prereq.PrerequisiteCourseId.Should().Be(prereqId);
        prereq.Type.Should().Be(PrerequisiteType.Required);
        prereq.MinimumGrade.Should().Be(70);
        prereq.Description.Should().Be("Must pass with C or better");
        prereq.DisplayOrder.Should().Be(1);
        prereq.PrerequisiteGroup.Should().Be("Group A");
    }

    [Fact]
    public void Create_SelfReference_ShouldThrow()
    {
        var courseId = Guid.NewGuid();

        var act = () => CoursePrerequisite.Create(courseId, courseId, null);

        act.Should().Throw<ArgumentException>().WithMessage("*cannot be a prerequisite of itself*");
    }

    [Fact]
    public void SetMinimumGrade_OutOfRange_ShouldThrow()
    {
        var prereq = CoursePrerequisite.Create(Guid.NewGuid(), Guid.NewGuid(), null);

        var act = () => prereq.SetMinimumGrade(101);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetMinimumGrade_Negative_ShouldThrow()
    {
        var prereq = CoursePrerequisite.Create(Guid.NewGuid(), Guid.NewGuid(), null);

        var act = () => prereq.SetMinimumGrade(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetMinimumGrade_Null_ShouldClear()
    {
        var prereq = CoursePrerequisite.Create(Guid.NewGuid(), Guid.NewGuid(), null, minimumGrade: 80);

        prereq.SetMinimumGrade(null);

        prereq.MinimumGrade.Should().BeNull();
    }
}

#endregion

#region ProgramRating Tests

public class ProgramRatingTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var rating = new ProgramRating();

        rating.IsVerified.Should().BeFalse();
        rating.IsFeatured.Should().BeFalse();
        rating.HelpfulVotes.Should().Be(0);
        rating.UnhelpfulVotes.Should().Be(0);
    }

    [Fact]
    public void CanSetRating()
    {
        var rating = new ProgramRating
        {
            ProgramId = Guid.NewGuid(),
            UserId = "user-1",
            Rating = 4.5m,
            Review = "Great course!"
        };

        rating.Rating.Should().Be(4.5m);
        rating.Review.Should().Be("Great course!");
    }
}

#endregion

#region ContentInteraction Tests

public class ContentInteractionTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var interaction = new ContentInteraction();

        interaction.IsCompleted.Should().BeFalse();
        interaction.ProgressPercentage.Should().Be(0);
        interaction.TimeSpentMinutes.Should().Be(0);
        interaction.Status.Should().Be(ProgressStatus.NotStarted);
        interaction.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void Start_ShouldSetStartedAtOnce()
    {
        var interaction = new ContentInteraction();

        interaction.Start();
        var first = interaction.StartedAt;
        interaction.Start(); // second call

        interaction.StartedAt.Should().Be(first);
        interaction.IsStarted.Should().BeTrue();
    }

    [Fact]
    public void UpdateProgress_ShouldClampTo100()
    {
        var interaction = new ContentInteraction();

        interaction.UpdateProgress(150);

        interaction.ProgressPercentage.Should().Be(100);
    }

    [Fact]
    public void UpdateProgress_ShouldClampToZero()
    {
        var interaction = new ContentInteraction();

        interaction.UpdateProgress(-10);

        interaction.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void CompletionPercentage_ShouldAliasProgressPercentage()
    {
        var interaction = new ContentInteraction();

        interaction.CompletionPercentage = 50;

        interaction.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public void IsInProgress_WhenStartedNotCompleted_ShouldBeTrue()
    {
        var interaction = new ContentInteraction();
        interaction.Start();

        interaction.IsInProgress.Should().BeTrue();
    }
}

#endregion

#region Enum Tests

public class CoursesEnumTests
{
    [Fact]
    public void GradeType_ShouldHave6Values()
    {
        Enum.GetValues<GradeType>().Should().HaveCount(6);
    }

    [Fact]
    public void PrerequisiteType_ShouldHave3Values()
    {
        Enum.GetValues<PrerequisiteType>().Should().HaveCount(3);
    }
}

#endregion
