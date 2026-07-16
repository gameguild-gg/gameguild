using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssignmentDeliveryContractTests
{
    [Fact]
    public void Assessment_ShouldExposeExplicitSubmissionAndPresentationContracts()
    {
        var assembly = typeof(Assessment).Assembly;
        var modality = assembly.GetType("GameGuild.Learning.Assessments.SubmissionModality");
        var presentationMode = assembly.GetType("GameGuild.Learning.Assessments.AssessmentPresentationMode");

        modality.Should().NotBeNull();
        presentationMode.Should().NotBeNull();
        typeof(Assessment).GetProperty("SubmissionModalities").Should().NotBeNull();
        typeof(Assessment).GetProperty("PresentationMode").Should().NotBeNull();
    }

    [Fact]
    public void DeliveryEnums_ShouldKeepExplicitPersistedValues()
    {
        ((int)SubmissionModality.Text).Should().Be(1);
        ((int)SubmissionModality.File).Should().Be(2);
        ((int)SubmissionModality.Url).Should().Be(4);
        ((int)SubmissionModality.Code).Should().Be(8);
        ((int)SubmissionModality.Media).Should().Be(16);
        ((int)SubmissionModality.Project).Should().Be(32);
        ((int)SubmissionModality.StructuredAnswer).Should().Be(64);
        ((int)AssessmentPresentationMode.SingleStep).Should().Be(0);
        ((int)AssessmentPresentationMode.Continuous).Should().Be(1);
    }

    [Fact]
    public void SetDeliverySchedule_WhenAvailabilityEndsBeforeItStarts_ShouldRejectIt()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Assignment", AssessmentType.Assignment, 100, 60);

        var action = () => assessment.SetAvailability(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*availability end must be on or after availability start*");
    }

    [Fact]
    public void AssessmentSubmission_ShouldExposePersistedPayloadsForEverySupportedModality()
    {
        var properties = typeof(AssessmentSubmission).GetProperties().Select(property => property.Name);

        properties.Should().Contain(new[]
        {
            "TextPayload",
            "FilePayload",
            "UrlPayload",
            "CodePayload",
            "MediaPayload",
            "ProjectPayload",
            "StructuredAnswerPayload"
        });
    }

    [Fact]
    public void AssessmentSubmission_ShouldExposeLateSubmissionMetadata()
    {
        typeof(AssessmentSubmission).GetProperty("IsLate").Should().NotBeNull();
    }

    [Fact]
    public void SetDeliverySchedule_WhenLateDeadlineIsOutsideAvailability_ShouldRejectIt()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Assignment", AssessmentType.Assignment, 100, 60);
        var dueAt = DateTime.UtcNow.AddDays(1);

        var action = () => assessment.SetDeliverySchedule(
            DateTime.UtcNow,
            dueAt.AddDays(1),
            dueAt,
            allowLateSubmissions: true,
            lateSubmissionDeadline: dueAt.AddDays(2));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*Late submission deadline must be on or before availability end*");
    }

    [Fact]
    public void Update_WhenAvailabilityEndsBeforeItStarts_ShouldRejectIt()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Assignment", AssessmentType.Assignment, 100, 60);

        var action = () => assessment.Update(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(1));

        action.Should().Throw<ArgumentException>()
            .WithMessage("*availability end must be on or after availability start*");
    }

    [Fact]
    public void TryGetSubmissionTiming_WhenInsideLateWindow_ShouldAcceptAndMarkLate()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Assignment", AssessmentType.Assignment, 100, 60);
        var dueAt = DateTime.UtcNow.AddDays(1);
        assessment.SetDeliverySchedule(
            DateTime.UtcNow,
            dueAt.AddDays(3),
            dueAt,
            allowLateSubmissions: true,
            lateSubmissionDeadline: dueAt.AddDays(2));

        var canSubmit = assessment.TryGetSubmissionTiming(dueAt.AddDays(1), out var isLate);

        canSubmit.Should().BeTrue();
        isLate.Should().BeTrue();
    }

    [Fact]
    public void SetPayload_WhenPayloadUsesAllowedModalities_ShouldPersistEachPayload()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var payload = new SubmitAssessmentRequest(
            TextPayload: "Reflection notes",
            FilePayload: "asset:file-123",
            UrlPayload: "https://example.test/demo",
            CodePayload: "Console.WriteLine(\"hello\");",
            MediaPayload: "https://example.test/demo.mp4",
            ProjectPayload: "project:abc",
            StructuredAnswerPayload: "{\"answer\":42}");

        submission.SetPayload(payload, SubmissionModality.Text |
                                       SubmissionModality.File |
                                       SubmissionModality.Url |
                                       SubmissionModality.Code |
                                       SubmissionModality.Media |
                                       SubmissionModality.Project |
                                       SubmissionModality.StructuredAnswer);

        submission.SubmittedModalities.Should().Be(SubmissionModality.Text |
                                                    SubmissionModality.File |
                                                    SubmissionModality.Url |
                                                    SubmissionModality.Code |
                                                    SubmissionModality.Media |
                                                    SubmissionModality.Project |
                                                    SubmissionModality.StructuredAnswer);
        submission.TextPayload.Should().Be(payload.TextPayload);
        submission.FilePayload.Should().Be(payload.FilePayload);
        submission.UrlPayload.Should().Be(payload.UrlPayload);
        submission.CodePayload.Should().Be(payload.CodePayload);
        submission.MediaPayload.Should().Be(payload.MediaPayload);
        submission.ProjectPayload.Should().Be(payload.ProjectPayload);
        submission.StructuredAnswerPayload.Should().Be(payload.StructuredAnswerPayload);
    }

    [Fact]
    public void SetPayload_WhenStructuredAnswerIsInvalidJson_ShouldRejectIt()
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        var action = () => submission.SetPayload(
            new SubmitAssessmentRequest(StructuredAnswerPayload: "not-json"),
            SubmissionModality.StructuredAnswer);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Structured answer payload must be valid JSON*");
    }

    [Fact]
    public void Assessments_ShouldPersistInteractiveVideoCueLinks()
    {
        var cueType = typeof(Assessment).Assembly.GetType("GameGuild.Learning.Assessments.InteractiveVideoAssessmentCue");

        cueType.Should().NotBeNull();
        cueType!.GetProperty("AssessmentId").Should().NotBeNull();
        cueType.GetProperty("ContentId").Should().NotBeNull();
        cueType.GetProperty("CueId").Should().NotBeNull();
    }

    [Fact]
    public void DatabaseContract_ShouldRestrictDeliveryEnumsAndSchedule()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        new AssessmentsModelConfiguration().Configure(modelBuilder);
        var entity = modelBuilder.Model.FindEntityType(typeof(Assessment))!;
        var constraints = entity.GetCheckConstraints();

        constraints.Should().Contain(constraint => constraint.Name == "CK_Assessments_SubmissionModalities");
        constraints.Should().Contain(constraint => constraint.Name == "CK_Assessments_PresentationMode");
        constraints.Should().Contain(constraint => constraint.Name == "CK_Assessments_DeliverySchedule");
    }
}
