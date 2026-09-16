using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Projects;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabContractCoverageTests
{
    [Fact]
    public void RequestAndEventUpdateContracts_PreserveEveryInput()
    {
        var id = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var open = SystemClock.UtcNow.AddDays(1);
        var close = open.AddDays(1);
        var start = close.AddDays(1);
        var end = start.AddHours(2);
        var request = new UpdateTestingRequestCommand(
            id, "Title", "Description", "https://example.test/build", InstructionType.File,
            "Instructions", "https://example.test/instructions", fileId, "Feedback", 4,
            start, end, true);

        request.Id.Should().Be(id);
        request.Title.Should().Be("Title");
        request.Description.Should().Be("Description");
        request.DownloadUrl.Should().Be("https://example.test/build");
        request.InstructionsType.Should().Be(InstructionType.File);
        request.InstructionsContent.Should().Be("Instructions");
        request.InstructionsUrl.Should().Be("https://example.test/instructions");
        request.InstructionsFileId.Should().Be(fileId);
        request.FeedbackFormContent.Should().Be("Feedback");
        request.MaxTesters.Should().Be(4);
        request.StartDate.Should().Be(start);
        request.EndDate.Should().Be(end);
        request.IsActive.Should().BeTrue();

        var update = new UpdateTestingEventRequest(
            "Event", "Description", TestingEventMode.Hybrid, TestingEventApprovalMode.Committee,
            open, close, start, end, true, "America/Sao_Paulo");
        update.Name.Should().Be("Event");
        update.Description.Should().Be("Description");
        update.Mode.Should().Be(TestingEventMode.Hybrid);
        update.ApprovalMode.Should().Be(TestingEventApprovalMode.Committee);
        update.ApplicationsOpenAt.Should().Be(open);
        update.ApplicationsCloseAt.Should().Be(close);
        update.StartsAt.Should().Be(start);
        update.EndsAt.Should().Be(end);
        update.RequiresFeedback.Should().BeTrue();
        update.TimeZoneId.Should().Be("America/Sao_Paulo");

        var learning = new ConfigureTestingEventLearningRequest(
            id, fileId, Guid.NewGuid(), TestingLearningCompletionRequirement.Attendance);
        learning.CourseId.Should().Be(id);
        learning.CohortId.Should().Be(fileId);
        learning.LearningActivityId.Should().NotBeEmpty();
        learning.Requirement.Should().Be(TestingLearningCompletionRequirement.Attendance);
    }

    [Fact]
    public void SlotContracts_PreserveEveryInput()
    {
        var eventId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var start = SystemClock.UtcNow.AddDays(2);
        var end = start.AddHours(2);
        var command = new UpdateTestingEventSlotCommand(
            eventId, slotId, TestingEventMode.InPerson, start, end, 8, 3,
            "Campus", "Room", null, locationId);

        command.EventId.Should().Be(eventId);
        command.SlotId.Should().Be(slotId);
        command.Mode.Should().Be(TestingEventMode.InPerson);
        command.StartsAt.Should().Be(start);
        command.EndsAt.Should().Be(end);
        command.MaxTesters.Should().Be(8);
        command.MaxProjects.Should().Be(3);
        command.CampusName.Should().Be("Campus");
        command.RoomName.Should().Be("Room");
        command.MeetingUrl.Should().BeNull();
        command.LocationId.Should().Be(locationId);

        var request = new UpsertTestingEventSlotRequest(
            TestingEventMode.Online, start, end, null, null, null, null,
            "https://example.test/meeting", locationId);
        request.Mode.Should().Be(TestingEventMode.Online);
        request.StartsAt.Should().Be(start);
        request.EndsAt.Should().Be(end);
        request.MaxTesters.Should().BeNull();
        request.MaxProjects.Should().BeNull();
        request.CampusName.Should().BeNull();
        request.RoomName.Should().BeNull();
        request.MeetingUrl.Should().Be("https://example.test/meeting");
        request.LocationId.Should().Be(locationId);
    }

    [Fact]
    public void ApplicationTransportContracts_PreserveEveryInput()
    {
        var projectId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var brief = new TestingProjectBrief(
            "Objective", "Install", ["Task"], "Controls", "Limitations");
        var schema = new QuestionnaireSchema("Form", []);
        var response = new QuestionnaireResponse([]);
        var submit = new SubmitTestingProjectApplicationRequest(
            projectId, versionId, "Morning", [assetId], brief, schema, response, true);

        submit.ProjectId.Should().Be(projectId);
        submit.ProjectVersionId.Should().Be(versionId);
        submit.PreferredAvailability.Should().Be("Morning");
        submit.SubmittedAssetReferenceIds.Should().Equal(assetId);
        submit.Brief.Should().BeSameAs(brief);
        submit.FeedbackQuestionnaire.Should().BeSameAs(schema);
        submit.EventApplicationResponse.Should().BeSameAs(response);
        submit.AcceptedRules.Should().BeTrue();

        var save = new SaveTestingProjectApplicationDraftRequest(
            versionId, brief, schema, response, true, "Evening", [assetId]);
        save.ProjectVersionId.Should().Be(versionId);
        save.Brief.Should().BeSameAs(brief);
        save.FeedbackQuestionnaire.Should().BeSameAs(schema);
        save.EventApplicationResponse.Should().BeSameAs(response);
        save.AcceptedRules.Should().BeTrue();
        save.PreferredAvailability.Should().Be("Evening");
        save.SubmittedAssetReferenceIds.Should().Equal(assetId);

        var update = new UpdateTestingProjectApplicationRequest(versionId, "Weekend", [assetId]);
        update.ProjectVersionId.Should().Be(versionId);
        update.PreferredAvailability.Should().Be("Weekend");
        update.SubmittedAssetReferenceIds.Should().Equal(assetId);

        var vote = new CastTestingApplicationVoteRequest(TestingApplicationVoteDecision.Approve, "Ready");
        vote.Decision.Should().Be(TestingApplicationVoteDecision.Approve);
        vote.Comments.Should().Be("Ready");
    }

    [Fact]
    public void ReviewPackageContracts_PreserveVersionAndAssetMetadata()
    {
        var assetId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var asset = new TestingApplicationReviewAssetProjection(
            assetId, "Build", "application/zip", "https://example.test/access", expiresAt);
        var brief = new TestingProjectBrief(
            "Objective", "Install", ["Task"], "Controls", "Limitations");
        var schema = new QuestionnaireSchema("Feedback", []);
        var package = new TestingApplicationReviewPackageProjection(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "1.0.0",
            ProjectVersionStatus.ReadyForTesting, "Notes", [asset], brief, schema);

        asset.AssetReferenceId.Should().Be(assetId);
        asset.DisplayName.Should().Be("Build");
        asset.MimeType.Should().Be("application/zip");
        asset.AccessUrl.Should().Be("https://example.test/access");
        asset.ExpiresAt.Should().Be(expiresAt);
        package.ApplicationId.Should().NotBeEmpty();
        package.ProjectId.Should().NotBeEmpty();
        package.ProjectVersionId.Should().NotBeEmpty();
        package.VersionNumber.Should().Be("1.0.0");
        package.VersionStatus.Should().Be(ProjectVersionStatus.ReadyForTesting);
        package.ReleaseNotes.Should().Be("Notes");
        package.Assets.Should().ContainSingle().Which.Should().BeSameAs(asset);
        package.Brief.Should().BeSameAs(brief);
        package.FeedbackQuestionnaire.Should().BeSameAs(schema);
    }

    [Fact]
    public void PublicSlotProjection_ClampsCapacityAndSupportsUnlimitedValues()
    {
        var start = SystemClock.UtcNow.AddDays(1);
        var limited = new PublicTestingEventSlotProjection(
            Guid.NewGuid(), Guid.NewGuid(), TestingEventMode.Online, start, start.AddHours(1),
            2, 1, null, null, 3, 4);
        limited.AvailableTesterCount.Should().Be(0);
        limited.AvailableProjectCount.Should().Be(0);

        var unlimited = limited with { MaxTesters = null, MaxProjects = null };
        unlimited.AvailableTesterCount.Should().BeNull();
        unlimited.AvailableProjectCount.Should().BeNull();
    }
}

public sealed class TestingEventTemplateContractCoverageTests
{
    private static readonly QuestionnaireSchema Schema = new("Form", []);

    [Fact]
    public void TemplateFactoryAndRevision_ValidateAndNormalizeEveryField()
    {
        Invoking(() => Create(Guid.Empty, Guid.NewGuid(), "Name", "Rules", "Candidates", "Testers"))
            .Should().Throw<ArgumentException>();
        Invoking(() => Create(Guid.NewGuid(), Guid.Empty, "Name", "Rules", "Candidates", "Testers"))
            .Should().Throw<ArgumentException>();
        Invoking(() => Create(Guid.NewGuid(), Guid.NewGuid(), " ", "Rules", "Candidates", "Testers"))
            .Should().Throw<ArgumentException>();
        Invoking(() => Create(Guid.NewGuid(), Guid.NewGuid(), "Name", " ", "Candidates", "Testers"))
            .Should().Throw<ArgumentException>();
        Invoking(() => Create(Guid.NewGuid(), Guid.NewGuid(), "Name", "Rules", " ", "Testers"))
            .Should().Throw<ArgumentException>();
        Invoking(() => Create(Guid.NewGuid(), Guid.NewGuid(), "Name", "Rules", "Candidates", " "))
            .Should().Throw<ArgumentException>();

        var tenantId = Guid.NewGuid();
        var template = TestingEventTemplate.Create(
            tenantId, "  Standard  ", " Rules ", " Candidates ", " Testers ", Schema, Schema,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, Guid.NewGuid(), " ");
        template.Name.Should().Be("Standard");
        template.Description.Should().BeNull();

        template.CreateRevision(
            "New rules", "New candidates", "New testers", Schema, Schema,
            TestingEventMode.Hybrid, TestingEventApprovalMode.Committee, false,
            Guid.NewGuid(), "  Revised  ", "  Revised description  ");
        template.Name.Should().Be("Revised");
        template.Description.Should().Be("Revised description");
        template.CurrentRevisionNumber.Should().Be(2);
        template.CreateRevision(
            "Third rules", "Third candidates", "Third testers", Schema, Schema,
            TestingEventMode.InPerson, TestingEventApprovalMode.ManagerOnly, true,
            Guid.NewGuid(), null, null);
        template.Name.Should().Be("Revised");
        template.Description.Should().Be("Revised description");

        template.Archive();
        var archivedAt = template.ArchivedAt;
        template.Archive();
        template.ArchivedAt.Should().Be(archivedAt);
        template.RestoreArchivedTemplate();
        template.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void Projection_MapsTemplateAndCurrentRevisionWithoutDroppingFields()
    {
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var template = TestingEventTemplate.Create(
            tenantId, "Template", "Rules", "Candidates", "Testers", Schema, Schema,
            TestingEventMode.Hybrid, TestingEventApprovalMode.Committee, true, creatorId,
            "Description");
        template.Archive();

        var projection = TestingEventTemplateProjection.FromEntity(template);
        projection.Id.Should().Be(template.Id);
        projection.TenantId.Should().Be(tenantId);
        projection.Name.Should().Be("Template");
        projection.Description.Should().Be("Description");
        projection.IsArchived.Should().BeTrue();
        projection.CurrentRevisionNumber.Should().Be(1);
        projection.CurrentRevision.Id.Should().Be(template.CurrentRevision.Id);
        projection.CurrentRevision.TemplateId.Should().Be(template.Id);
        projection.CurrentRevision.RevisionNumber.Should().Be(1);
        projection.CurrentRevision.GeneralRules.Should().Be("Rules");
        projection.CurrentRevision.CandidateInstructions.Should().Be("Candidates");
        projection.CurrentRevision.TesterInstructions.Should().Be("Testers");
        projection.CurrentRevision.ProjectApplicationSchema.Should().BeEquivalentTo(Schema);
        projection.CurrentRevision.TesterRegistrationSchema.Should().BeEquivalentTo(Schema);
        projection.CurrentRevision.DefaultMode.Should().Be(TestingEventMode.Hybrid);
        projection.CurrentRevision.DefaultApprovalMode.Should().Be(TestingEventApprovalMode.Committee);
        projection.CurrentRevision.DefaultRequiresFeedback.Should().BeTrue();
        projection.CurrentRevision.CreatedByUserId.Should().Be(creatorId);
        projection.CurrentRevision.CreatedAt.Should().Be(template.CurrentRevision.CreatedAt);

        var request = new UpsertTestingEventTemplateRequest(
            "Name", "Description", "Rules", "Candidates", "Testers", Schema, Schema,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, false);
        request.Name.Should().Be("Name");
        request.Description.Should().Be("Description");
        request.GeneralRules.Should().Be("Rules");
        request.CandidateInstructions.Should().Be("Candidates");
        request.TesterInstructions.Should().Be("Testers");
        request.ProjectApplicationSchema.Should().BeSameAs(Schema);
        request.TesterRegistrationSchema.Should().BeSameAs(Schema);
        request.DefaultMode.Should().Be(TestingEventMode.Online);
        request.DefaultApprovalMode.Should().Be(TestingEventApprovalMode.ManagerOnly);
        request.DefaultRequiresFeedback.Should().BeFalse();
    }

    private static TestingEventTemplate Create(
        Guid tenantId, Guid creatorId, string name, string rules, string candidates, string testers) =>
        TestingEventTemplate.Create(
            tenantId, name, rules, candidates, testers, Schema, Schema,
            TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly, true, creatorId);

    private static Action Invoking(Action action) => action;
}

public sealed class TestingLabResourcePermissionCoverageTests
{
    [Fact]
    public void TestingFeedbackPermission_ComposesModerationCapabilities()
    {
        var permission = new TestingFeedbackPermission(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionType.Read);
        permission.CanView.Should().BeTrue();
        permission.CanEdit.Should().BeFalse();
        permission.CanDelete.Should().BeFalse();
        permission.CanManage.Should().BeFalse();
        permission.CanReport.Should().BeFalse();
        permission.CanRateQuality.Should().BeFalse();
        permission.CanRespond.Should().BeFalse();
        permission.CanModerate.Should().BeFalse();
        permission.AddPermission(PermissionType.Edit);
        permission.CanManage.Should().BeTrue();
        permission.AddPermission(PermissionType.Approve);
        permission.CanManage.Should().BeTrue();
        permission.CanModerate.Should().BeFalse();
        permission.AddPermission(PermissionType.Review);
        permission.AddPermission(PermissionType.Report);
        permission.AddPermission(PermissionType.Comment);
        permission.AddPermission(PermissionType.Delete);
        permission.CanModerate.Should().BeTrue();
        permission.CanReport.Should().BeTrue();
        permission.CanRateQuality.Should().BeTrue();
        permission.CanRespond.Should().BeTrue();
        permission.CanDelete.Should().BeTrue();
    }

    [Fact]
    public void RequestRegistrationAndSessionPermissions_ExposeTheirDistinctCapabilities()
    {
        var request = new TestingRequestPermission(Guid.NewGuid(), null, Guid.NewGuid(), PermissionType.Read);
        request.CanView.Should().BeTrue();
        request.CanParticipate.Should().BeTrue();
        request.CanManage.Should().BeFalse();
        request.CanEdit.Should().BeFalse();
        request.CanDelete.Should().BeFalse();
        request.CanProvideFeedback.Should().BeFalse();
        request.CanReview.Should().BeFalse();
        request.CanApprove.Should().BeFalse();
        request.AddPermission(PermissionType.Edit);
        request.CanManage.Should().BeTrue();
        request.AddPermission(PermissionType.Approve);
        request.CanManage.Should().BeTrue();
        request.AddPermission(PermissionType.Delete);
        request.AddPermission(PermissionType.Comment);
        request.AddPermission(PermissionType.Review);
        request.CanEdit.Should().BeTrue();
        request.CanDelete.Should().BeTrue();
        request.CanProvideFeedback.Should().BeTrue();
        request.CanReview.Should().BeTrue();
        request.CanApprove.Should().BeTrue();

        var registration = new SessionRegistrationPermission(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionType.Read);
        registration.CanView.Should().BeTrue();
        registration.CanManage.Should().BeFalse();
        registration.CanRegister.Should().BeFalse();
        registration.CanEdit.Should().BeFalse();
        registration.CanDelete.Should().BeFalse();
        registration.CanUpdateAttendance.Should().BeFalse();
        registration.CanApprove.Should().BeFalse();
        registration.CanReview.Should().BeFalse();
        registration.AddPermission(PermissionType.Edit);
        registration.CanManage.Should().BeTrue();
        registration.AddPermission(PermissionType.Approve);
        registration.CanManage.Should().BeTrue();
        registration.AddPermission(PermissionType.Create);
        registration.AddPermission(PermissionType.Delete);
        registration.AddPermission(PermissionType.Review);
        registration.CanRegister.Should().BeTrue();
        registration.CanDelete.Should().BeTrue();
        registration.CanUpdateAttendance.Should().BeTrue();
        registration.CanApprove.Should().BeTrue();
        registration.CanReview.Should().BeTrue();

        var session = new TestingSessionPermission(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PermissionType.Read);
        session.CanManage.Should().BeFalse();
        session.CanView.Should().BeTrue();
        session.CanRegister.Should().BeTrue();
        session.CanProvideFeedback.Should().BeFalse();
        session.CanModerate.Should().BeFalse();
        session.CanApprove.Should().BeFalse();
        session.AddPermission(PermissionType.Edit);
        session.CanManage.Should().BeFalse();
        session.AddPermission(PermissionType.Delete);
        session.AddPermission(PermissionType.Comment);
        session.AddPermission(PermissionType.Review);
        session.AddPermission(PermissionType.Approve);
        session.CanManage.Should().BeTrue();
        session.CanProvideFeedback.Should().BeTrue();
        session.CanModerate.Should().BeTrue();
        session.CanApprove.Should().BeTrue();
    }
}
