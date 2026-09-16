using System.Reflection;
using FluentAssertions;
using GameGuild.Projects;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingProjectApplicationCoverageTests
{
    [Fact]
    public void Factories_ValidateActorsNormalizeAssetsAndInitializeExpectedState()
    {
        Invoking(() => TestingProjectApplication.CreateDraft(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingProjectApplication.CreateDraft(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingProjectApplication.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingProjectApplication.Submit(Guid.Empty, Guid.NewGuid(), null, Guid.NewGuid(), null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingProjectApplication.Submit(Guid.NewGuid(), Guid.Empty, null, Guid.NewGuid(), null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingProjectApplication.Submit(Guid.NewGuid(), Guid.NewGuid(), null, Guid.Empty, null, null))
            .Should().Throw<ArgumentException>();

        var firstAsset = Guid.NewGuid();
        var secondAsset = Guid.NewGuid();
        var submitted = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  Weekends  ", Guid.NewGuid(),
            [Guid.Empty, firstAsset, firstAsset, secondAsset]);

        submitted.PreferredAvailability.Should().Be("Weekends");
        submitted.SubmittedAssetReferenceIds.Should().Equal(firstAsset, secondAsset);
        submitted.Status.Should().Be(TestingApplicationStatus.Pending);

        var draft = TestingProjectApplication.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        draft.Status.Should().Be(TestingApplicationStatus.Draft);
        draft.SubmittedAssetReferenceIds.Should().BeEmpty();
        draft.Brief.Should().BeNull();
        draft.EventApplicationResponse.Should().BeNull();

        typeof(TestingProjectApplication).GetProperty(nameof(TestingProjectApplication.SubmittedAssetReferenceIdsJson))!
            .SetValue(draft, "null");
        draft.SubmittedAssetReferenceIds.Should().BeEmpty();
    }

    [Fact]
    public void AssetReferences_AreBoundedToOneHundredDistinctNonEmptyIds()
    {
        var ids = Enumerable.Range(0, 110).Select(_ => Guid.NewGuid()).ToList();
        ids.Insert(0, Guid.Empty);
        ids.Add(ids[1]);

        var application = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), " ", null, ids);

        application.SubmittedAssetReferenceIds.Should().HaveCount(100);
        application.SubmittedAssetReferenceIds.Should().OnlyHaveUniqueItems();
        application.PreferredAvailability.Should().BeNull();
    }

    [Fact]
    public void UpdateDraftPackage_MapsOptionalPackageAndRulesBranches()
    {
        var application = NewDraft();
        var versionId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var brief = ValidBrief();
        var response = new QuestionnaireResponse([]);

        Invoking(() => application.UpdateDraftPackage(Guid.Empty, null, null, null, null))
            .Should().Throw<ArgumentException>();

        application.UpdateDraftPackage(versionId, brief, response, true, [assetId], "  Friday  ");
        var acceptedAt = application.RulesAcceptedAt;
        application.ProjectVersionId.Should().Be(versionId);
        application.Brief.Should().BeEquivalentTo(brief);
        application.EventApplicationResponse.Should().BeEquivalentTo(response);
        application.RulesAcceptedAt.Should().NotBeNull();
        application.SubmittedAssetReferenceIds.Should().Equal(assetId);
        application.PreferredAvailability.Should().Be("Friday");

        application.UpdateDraftPackage(null, null, null, true, null, " ");
        application.RulesAcceptedAt.Should().Be(acceptedAt);
        application.SubmittedAssetReferenceIds.Should().BeEmpty();
        application.PreferredAvailability.Should().BeNull();

        application.UpdateDraftPackage(null, null, null, false, null);
        application.RulesAcceptedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateDraftPackage_EnforcesPendingVersionPolicyAndFrozenState()
    {
        var immutable = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(),
            submissionVersionPolicy: VersionSubmissionPolicy.ReleasedImmutable);
        Invoking(() => immutable.UpdateDraftPackage(Guid.NewGuid(), null, null, null, null))
            .Should().Throw<InvalidOperationException>();

        var mutable = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());
        var replacement = Guid.NewGuid();
        mutable.UpdateDraftPackage(replacement, null, null, null, null);
        mutable.ProjectVersionId.Should().Be(replacement);

        mutable.BeginReview();
        Invoking(() => mutable.UpdateDraftPackage(null, null, null, null, null))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QuestionnaireRevision_MustBelongToDraftOrPendingApplicationAndTenant()
    {
        var application = NewDraft();
        var valid = Revision(application);

        Invoking(() => application.UseQuestionnaireRevision(null!)).Should().Throw<ArgumentNullException>();
        Invoking(() => application.UseQuestionnaireRevision(
            TestingQuestionnaireRevision.Create(Guid.NewGuid(), 1, Schema(), Guid.NewGuid(), application.TenantId)))
            .Should().Throw<InvalidOperationException>();
        Invoking(() => application.UseQuestionnaireRevision(
            TestingQuestionnaireRevision.Create(application.Id, 1, Schema(), Guid.NewGuid(), Guid.NewGuid())))
            .Should().Throw<InvalidOperationException>();

        application.UseQuestionnaireRevision(valid);
        application.UseQuestionnaireRevision(valid);
        application.QuestionnaireRevisions.Should().ContainSingle();
        application.CurrentQuestionnaireRevisionId.Should().Be(valid.Id);

        PopulateDraft(application);
        application.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview);
        application.BeginReview();
        Invoking(() => application.UseQuestionnaireRevision(valid)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SubmitDraft_RequiresEachPackageComponentAndDraftState()
    {
        var missingVersion = NewDraft();
        Invoking(() => missingVersion.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview))
            .Should().Throw<InvalidOperationException>();

        var missingBrief = NewDraft();
        missingBrief.UpdateDraftPackage(Guid.NewGuid(), null, new QuestionnaireResponse([]), true, null);
        missingBrief.UseQuestionnaireRevision(Revision(missingBrief));
        Invoking(() => missingBrief.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview))
            .Should().Throw<InvalidOperationException>();

        var missingResponse = NewDraft();
        missingResponse.UpdateDraftPackage(Guid.NewGuid(), ValidBrief(), null, true, null);
        missingResponse.UseQuestionnaireRevision(Revision(missingResponse));
        Invoking(() => missingResponse.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview))
            .Should().Throw<InvalidOperationException>();

        var missingRules = NewDraft();
        missingRules.UpdateDraftPackage(Guid.NewGuid(), ValidBrief(), new QuestionnaireResponse([]), false, null);
        missingRules.UseQuestionnaireRevision(Revision(missingRules));
        Invoking(() => missingRules.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview))
            .Should().Throw<InvalidOperationException>();

        var missingRevision = NewDraft();
        missingRevision.UpdateDraftPackage(Guid.NewGuid(), ValidBrief(), new QuestionnaireResponse([]), true, null);
        Invoking(() => missingRevision.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview))
            .Should().Throw<InvalidOperationException>();

        var complete = NewDraft();
        PopulateDraft(complete);
        complete.SubmitDraft(VersionSubmissionPolicy.ReleasedImmutable);
        complete.Status.Should().Be(TestingApplicationStatus.Pending);
        complete.SubmissionVersionPolicy.Should().Be(VersionSubmissionPolicy.ReleasedImmutable);
        Invoking(() => complete.SubmitDraft(VersionSubmissionPolicy.ReleasedImmutable))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateSubmission_ValidatesStatusIdentifierAndVersionPolicy()
    {
        var currentVersion = Guid.NewGuid();
        var mutable = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), currentVersion, Guid.NewGuid(), null, Guid.NewGuid());
        var replacement = Guid.NewGuid();
        var asset = Guid.NewGuid();

        Invoking(() => mutable.UpdateSubmission(Guid.Empty, null)).Should().Throw<ArgumentException>();
        mutable.UpdateSubmission(replacement, "  Morning  ", [asset]);
        mutable.ProjectVersionId.Should().Be(replacement);
        mutable.PreferredAvailability.Should().Be("Morning");
        mutable.SubmittedAssetReferenceIds.Should().Equal(asset);
        mutable.UpdateSubmission(replacement, " ");
        mutable.PreferredAvailability.Should().BeNull();

        var immutable = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), currentVersion, Guid.NewGuid(), null, Guid.NewGuid(),
            submissionVersionPolicy: VersionSubmissionPolicy.ReleasedImmutable);
        Invoking(() => immutable.UpdateSubmission(Guid.NewGuid(), null))
            .Should().Throw<InvalidOperationException>();
        immutable.UpdateSubmission(currentVersion, null);

        mutable.BeginReview();
        Invoking(() => mutable.UpdateSubmission(replacement, null)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Decisions_ValidateStateActorsSlotsAndRationales()
    {
        var pending = NewPending();
        Invoking(() => pending.Approve(Guid.Empty, Guid.NewGuid(), null)).Should().Throw<ArgumentException>();
        Invoking(() => pending.Approve(Guid.NewGuid(), Guid.Empty, null)).Should().Throw<ArgumentException>();
        Invoking(() => pending.Reject(Guid.Empty, "No")).Should().Throw<ArgumentException>();
        Invoking(() => pending.Reject(Guid.NewGuid(), " ")).Should().Throw<ArgumentException>();
        Invoking(() => pending.ReassignSlot(Guid.NewGuid())).Should().Throw<InvalidOperationException>();

        pending.PlaceOnWaitlist(Guid.NewGuid(), "  Needs capacity  ");
        pending.Status.Should().Be(TestingApplicationStatus.Waitlisted);
        pending.DecisionRationale.Should().Be("Needs capacity");
        pending.Approve(Guid.NewGuid(), Guid.NewGuid(), " ");
        pending.DecisionRationale.Should().BeNull();
        Invoking(() => pending.Approve(Guid.NewGuid(), Guid.NewGuid(), null)).Should().Throw<InvalidOperationException>();
        Invoking(() => pending.ReassignSlot(Guid.Empty)).Should().Throw<ArgumentException>();
        pending.ReassignSlot(Guid.NewGuid());
        Invoking(pending.Withdraw).Should().Throw<InvalidOperationException>();

        var reviewed = NewPending();
        reviewed.BeginReview();
        reviewed.PlaceOnWaitlist(Guid.NewGuid(), null);
        reviewed.DecisionRationale.Should().BeNull();
        reviewed.Reject(Guid.NewGuid(), "  Not ready  ");
        reviewed.Status.Should().Be(TestingApplicationStatus.Rejected);
        reviewed.DecisionRationale.Should().Be("Not ready");
        Invoking(() => reviewed.Reject(Guid.NewGuid(), "Again")).Should().Throw<InvalidOperationException>();
        Invoking(reviewed.Withdraw).Should().Throw<InvalidOperationException>();

        var withdrawn = NewPending();
        withdrawn.Withdraw();
        withdrawn.Status.Should().Be(TestingApplicationStatus.Withdrawn);
        Invoking(withdrawn.BeginReview).Should().Throw<InvalidOperationException>();
        Invoking(() => withdrawn.PlaceOnWaitlist(Guid.NewGuid(), null)).Should().Throw<InvalidOperationException>();
        Invoking(() => withdrawn.Approve(Guid.NewGuid(), Guid.NewGuid(), null)).Should().Throw<InvalidOperationException>();
    }

    private static TestingProjectApplication NewDraft() => TestingProjectApplication.CreateDraft(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static TestingProjectApplication NewPending() => TestingProjectApplication.Submit(
        Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), null, Guid.NewGuid());

    private static void PopulateDraft(TestingProjectApplication application)
    {
        application.UpdateDraftPackage(
            Guid.NewGuid(), ValidBrief(), new QuestionnaireResponse([]), true, null);
        application.UseQuestionnaireRevision(Revision(application));
    }

    private static TestingQuestionnaireRevision Revision(TestingProjectApplication application) =>
        TestingQuestionnaireRevision.Create(
            application.Id, 1, Schema(), application.SubmittedByUserId, application.TenantId);

    private static QuestionnaireSchema Schema() => new("Feedback", []);

    private static TestingProjectBrief ValidBrief() => new(
        "Find defects", "Install the build", ["Complete the tutorial"],
        "Keyboard", "Known test limitation", ["https://example.test/build"]);

    private static Action Invoking(Action action) => action;
}

public sealed class TestingFeedbackCoverageTests
{
    [Fact]
    public void CreateForEvent_ValidatesRequiredIdentifiersContentAndRating()
    {
        var eventId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var testerId = Guid.NewGuid();

        Invoking(() => Create(Guid.Empty, applicationId, testerId, "{}", 5)).Should().Throw<ArgumentException>();
        Invoking(() => Create(eventId, Guid.Empty, testerId, "{}", 5)).Should().Throw<ArgumentException>();
        Invoking(() => Create(eventId, applicationId, Guid.Empty, "{}", 5)).Should().Throw<ArgumentException>();
        Invoking(() => Create(eventId, applicationId, testerId, " ", 5)).Should().Throw<ArgumentException>();
        Invoking(() => Create(eventId, applicationId, testerId, "{}", 0)).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Create(eventId, applicationId, testerId, "{}", 11)).Should().Throw<ArgumentOutOfRangeException>();

        var feedback = TestingFeedback.CreateForEvent(
            eventId, applicationId, testerId, TestingContext.Online, "  {}  ", null, null, " ", null);
        feedback.FeedbackData.Should().Be("{}");
        feedback.AdditionalNotes.Should().BeNull();
        feedback.IsGlobal.Should().BeTrue();
        feedback.OverallRating.Should().BeNull();
    }

    [Fact]
    public void CreateStructuredForEvent_RequiresRevisionMetricsAndValidAnswers()
    {
        var schema = new QuestionnaireSchema("Feedback", [
            new QuestionnaireQuestion("notes", "Notes", QuestionnaireQuestionType.FreeText, true)
        ]);
        var response = new QuestionnaireResponse([
            new QuestionnaireAnswer("notes", TextValue: "Works")
        ]);
        Func<Guid, int?, bool?, QuestionnaireResponse, TestingFeedback> create =
            (revisionId, rating, recommend, answers) => TestingFeedback.CreateStructuredForEvent(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestingContext.InPerson,
                revisionId, schema, answers, rating, recommend, "  Useful  ", Guid.NewGuid());

        Invoking(() => create(Guid.Empty, 8, true, response)).Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), null, true, response)).Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), 0, true, response)).Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), 11, true, response)).Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), 8, null, response)).Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), 8, true, new QuestionnaireResponse([])))
            .Should().Throw<ArgumentException>();

        var feedback = create(Guid.NewGuid(), 8, false, response);
        feedback.StructuredResponses.Should().BeEquivalentTo(response);
        feedback.AdditionalNotes.Should().Be("Useful");
        feedback.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void ComputedProperties_CoverPositiveNegativeAndAverageQualityBranches()
    {
        var feedback = new TestingFeedback { OverallRating = 8, WouldRecommend = true };
        feedback.IsPositive.Should().BeTrue();
        feedback.IsNegative.Should().BeFalse();

        feedback.OverallRating = 5;
        feedback.WouldRecommend = false;
        feedback.IsPositive.Should().BeFalse();
        feedback.IsNegative.Should().BeTrue();

        feedback.OverallRating = 3;
        feedback.WouldRecommend = true;
        feedback.IsNegative.Should().BeTrue();
        feedback.QualityRatings = null!;
        feedback.AverageQualityRating.Should().BeNull();
        feedback.QualityRatings = [];
        feedback.AverageQualityRating.Should().BeNull();
        feedback.QualityRatings = [
            new FeedbackQualityRating { QualityRating = 2 },
            new FeedbackQualityRating { QualityRating = 4 }
        ];
        feedback.AverageQualityRating.Should().Be(3m);
    }

    [Fact]
    public void MutationMethods_UpdateModerationNotesRecommendationAndQuality()
    {
        var feedback = new TestingFeedback();
        var reporterId = Guid.NewGuid();

        feedback.ReportedByUserId = reporterId;
        feedback.ReportedByUserId.Should().Be(reporterId);
        feedback.UpdateNotes("Notes");
        feedback.SetRecommendation(false);
        feedback.SetQualityRating(FeedbackQuality.High);
        feedback.Report(reporterId, "Reason");
        feedback.AdditionalNotes.Should().Be("Notes");
        feedback.WouldRecommend.Should().BeFalse();
        feedback.QualityRating.Should().Be(FeedbackQuality.High);
        feedback.IsReported.Should().BeTrue();

        feedback.Unreport();
        feedback.IsReported.Should().BeFalse();
        feedback.ReportedById.Should().BeNull();
        feedback.ReportReason.Should().BeNull();
        feedback.ReportedAt.Should().BeNull();

        feedback.UpdateNotes(null);
        feedback.AdditionalNotes.Should().BeNull();
    }

    [Fact]
    public void FeedbackQualityRating_ValidatesRangeAndComputesPolarityAndScope()
    {
        var rating = new FeedbackQualityRating { QualityRating = 3 };
        rating.IsGlobal.Should().BeTrue();
        rating.IsPositive.Should().BeFalse();
        rating.IsNegative.Should().BeFalse();

        rating.UpdateRating(4, "  useful  ");
        rating.QualityRating.Should().Be(4);
        rating.Reason.Should().Be("  useful  ");
        rating.IsPositive.Should().BeTrue();
        rating.IsNegative.Should().BeFalse();

        rating.TenantId = Guid.NewGuid();
        rating.UpdateRating(2);
        rating.IsGlobal.Should().BeFalse();
        rating.IsPositive.Should().BeFalse();
        rating.IsNegative.Should().BeTrue();
        Invoking(() => rating.UpdateRating(0)).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => rating.UpdateRating(6)).Should().Throw<ArgumentOutOfRangeException>();
    }

    private static TestingFeedback Create(
        Guid eventId, Guid applicationId, Guid testerId, string data, int? rating) =>
        TestingFeedback.CreateForEvent(
            eventId, applicationId, testerId, TestingContext.Online, data,
            rating, true, null, Guid.NewGuid());

    private static Action Invoking(Action action) => action;
}
