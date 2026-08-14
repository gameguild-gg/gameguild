using FluentAssertions;
using Xunit;

namespace GameGuild.LaunchPad.UnitTests;

public sealed class LaunchPadEventDomainTests
{
    [Fact]
    public void Event_Should_Enforce_Applications_And_Runtime_Lifecycle()
    {
        var launchEvent = LaunchPadEvent.Create(
            Guid.NewGuid(),
            "Community launch",
            DateTime.UtcNow.AddDays(10),
            DateTime.UtcNow.AddDays(11));

        launchEvent.OpenApplications();
        launchEvent.Status.Should().Be(LaunchPadEventStatus.ApplicationsOpen);
        launchEvent.CloseApplications();
        launchEvent.Schedule();
        launchEvent.Activate();
        launchEvent.Complete();

        launchEvent.Status.Should().Be(LaunchPadEventStatus.Completed);
        var invalid = () => launchEvent.OpenApplications();
        invalid.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Application_Should_Belong_To_Project_And_Approval_Should_Be_Idempotently_Rejected()
    {
        var application = LaunchPadApplication.Submit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A playable release");

        application.StartReview();
        application.Approve(Guid.NewGuid());

        application.Status.Should().Be(LaunchPadApplicationStatus.Approved);
        application.SubmittedByUserId.Should().NotBeEmpty();
        var secondApproval = () => application.Approve(Guid.NewGuid());
        secondApproval.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Application_Should_Keep_Only_Explicit_Distinct_Submitted_Artifacts()
    {
        var assetId = Guid.NewGuid();
        var application = LaunchPadApplication.Submit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A playable release",
            [assetId, assetId, Guid.Empty]);

        application.SubmittedAssetReferenceIds.Should().Equal(assetId);
    }

    [Fact]
    public void Slot_Should_Enforce_Capacity_And_Registration_Lifecycle()
    {
        var slot = LaunchPadParticipantSlot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Audience",
            LaunchPadParticipantRole.Audience,
            1,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(2));
        var registration = LaunchPadParticipantRegistration.Register(
            Guid.NewGuid(), slot.Id, Guid.NewGuid(), waitlisted: false);

        slot.Reserve();
        var overCapacity = () => slot.Reserve();
        overCapacity.Should().Throw<InvalidOperationException>();

        registration.CheckIn();
        registration.MarkAttended();
        registration.Complete();
        registration.Status.Should().Be(LaunchPadParticipantStatus.Completed);
    }

    [Fact]
    public void Approved_Application_Should_Create_Plan_Linked_To_Event_Application_And_Version()
    {
        var applicationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var plan = LaunchPlan.CreateForApprovedApplication(
            Guid.NewGuid(), eventId, applicationId, projectId, versionId, "Launch plan");

        plan.LaunchPadEventId.Should().Be(eventId);
        plan.LaunchPadApplicationId.Should().Be(applicationId);
        plan.ProjectVersionId.Should().Be(versionId);
    }

    [Fact]
    public void Event_Slot_AndSubmittedApplication_ShouldBeEditableOnlyInCompatibleStates()
    {
        var tenantId = Guid.NewGuid();
        var startsAt = DateTime.UtcNow.AddDays(10);
        var launchEvent = LaunchPadEvent.Create(tenantId, "Original", startsAt, startsAt.AddHours(4));
        launchEvent.Update("Updated", "Description", startsAt.AddDays(1), startsAt.AddDays(1).AddHours(5),
            startsAt.AddDays(-2), startsAt.AddDays(-1));
        launchEvent.Name.Should().Be("Updated");

        var slot = LaunchPadParticipantSlot.Create(tenantId, launchEvent.Id, "Audience", LaunchPadParticipantRole.Audience,
            2, launchEvent.StartsAt, launchEvent.StartsAt.AddHours(1));
        slot.Update("Mentors", LaunchPadParticipantRole.Mentor, 3, launchEvent.StartsAt, launchEvent.StartsAt.AddHours(2));
        slot.Role.Should().Be(LaunchPadParticipantRole.Mentor);

        var application = LaunchPadApplication.Submit(tenantId, launchEvent.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pitch");
        var replacementVersionId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        application.Update(replacementVersionId, "Updated pitch", [assetId]);
        application.ProjectVersionId.Should().Be(replacementVersionId);
        application.SubmittedAssetReferenceIds.Should().Equal(assetId);
        application.StartReview();
        var editUnderReview = () => application.Update(Guid.NewGuid(), "Too late");
        editUnderReview.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Application_UpdateWithoutArtifactList_ShouldPreserveSubmittedArtifacts()
    {
        var assetId = Guid.NewGuid();
        var application = LaunchPadApplication.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pitch", [assetId]);

        application.Update(Guid.NewGuid(), "Revised pitch");

        application.SubmittedAssetReferenceIds.Should().Equal(assetId);
    }
}
