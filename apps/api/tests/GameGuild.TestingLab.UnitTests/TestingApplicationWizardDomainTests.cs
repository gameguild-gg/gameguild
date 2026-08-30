using FluentAssertions;
using GameGuild.Projects;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingApplicationWizardDomainTests
{
    [Fact]
    public void QuestionnaireSerialization_OmitsAnUnsetCondition()
    {
        var schema = new QuestionnaireSchema("Optional condition", [
            new QuestionnaireQuestion("notes", "Share your notes", QuestionnaireQuestionType.FreeText, true)
        ]);

        var json = schema.ToJson();
        var restored = QuestionnaireSchema.FromJson(json);

        json.Should().NotContain("\"condition\"");
        restored.Questions.Should().ContainSingle().Which.Condition.Should().BeNull();
    }

    [Fact]
    public void QuestionnaireValidation_EnforcesOptionsRequiredFieldsAndConditions()
    {
        var schema = new QuestionnaireSchema("Conditional", [
            new QuestionnaireQuestion(
                "played",
                "Did you play?",
                QuestionnaireQuestionType.SingleChoice,
                true,
                [new QuestionnaireOption("yes", "Yes"), new QuestionnaireOption("no", "No")]),
            new QuestionnaireQuestion(
                "details",
                "What happened?",
                QuestionnaireQuestionType.FreeText,
                true,
                Condition: new QuestionnaireCondition("played", QuestionnaireConditionOperator.Equals, "yes"))
        ]);

        var hiddenRequired = new QuestionnaireResponse([
            new QuestionnaireAnswer("played", SelectedOptionIds: ["no"])
        ]);
        var missingConditional = new QuestionnaireResponse([
            new QuestionnaireAnswer("played", SelectedOptionIds: ["yes"])
        ]);
        var invalidOption = new QuestionnaireResponse([
            new QuestionnaireAnswer("played", SelectedOptionIds: ["maybe"])
        ]);

        QuestionnaireResponseValidator.Validate(schema, hiddenRequired).Should().BeEmpty();
        QuestionnaireResponseValidator.Validate(schema, missingConditional).Should().ContainSingle();
        QuestionnaireResponseValidator.Validate(schema, invalidOption).Should().Contain(error => error.Contains("allowed option"));
    }

    [Fact]
    public void ApplicationDraft_SubmitsOnlyCompletePackageAndFreezesUnderReview()
    {
        var application = TestingProjectApplication.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var versionId = Guid.NewGuid();
        var brief = ValidBrief();
        var eventResponses = new QuestionnaireResponse([]);
        var feedbackSchema = new QuestionnaireSchema("Developer feedback", [
            new QuestionnaireQuestion("fun", "What was fun?", QuestionnaireQuestionType.FreeText, true)
        ]);
        var revision = TestingQuestionnaireRevision.Create(
            application.Id, 1, feedbackSchema, application.SubmittedByUserId, application.TenantId);

        var incompleteSubmit = () => application.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview);
        incompleteSubmit.Should().Throw<InvalidOperationException>();

        application.UpdateDraftPackage(versionId, brief, eventResponses, true, []);
        application.UseQuestionnaireRevision(revision);
        application.SubmitDraft(VersionSubmissionPolicy.ReadyMutableUntilReview);
        application.BeginReview();
        var frozenEdit = () => application.UpdateDraftPackage(versionId, brief, eventResponses, true, []);

        application.Status.Should().Be(TestingApplicationStatus.UnderReview);
        application.CurrentQuestionnaireRevisionId.Should().Be(revision.Id);
        application.RulesAcceptedAt.Should().NotBeNull();
        frozenEdit.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QuestionnaireRevisionAndFeedbackObligation_FixTheExpectedRevision()
    {
        var applicationId = Guid.NewGuid();
        var first = TestingQuestionnaireRevision.Create(
            applicationId,
            1,
            new QuestionnaireSchema("First", [
                new QuestionnaireQuestion("q1", "First question", QuestionnaireQuestionType.FreeText, true)
            ]),
            Guid.NewGuid(),
            Guid.NewGuid());
        var second = TestingQuestionnaireRevision.Create(
            applicationId,
            2,
            new QuestionnaireSchema("Second", [
                new QuestionnaireQuestion("q2", "Second question", QuestionnaireQuestionType.FreeText, true)
            ]),
            Guid.NewGuid(),
            first.TenantId);
        var obligation = TestingFeedbackObligation.Create(
            Guid.NewGuid(), Guid.NewGuid(), applicationId, Guid.NewGuid(), first.TenantId, first.Id);

        first.Schema.Questions.Should().ContainSingle(question => question.Id == "q1");
        second.Schema.Questions.Should().ContainSingle(question => question.Id == "q2");
        obligation.QuestionnaireRevisionId.Should().Be(first.Id);
    }

    [Fact]
    public void StructuredFeedback_RequiresQuestionnaireMetricsAndStableAnswers()
    {
        var revisionId = Guid.NewGuid();
        var schema = new QuestionnaireSchema("Developer feedback", [
            new QuestionnaireQuestion("difficulty", "What felt difficult?", QuestionnaireQuestionType.FreeText, true)
        ]);
        var responses = new QuestionnaireResponse([
            new QuestionnaireAnswer("difficulty", TextValue: "The first boss")
        ]);

        var feedback = TestingFeedback.CreateStructuredForEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestingContext.Online,
            revisionId,
            schema,
            responses,
            8,
            true,
            "Clear controls",
            Guid.NewGuid());

        feedback.QuestionnaireRevisionId.Should().Be(revisionId);
        feedback.StructuredResponses.Should().BeEquivalentTo(responses);
        feedback.OverallRating.Should().Be(8);
        feedback.WouldRecommend.Should().BeTrue();

        var missingMetric = () => TestingFeedback.CreateStructuredForEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestingContext.Online,
            revisionId, schema, responses, null, true, null, Guid.NewGuid());
        missingMetric.Should().Throw<ArgumentException>();
    }

    private static TestingProjectBrief ValidBrief() => new(
        "Find usability blockers",
        "Download and sign in with the test account",
        ["Finish the tutorial", "Complete one match"],
        "Keyboard and mouse",
        "Matchmaking may take up to two minutes",
        ["https://example.com/build"]);
}
