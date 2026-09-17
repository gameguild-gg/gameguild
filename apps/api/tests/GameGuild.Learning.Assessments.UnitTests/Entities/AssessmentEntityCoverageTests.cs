using FluentAssertions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Courses;
using System.Reflection;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentEntityCoverageTests
{
    [Fact]
    public void Mutators_ApplyEverySupportedDeliveryAndGroupingValue()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Original", AssessmentType.Quiz, Score(100));
        var groupId = Guid.NewGuid();
        var groupSetId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        assessment.SetMaxScore(Score(150));
        assessment.AssignToGroup(groupId);
        assessment.SetDeliveryContract(
            SubmissionModality.Text | SubmissionModality.File,
            AssessmentPresentationMode.Continuous);
        assessment.Update(
            title: "Updated",
            description: "Description",
            clearDescription: false,
            maxScore: Score(125),
            passingScore: null,
            timeLimitMinutes: 30,
            clearTimeLimitMinutes: false,
            maxAttempts: 1,
            isRequired: false,
            availableFrom: null,
            clearAvailableFrom: false,
            availableUntil: null,
            clearAvailableUntil: false,
            contentId: contentId,
            assessmentGroupId: groupId,
            submissionModalities: SubmissionModality.Project,
            presentationMode: AssessmentPresentationMode.SingleStep,
            reviewMethods: ReviewMethods.AutomatedReview,
            groupSetId: groupSetId,
            slug: "updated-slug");

        assessment.MaxScore.Should().Be(Score(125));
        assessment.ContentId.Should().Be(contentId);
        assessment.AssessmentGroupId.Should().Be(groupId);
        assessment.GroupSetId.Should().Be(groupSetId);
        assessment.SubmissionModalities.Should().Be(SubmissionModality.Project);
        assessment.PresentationMode.Should().Be(AssessmentPresentationMode.SingleStep);
        assessment.ReviewMethods.Should().Be(ReviewMethods.AutomatedReview);
        assessment.Slug.Should().Be("updated-slug");
    }

    [Fact]
    public void Update_CanClearOptionalLinksAndSchedule()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100));
        var dueAt = DateTime.UtcNow.AddDays(1);
        assessment.AssignToGroup(Guid.NewGuid());
        assessment.AssignToGroupSet(Guid.NewGuid());
        assessment.SetDeliverySchedule(null, dueAt.AddDays(2), dueAt, true, dueAt.AddDays(1));

        assessment.Update(
            title: null,
            description: null,
            clearDescription: false,
            maxScore: null,
            passingScore: null,
            timeLimitMinutes: null,
            clearTimeLimitMinutes: false,
            maxAttempts: null,
            isRequired: null,
            availableFrom: null,
            clearAvailableFrom: false,
            availableUntil: dueAt.AddDays(2),
            clearAvailableUntil: false,
            clearContentId: true,
            clearAssessmentGroupId: true,
            clearDueAt: true,
            allowLateSubmissions: false,
            clearLateSubmissionDeadline: true,
            clearGroupSetId: true);

        assessment.ContentId.Should().BeNull();
        assessment.AssessmentGroupId.Should().BeNull();
        assessment.GroupSetId.Should().BeNull();
        assessment.DueAt.Should().BeNull();
        assessment.LateSubmissionDeadline.Should().BeNull();
    }

    [Fact]
    public void Update_CanProvideScheduleAndEitherDeliveryFieldIndependently()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100), slug: "quiz");
        var dueAt = DateTime.UtcNow.AddHours(2);

        assessment.Update(
            title: null,
            description: null,
            clearDescription: false,
            maxScore: null,
            passingScore: null,
            timeLimitMinutes: null,
            clearTimeLimitMinutes: false,
            maxAttempts: null,
            isRequired: null,
            availableFrom: null,
            clearAvailableFrom: false,
            availableUntil: dueAt.AddHours(2),
            clearAvailableUntil: false,
            presentationMode: AssessmentPresentationMode.Continuous,
            dueAt: dueAt,
            allowLateSubmissions: true,
            lateSubmissionDeadline: dueAt.AddHours(1));
        assessment.Update(
            title: null,
            description: null,
            clearDescription: false,
            maxScore: null,
            passingScore: null,
            timeLimitMinutes: null,
            clearTimeLimitMinutes: false,
            maxAttempts: null,
            isRequired: null,
            availableFrom: null,
            clearAvailableFrom: false,
            availableUntil: dueAt.AddHours(2),
            clearAvailableUntil: false,
            submissionModalities: SubmissionModality.Code);

        assessment.DueAt.Should().Be(dueAt);
        assessment.LateSubmissionDeadline.Should().Be(dueAt.AddHours(1));
        assessment.SubmissionModalities.Should().Be(SubmissionModality.Code);
        assessment.PresentationMode.Should().Be(AssessmentPresentationMode.Continuous);
    }

    [Fact]
    public void Update_WithExistingContentAndNoContentChange_IsAllowed()
    {
        var contentId = Guid.NewGuid();
        var assessment = Assessment.Create(
            Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100), contentId: contentId);

        assessment.Update(
            title: null,
            description: null,
            clearDescription: false,
            maxScore: null,
            passingScore: null,
            timeLimitMinutes: null,
            clearTimeLimitMinutes: false,
            maxAttempts: null,
            isRequired: null,
            availableFrom: null,
            clearAvailableFrom: false,
            availableUntil: null,
            clearAvailableUntil: false);

        assessment.ContentId.Should().Be(contentId);
    }

    [Theory]
    [InlineData(0)]
    public void SetMaxScore_RejectsNonPositiveValues(int score)
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(1));
        var action = () => assessment.SetMaxScore(Score(score));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetDeliveryContract_RejectsUndefinedPresentationMode()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100));
        var action = () => assessment.SetDeliveryContract(SubmissionModality.Text, (AssessmentPresentationMode)999);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(SubmissionModality.None)]
    [InlineData((SubmissionModality)128)]
    public void SetDeliveryContract_RejectsUnsupportedModalities(SubmissionModality modality)
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100));
        var action = () => assessment.SetDeliveryContract(modality, AssessmentPresentationMode.SingleStep);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    public static TheoryData<DateTime?, DateTime?, DateTime?, bool, DateTime?> InvalidSchedules()
    {
        var now = DateTime.UtcNow;
        return new TheoryData<DateTime?, DateTime?, DateTime?, bool, DateTime?>
        {
            { now, now.AddMinutes(-1), null, false, null },
            { now, null, now.AddMinutes(-1), false, null },
            { null, now, now.AddMinutes(1), false, null },
            { null, null, null, true, null },
            { null, null, now, true, null },
            { null, null, now, false, now.AddMinutes(1) },
            { null, null, now, true, now },
            { null, now.AddHours(1), now, true, now.AddHours(2) }
        };
    }

    [Theory]
    [MemberData(nameof(InvalidSchedules))]
    public void SetDeliverySchedule_RejectsInvalidTimelines(
        DateTime? availableFrom,
        DateTime? availableUntil,
        DateTime? dueAt,
        bool allowLate,
        DateTime? lateDeadline)
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, Score(100));
        var action = () => assessment.SetDeliverySchedule(
            availableFrom, availableUntil, dueAt, allowLate, lateDeadline);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssessmentGroup_UpdateChangesAllValuesAndNormalizesDescription()
    {
        var group = AssessmentGroup.Create(Guid.NewGuid(), "Initial", Percent(10), description: "Initial");

        group.Update(" Updated ", "   ", Percent(25), 4);

        group.Name.Should().Be("Updated");
        group.Description.Should().BeNull();
        group.WeightPercent.Should().Be(Percent(25));
        group.Order.Should().Be(4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AssessmentGroup_UpdateRejectsBlankName(string name)
    {
        var group = AssessmentGroup.Create(Guid.NewGuid(), "Initial", Percent(10));
        var action = () => group.Update(name, null, null, null);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(100.1)]
    public void AssessmentGroup_UpdateRejectsInvalidWeight(decimal weight)
    {
        var group = AssessmentGroup.Create(Guid.NewGuid(), "Initial", Percent(10));
        var action = () => group.Update(null, null, Percent(weight), null);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AssessmentSubmission_SetPayloadRejectsEmptyAndDisallowedPayloads()
    {
        var empty = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var disallowed = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        Action emptyAction = () => empty.SetPayload(new SubmitAssessmentRequest(), SubmissionModality.Text);
        Action disallowedAction = () => disallowed.SetPayload(
            new SubmitAssessmentRequest(FilePayload: "file-id"), SubmissionModality.Text);

        emptyAction.Should().Throw<ArgumentException>();
        disallowedAction.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file")]
    public void AssessmentSubmission_SetPayloadRejectsInvalidUrlSchemes(string url)
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var action = () => submission.SetPayload(
            new SubmitAssessmentRequest(UrlPayload: url), SubmissionModality.Url);
        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    public void AssessmentSubmission_SetPayloadAcceptsHttpUrlSchemes(string url)
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.SetPayload(new SubmitAssessmentRequest(UrlPayload: url), SubmissionModality.Url);
        submission.UrlPayload.Should().Be(url);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10, 11, 5)]
    public void AssessmentSubmission_GradeRejectsInvalidAssessmentBounds(
        int maxScore,
        int passingScore,
        int score)
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();
        var action = () => submission.Grade(Score(score), Score(passingScore), Score(maxScore));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(101)]
    public void AssessmentSubmission_GradeRejectsScoreOutsideBounds(int score)
    {
        var submission = AssessmentSubmission.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.Submit();
        var action = () => submission.Grade(Score(score), Score(60), Score(100));
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AssessmentRubric_ReplaceRejectsBlankTitle()
    {
        var rubric = AssessmentRubric.Create("Initial");
        var action = () => rubric.Replace("   ");
        action.Should().Throw<ArgumentException>();
    }

    public static TheoryData<string, decimal?> InvalidCueValues() => new()
    {
        { "", null },
        { "cue", -1m },
        { "cue", 1000000000m },
        { "cue", 1.0001m }
    };

    [Theory]
    [MemberData(nameof(InvalidCueValues))]
    public void InteractiveVideoCue_CreateRejectsInvalidValues(string cueId, decimal? position)
    {
        var action = () => InteractiveVideoAssessmentCue.Create(
            Guid.NewGuid(), Guid.NewGuid(), cueId, position);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InteractiveVideoCue_CreateRejectsMissingIdentifiersAndLongCueId()
    {
        Action missingAssessment = () => InteractiveVideoAssessmentCue.Create(Guid.Empty, Guid.NewGuid(), "cue");
        Action missingContent = () => InteractiveVideoAssessmentCue.Create(Guid.NewGuid(), Guid.Empty, "cue");
        Action longCueId = () => InteractiveVideoAssessmentCue.Create(
            Guid.NewGuid(), Guid.NewGuid(), new string('x', 129));

        missingAssessment.Should().Throw<ArgumentException>();
        missingContent.Should().Throw<ArgumentException>();
        longCueId.Should().Throw<ArgumentException>();
    }
}
