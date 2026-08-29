using FluentAssertions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventTemplateTests
{
    private static readonly QuestionnaireSchema ProjectForm = new(
        "Project application",
        [new QuestionnaireQuestion("project-goal", "What should be tested?", QuestionnaireQuestionType.FreeText, true)]);

    private static readonly QuestionnaireSchema TesterForm = new(
        "Tester registration",
        [new QuestionnaireQuestion("experience", "Experience level", QuestionnaireQuestionType.SingleChoice, true,
            [new QuestionnaireOption("beginner", "Beginner"), new QuestionnaireOption("advanced", "Advanced")])]);

    [Fact]
    public void TemplateRevision_IsImmutableAndIndependentFromFutureRevisions()
    {
        var template = TestingEventTemplate.Create(
            Guid.NewGuid(),
            "Standard playtest",
            "General rules v1",
            "Candidate instructions v1",
            "Tester instructions v1",
            ProjectForm,
            TesterForm,
            TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly,
            true,
            Guid.NewGuid());

        var first = template.CurrentRevision;
        var second = template.CreateRevision(
            "General rules v2",
            "Candidate instructions v2",
            "Tester instructions v2",
            ProjectForm,
            TesterForm,
            TestingEventMode.Hybrid,
            TestingEventApprovalMode.Committee,
            false,
            Guid.NewGuid());

        first.RevisionNumber.Should().Be(1);
        first.GeneralRules.Should().Be("General rules v1");
        second.RevisionNumber.Should().Be(2);
        second.GeneralRules.Should().Be("General rules v2");
        template.CurrentRevision.Should().BeSameAs(second);
    }

    [Fact]
    public void ArchivedTemplate_RejectsNewRevision()
    {
        var template = TestingEventTemplate.Create(
            Guid.NewGuid(), "Archived", "Rules", "Candidates", "Testers", ProjectForm, TesterForm,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, Guid.NewGuid());
        template.Archive();

        var act = () => template.CreateRevision(
            "New rules", "Candidates", "Testers", ProjectForm, TesterForm,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EventConfiguration_MustBeCompleteBeforeOpeningAndFreezesAsIndependentSnapshot()
    {
        var template = TestingEventTemplate.Create(
            Guid.NewGuid(), "Standard", "Rules", "Candidates", "Testers", ProjectForm, TesterForm,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, Guid.NewGuid());
        var testingEvent = TestingEvent.Create(
            "Event",
            TestingEventMode.Online,
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(4),
            true,
            TestingEventApprovalMode.ManagerOnly,
            template.TenantId);

        var openBlank = () => testingEvent.OpenApplications();
        openBlank.Should().Throw<InvalidOperationException>();

        testingEvent.ConfigureFromTemplate(template.CurrentRevision);
        testingEvent.OpenApplications();
        var editFrozen = () => testingEvent.Configure(
            "Changed", "Candidates", "Testers", ProjectForm, TesterForm);

        testingEvent.SourceTemplateId.Should().Be(template.Id);
        testingEvent.SourceTemplateRevisionId.Should().Be(template.CurrentRevision.Id);
        testingEvent.GeneralRules.Should().Be("Rules");
        testingEvent.ConfigurationFrozenAt.Should().NotBeNull();
        editFrozen.Should().Throw<InvalidOperationException>();

        template.CreateRevision(
            "New template rules", "Candidates", "Testers", ProjectForm, TesterForm,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, Guid.NewGuid());
        testingEvent.GeneralRules.Should().Be("Rules");
    }
}
