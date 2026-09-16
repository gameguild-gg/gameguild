using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ProgramUserCoverageTests
{
    [Fact]
    public void ComputedProperties_ReflectTenantLifecycleAndAccessDates()
    {
        var enrollment = new ProgramUser
        {
            JoinedAt = SystemClock.UtcNow.AddDays(-12)
        };

        enrollment.IsGlobal.Should().BeTrue();
        enrollment.IsCompleted.Should().BeFalse();
        enrollment.IsInProgress.Should().BeFalse();
        enrollment.DaysSinceEnrollment.Should().BeInRange(11, 12);
        enrollment.DaysSinceLastAccess.Should().BeNull();

        enrollment.Start();
        enrollment.IsInProgress.Should().BeTrue();
        enrollment.DaysSinceLastAccess.Should().Be(0);

        enrollment.Deactivate();
        enrollment.IsInProgress.Should().BeFalse();
        enrollment.TenantId = Guid.NewGuid();
        enrollment.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void Complete_WithoutExplicitGrade_UsesAverageOfGradedActivities()
    {
        var enrollment = new ProgramUser
        {
            ReceivedGrades =
            [
                new ActivityGrade { Points = 70m },
                new ActivityGrade { Points = null },
                new ActivityGrade { Points = 90m }
            ]
        };

        enrollment.AverageGrade.Should().Be(80m);
        enrollment.CalculateFinalGrade().Should().Be(80m);
        enrollment.Complete();

        enrollment.FinalGrade.Should().Be(80m);
        enrollment.IsCompleted.Should().BeTrue();
        enrollment.IsInProgress.Should().BeFalse();
    }

    [Fact]
    public void GradeCalculations_WhenGradesAreUnavailable_ReturnNull()
    {
        var enrollment = new ProgramUser();

        enrollment.AverageGrade.Should().BeNull();
        enrollment.CalculateFinalGrade().Should().BeNull();

        enrollment.ReceivedGrades = null!;
        enrollment.AverageGrade.Should().BeNull();
        enrollment.CalculateFinalGrade().Should().BeNull();
    }

    [Fact]
    public void UpdateCompletionPercentage_WhenNoRequiredContentExists_ResetsProgress()
    {
        var enrollment = new ProgramUser
        {
            CompletionPercentage = 75m,
            Program = new Program { ProgramContents = [new ProgramContent { IsRequired = false }] }
        };

        enrollment.UpdateCompletionPercentage();

        enrollment.CompletionPercentage.Should().Be(0m);

        enrollment.Program.ProgramContents = null!;
        enrollment.CompletionPercentage = 50m;
        enrollment.UpdateCompletionPercentage();
        enrollment.CompletionPercentage.Should().Be(0m);
    }

    [Fact]
    public void UpdateCompletionPercentage_ComputesPartialRequiredProgress()
    {
        var completedContent = new ProgramContent { Id = Guid.NewGuid(), IsRequired = true };
        var pendingContent = new ProgramContent { Id = Guid.NewGuid(), IsRequired = true };
        var enrollment = new ProgramUser
        {
            Program = new Program { ProgramContents = [completedContent, pendingContent] },
            ContentInteractions =
            [
                new ContentInteraction { ContentId = completedContent.Id, IsCompleted = true }
            ]
        };

        enrollment.UpdateCompletionPercentage();

        enrollment.CompletionPercentage.Should().Be(50m);
        enrollment.CompletedAt.Should().BeNull();
        enrollment.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void UpdateCompletionPercentage_WhenAllRequiredContentIsDone_AutoCompletesOnce()
    {
        var content = new ProgramContent { Id = Guid.NewGuid(), IsRequired = true };
        var enrollment = new ProgramUser
        {
            Program = new Program { ProgramContents = [content] },
            ContentInteractions = [new ContentInteraction { ContentId = content.Id, IsCompleted = true }]
        };

        enrollment.UpdateCompletionPercentage();
        var completedAt = enrollment.CompletedAt;
        enrollment.UpdateCompletionPercentage();

        enrollment.CompletionPercentage.Should().Be(100m);
        enrollment.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void CanAccessContent_RequiresActiveEnrollmentAndExistingAccessibleContent()
    {
        var content = new ProgramContent { Id = Guid.NewGuid(), Visibility = Visibility.Public };
        var enrollment = new ProgramUser
        {
            UserId = Guid.NewGuid(),
            Program = new Program { ProgramContents = [content] }
        };

        enrollment.CanAccessContent(content.Id).Should().BeTrue();
        enrollment.CanAccessContent(Guid.NewGuid()).Should().BeFalse();
        enrollment.Program.ProgramContents = null!;
        enrollment.CanAccessContent(content.Id).Should().BeFalse();
        enrollment.Program.ProgramContents = [content];
        enrollment.Deactivate();
        enrollment.CanAccessContent(content.Id).Should().BeFalse();
    }

    [Fact]
    public void GetContentProgress_ReturnsMatchingInteractionOrNull()
    {
        var contentId = Guid.NewGuid();
        var interaction = new ContentInteraction { ContentId = contentId };
        var enrollment = new ProgramUser { ContentInteractions = [interaction] };

        enrollment.GetContentProgress(contentId).Should().BeSameAs(interaction);
        enrollment.GetContentProgress(Guid.NewGuid()).Should().BeNull();

        enrollment.ContentInteractions = null!;
        enrollment.GetContentProgress(contentId).Should().BeNull();
    }
}
