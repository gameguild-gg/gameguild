using System.Reflection;
using FluentAssertions;
using GameGuild.Projects;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabEventApplicationEdgeCoverageTests
{
    private static readonly QuestionnaireSchema Schema = new("Form", []);

    [Fact]
    public void EventConfiguration_CoversEmptySchemaAndTenantMismatchPaths()
    {
        var testingEvent = Event(Guid.NewGuid());
        testingEvent.ProjectApplicationSchema.Should().BeNull();
        testingEvent.TesterRegistrationSchema.Should().BeNull();

        var otherTenantTemplate = TestingEventTemplate.Create(
            Guid.NewGuid(), "Template", "Rules", "Candidates", "Testers", Schema, Schema,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, Guid.NewGuid());
        Invoking(() => testingEvent.ConfigureFromTemplate(otherTenantTemplate.CurrentRevision))
            .Should().Throw<InvalidOperationException>().WithMessage("*event tenant*");

        Set(testingEvent, nameof(TestingEvent.GeneralRules), "Rules");
        Set(testingEvent, nameof(TestingEvent.CandidateInstructions), "Candidates");
        Set(testingEvent, nameof(TestingEvent.TesterInstructions), "Testers");
        Set(testingEvent, nameof(TestingEvent.ProjectApplicationSchemaJson), Schema.ToJson());
        Set(testingEvent, nameof(TestingEvent.TesterRegistrationSchemaJson), null);
        Invoking(testingEvent.OpenApplications).Should().Throw<InvalidOperationException>()
            .WithMessage("*registration schemas*");
    }

    [Fact]
    public void PendingDraftUpdates_CoverNullSameAndMutableVersionBranches()
    {
        var current = Guid.NewGuid();
        var pending = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), current, Guid.NewGuid(), null, Guid.NewGuid());

        pending.UpdateDraftPackage(null, null, null, null, null);
        pending.ProjectVersionId.Should().Be(current);
        pending.UpdateDraftPackage(current, null, null, null, null);
        pending.ProjectVersionId.Should().Be(current);

        foreach (var policy in Enum.GetValues<VersionSubmissionPolicy>())
        {
            var application = TestingProjectApplication.Submit(
                Guid.NewGuid(), Guid.NewGuid(), current, Guid.NewGuid(), null, Guid.NewGuid(),
                submissionVersionPolicy: policy);
            var replacement = Guid.NewGuid();
            if (ProjectVersionEligibility.CanReplaceAfterSubmission(policy))
            {
                application.UpdateSubmission(replacement, null);
                application.ProjectVersionId.Should().Be(replacement);
            }
            else
            {
                Invoking(() => application.UpdateSubmission(replacement, null))
                    .Should().Throw<InvalidOperationException>();
            }
        }
    }

    [Fact]
    public void QuestionnaireRevision_AllowsMatchingGlobalScopeAndRejectsCorruptAssignedSlot()
    {
        var global = TestingProjectApplication.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        var revision = TestingQuestionnaireRevision.Create(
            global.Id, 1, Schema, Guid.NewGuid(), null);

        global.UseQuestionnaireRevision(revision);
        global.CurrentQuestionnaireRevisionId.Should().Be(revision.Id);

        var corrupt = TestingProjectApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());
        Set(corrupt, nameof(TestingProjectApplication.AssignedSlotId), Guid.NewGuid());
        Invoking(() => corrupt.Approve(Guid.NewGuid(), Guid.NewGuid(), null))
            .Should().Throw<InvalidOperationException>().WithMessage("*already has an assigned slot*");
    }

    private static TestingEvent Event(Guid tenantId)
    {
        var startsAt = SystemClock.UtcNow.AddDays(3);
        return TestingEvent.Create(
            "Event", TestingEventMode.Online, Guid.NewGuid(), startsAt.AddDays(-2),
            startsAt.AddDays(-1), startsAt, startsAt.AddHours(2), true,
            TestingEventApprovalMode.ManagerOnly, tenantId);
    }

    private static void Set<T>(T target, string property, object? value) where T : class =>
        typeof(T).GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(target, value);

    private static Action Invoking(Action action) => action;
}
