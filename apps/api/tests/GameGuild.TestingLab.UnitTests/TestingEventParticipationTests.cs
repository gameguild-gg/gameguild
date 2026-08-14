using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using GameGuild.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventParticipationTests : IDisposable
{
    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly Guid _testerOneId = Guid.NewGuid();
    private readonly Guid _testerTwoId = Guid.NewGuid();
    private readonly Guid _testerThreeId = Guid.NewGuid();

    public TestingEventParticipationTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-event-participation-{Guid.NewGuid():N}")
            .Options);
        AddActor(_managerId, TenantRole.Owner);
        AddActor(_testerOneId, TenantRole.Member);
        AddActor(_testerTwoId, TenantRole.Member);
        AddActor(_testerThreeId, TenantRole.Member);
        SetActor(_managerId);
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Register_WhenInPersonSlotIsFull_ShouldWaitlistWithoutConsumingCapacity()
    {
        var (_, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, maxTesters: 1);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_testerTwoId);
        var waitlisted = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);

        registered.IsSuccess.Should().BeTrue();
        registered.Value.Status.Should().Be(TestingSlotRegistrationStatus.Registered);
        waitlisted.IsSuccess.Should().BeTrue();
        waitlisted.Value.Status.Should().Be(TestingSlotRegistrationStatus.Waitlisted);
        waitlisted.Value.WaitlistPosition.Should().Be(1);
        (await _context.TestingSlotRegistrations.CountAsync(candidate =>
            candidate.SlotId == slot.Id &&
            candidate.Status == TestingSlotRegistrationStatus.Registered)).Should().Be(1);
    }

    [Fact]
    public async Task Register_WhenOnlineCapacityIsUnlimited_ShouldRegisterEveryTester()
    {
        var (_, slot) = AddScheduledEventAndSlot(TestingEventMode.Online, maxTesters: null);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        foreach (var testerId in new[] { _testerOneId, _testerTwoId, _testerThreeId })
        {
            SetActor(testerId);
            var result = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(TestingSlotRegistrationStatus.Registered);
        }

        (await _context.TestingSlotRegistrations.CountAsync(candidate =>
            candidate.SlotId == slot.Id &&
            candidate.Status == TestingSlotRegistrationStatus.Registered)).Should().Be(3);
    }

    [Fact]
    public async Task Cancel_ShouldPromoteOldestWaitlistedTesterDeterministically()
    {
        var (_, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, maxTesters: 1);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_testerTwoId);
        var firstWaitlisted = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_testerThreeId);
        var secondWaitlisted = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_testerOneId);
        var cancelled = await handler.Handle(new CancelTestingEventSlotRegistrationCommand(registered.Value.Id), default);

        cancelled.IsSuccess.Should().BeTrue(cancelled.IsFailure ? cancelled.Error.Description : string.Empty);
        (await _context.TestingSlotRegistrations.FindAsync(firstWaitlisted.Value.Id))!
            .Status.Should().Be(TestingSlotRegistrationStatus.Registered);
        (await _context.TestingSlotRegistrations.FindAsync(secondWaitlisted.Value.Id))!
            .WaitlistPosition.Should().Be(1);
    }

    [Fact]
    public async Task MarkNoShow_ShouldNotCompleteParticipation()
    {
        var (_, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, maxTesters: 2);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);

        var noShow = await handler.Handle(new MarkTestingEventNoShowCommand(registered.Value.Id), default);
        var complete = await handler.Handle(new CompleteTestingEventParticipationCommand(registered.Value.Id), default);

        noShow.IsSuccess.Should().BeTrue();
        noShow.Value.Status.Should().Be(TestingSlotRegistrationStatus.NoShow);
        complete.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Complete_WithRequiredFeedbackPending_ShouldFail()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(
            TestingEventMode.InPerson,
            maxTesters: 2,
            requiresFeedback: true);
        var application = AddApprovedApplication(testingEvent, slot);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(registered.Value.Id), default);
        await handler.Handle(new AssignTestingProjectToTesterCommand(registered.Value.Id, application.Id), default);
        await handler.Handle(new CheckOutTestingEventRegistrationCommand(registered.Value.Id), default);

        var result = await handler.Handle(new CompleteTestingEventParticipationCommand(registered.Value.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        (await _context.TestingFeedbackObligations.SingleAsync()).Status
            .Should().Be(TestingFeedbackObligationStatus.Pending);
    }

    [Fact]
    public async Task AssignProject_ShouldRejectTheProjectCreatorAsTester()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(
            TestingEventMode.InPerson,
            maxTesters: 2,
            requiresFeedback: true);
        var application = AddApprovedApplication(testingEvent, slot, _testerOneId);
        await AssignAndAssertProjectTesterConflict(application, slot);
    }

    [Fact]
    public async Task AssignProject_ShouldRejectAnActiveProjectCollaboratorAsTester()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, 2, true);
        var application = AddApprovedApplication(testingEvent, slot);
        _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = application.ProjectId,
            UserId = _testerOneId,
            Role = ProjectRoles.Viewer,
            Permissions = "Read",
            IsActive = true
        });

        await AssignAndAssertProjectTesterConflict(application, slot);
    }

    [Fact]
    public async Task AssignProject_ShouldRejectAnActiveProjectTeamMemberAsTester()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, 2, true);
        var application = AddApprovedApplication(testingEvent, slot);
        var team = new Team { TenantId = _tenantId, Name = "Project team", IsActive = true };
        _context.Set<Team>().Add(team);
        _context.Set<TeamMember>().Add(new TeamMember
        {
            TenantId = _tenantId,
            TeamId = team.Id,
            UserId = _testerOneId,
            IsActive = true
        });
        _context.Set<ProjectTeam>().Add(new ProjectTeam
        {
            TenantId = _tenantId,
            ProjectId = application.ProjectId,
            TeamId = team.Id,
            IsActive = true,
            Permissions = "Read"
        });

        await AssignAndAssertProjectTesterConflict(application, slot);
    }

    [Fact]
    public async Task AssignProject_ShouldIgnoreCorruptCrossTenantTeamMembership()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, 2, true);
        var application = AddApprovedApplication(testingEvent, slot);
        var otherTenantId = Guid.NewGuid();
        var team = new Team { TenantId = otherTenantId, Name = "Other tenant team", IsActive = true };
        _context.Set<Team>().Add(team);
        _context.Set<TeamMember>().Add(new TeamMember
        {
            TenantId = otherTenantId,
            TeamId = team.Id,
            UserId = _testerOneId,
            IsActive = true,
        });
        _context.Set<ProjectTeam>().Add(new ProjectTeam
        {
            TenantId = _tenantId,
            ProjectId = application.ProjectId,
            TeamId = team.Id,
            IsActive = true,
            Permissions = "Read",
        });
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(registered.Value.Id), default);

        var result = await handler.Handle(
            new AssignTestingProjectToTesterCommand(registered.Value.Id, application.Id),
            default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AssignProject_ShouldAllowAReadOnlyGrantThatDoesNotCreateTeamMembership()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, 2, true);
        var application = AddApprovedApplication(testingEvent, slot);
        _context.Set<GameGuild.Identity.Authorization.ResourceUserPermission>().Add(new()
        {
            TenantId = new GameGuild.CQRS.Models.TenantId(_tenantId),
            UserId = _testerOneId,
            ResourceType = nameof(Project),
            ResourceId = application.ProjectId.ToString(),
            Permissions = ["Read"],
            GrantedByUserId = _managerId
        });
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(registered.Value.Id), default);

        var result = await handler.Handle(new AssignTestingProjectToTesterCommand(registered.Value.Id, application.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetTesterEligibility_ShouldExcludeCollaboratorsAndTeamMembersButKeepReadOnlyGrants()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, 3, true);
        var application = AddApprovedApplication(testingEvent, slot);
        _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = application.ProjectId,
            UserId = _testerOneId,
            Role = ProjectRoles.Viewer,
            Permissions = "Read",
            IsActive = true
        });
        var team = new Team { TenantId = _tenantId, Name = "Project team", IsActive = true };
        _context.Set<Team>().Add(team);
        _context.Set<TeamMember>().Add(new TeamMember
        {
            TenantId = _tenantId,
            TeamId = team.Id,
            UserId = _testerTwoId,
            IsActive = true
        });
        _context.Set<ProjectTeam>().Add(new ProjectTeam
        {
            TenantId = _tenantId,
            ProjectId = application.ProjectId,
            TeamId = team.Id,
            IsActive = true,
            Permissions = "Read"
        });
        _context.Set<GameGuild.Identity.Authorization.ResourceUserPermission>().Add(new()
        {
            TenantId = new GameGuild.CQRS.Models.TenantId(_tenantId),
            UserId = _testerThreeId,
            ResourceType = nameof(Project),
            ResourceId = application.ProjectId.ToString(),
            Permissions = ["Read"],
            GrantedByUserId = _managerId
        });
        await _context.SaveChangesAsync();

        var result = await CreateApplicationHandler().Handle(
            new GetTestingApplicationTesterEligibilityQuery(
                testingEvent.Id,
                [_testerOneId, _testerTwoId, _testerThreeId]),
            default);

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Description : string.Empty);
        result.Value.Single(item => item.TesterUserId == _testerOneId).EligibleApplicationIds.Should().BeEmpty();
        result.Value.Single(item => item.TesterUserId == _testerTwoId).EligibleApplicationIds.Should().BeEmpty();
        result.Value.Single(item => item.TesterUserId == _testerThreeId).EligibleApplicationIds.Should().Equal(application.Id);
    }

    [Fact]
    public async Task SubmitFeedback_ShouldFulfillObligation_AndAllowParticipationCompletion()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(
            TestingEventMode.InPerson,
            maxTesters: 2,
            requiresFeedback: true);
        var application = AddApprovedApplication(testingEvent, slot);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(registered.Value.Id), default);
        var assigned = await handler.Handle(
            new AssignTestingProjectToTesterCommand(registered.Value.Id, application.Id),
            default);
        await handler.Handle(new CheckOutTestingEventRegistrationCommand(registered.Value.Id), default);
        SetActor(_testerOneId);

        var feedback = await handler.Handle(new SubmitTestingEventFeedbackCommand(
            assigned.Value.Id,
            """{"playability":"clear","notes":"Improve onboarding"}""",
            8,
            true,
            "Useful build"), default);
        var completed = await handler.Handle(
            new CompleteTestingEventParticipationCommand(registered.Value.Id),
            default);

        feedback.IsSuccess.Should().BeTrue();
        completed.IsSuccess.Should().BeTrue();
        completed.Value.Status.Should().Be(TestingSlotRegistrationStatus.Completed);
        (await _context.TestingFeedbackObligations.SingleAsync()).IsFulfilled.Should().BeTrue();
        (await _context.TestingFeedback.SingleAsync()).EventId.Should().Be(testingEvent.Id);
    }


    [Fact]
    public async Task GetEventFeedback_AsManager_ReturnsObligationsAndSubmittedFeedback()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(
            TestingEventMode.InPerson,
            maxTesters: 2,
            requiresFeedback: true);
        var application = AddApprovedApplication(testingEvent, slot);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(registered.Value.Id), default);
        var assigned = await handler.Handle(
            new AssignTestingProjectToTesterCommand(registered.Value.Id, application.Id),
            default);
        SetActor(_testerOneId);
        await handler.Handle(new SubmitTestingEventFeedbackCommand(
            assigned.Value.Id,
            """{"playability":"clear"}""",
            9,
            true,
            "Ready for another round"), default);
        SetActor(_managerId);

        var result = await handler.Handle(new GetTestingEventFeedbackQuery(testingEvent.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].ApplicationId.Should().Be(application.Id);
        result.Value[0].TesterUserId.Should().Be(_testerOneId);
        result.Value[0].Feedback.Should().NotBeNull();
        result.Value[0].Feedback!.OverallRating.Should().Be(9);
    }

    [Fact]
    public async Task GetEventFeedback_AsNonManager_ShouldBeForbidden()
    {
        var (testingEvent, _) = AddScheduledEventAndSlot(TestingEventMode.Online, maxTesters: null);
        await _context.SaveChangesAsync();
        SetActor(_testerOneId);

        var result = await CreateHandler().Handle(new GetTestingEventFeedbackQuery(testingEvent.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetMyRegistrations_ReturnsOnlyCurrentTesterRegistrationsForEvent()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(TestingEventMode.InPerson, maxTesters: 2);
        _context.AddRange(
            TestingSlotRegistration.Register(testingEvent.Id, slot.Id, _testerOneId, null, _tenantId),
            TestingSlotRegistration.Register(testingEvent.Id, slot.Id, _testerTwoId, null, _tenantId));
        await _context.SaveChangesAsync();
        SetActor(_testerOneId);

        var result = await CreateHandler().Handle(
            new GetMyTestingSlotRegistrationsQuery(testingEvent.Id),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.UserId.Should().Be(_testerOneId);
    }

    [Fact]
    public async Task ReRegister_AfterCancellation_ShouldNotAttachPendingFeedbackToCancelledHistory()
    {
        var (testingEvent, slot) = AddScheduledEventAndSlot(
            TestingEventMode.InPerson,
            maxTesters: 2,
            requiresFeedback: true);
        var application = AddApprovedApplication(testingEvent, slot);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        SetActor(_testerOneId);
        var original = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        await handler.Handle(new CancelTestingEventSlotRegistrationCommand(original.Value.Id), default);
        var replacement = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);

        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(replacement.Value.Id), default);
        await handler.Handle(new AssignTestingProjectToTesterCommand(replacement.Value.Id, application.Id), default);
        var registrations = await handler.Handle(
            new GetTestingEventSlotRegistrationsQuery(slot.Id),
            default);

        registrations.IsSuccess.Should().BeTrue();
        registrations.Value.Single(item => item.Id == original.Value.Id)
            .PendingFeedbackCount.Should().Be(0);
        registrations.Value.Single(item => item.Id == replacement.Value.Id)
            .PendingFeedbackCount.Should().Be(1);
    }
    private TestingParticipationHandlers CreateHandler() => new(
        _context,
        _actorAccessor,
        NullLogger<TestingParticipationHandlers>.Instance);

    private TestingApplicationHandlers CreateApplicationHandler() => new(
        _context,
        _actorAccessor,
        new ProjectAuthorizationService(_context, _actorAccessor),
        NullLogger<TestingApplicationHandlers>.Instance);

    private async Task AssignAndAssertProjectTesterConflict(
        TestingProjectApplication application,
        TestingEventSlot slot)
    {
        await _context.SaveChangesAsync();
        var handler = CreateHandler();
        SetActor(_testerOneId);
        var registered = await handler.Handle(new RegisterTestingEventSlotCommand(slot.Id, null), default);
        SetActor(_managerId);
        await handler.Handle(new CheckInTestingEventRegistrationCommand(registered.Value.Id), default);

        var result = await handler.Handle(
            new AssignTestingProjectToTesterCommand(registered.Value.Id, application.Id),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TestingLab.ProjectTesterConflict");
        (await _context.TestingFeedbackObligations.CountAsync()).Should().Be(0);
    }

    private (TestingEvent Event, TestingEventSlot Slot) AddScheduledEventAndSlot(
        TestingEventMode mode,
        int? maxTesters,
        bool requiresFeedback = false)
    {
        var testingEvent = TestingEvent.Create(
            $"Event {Guid.NewGuid():N}",
            mode,
            _managerId,
            SystemClock.UtcNow.AddDays(-4),
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            requiresFeedback,
            TestingEventApprovalMode.ManagerOnly,
            _tenantId);
        testingEvent.OpenApplications();
        testingEvent.CloseApplications();
        var slot = TestingEventSlot.Create(
            testingEvent.Id,
            mode,
            testingEvent.StartsAt,
            testingEvent.StartsAt.AddHours(2),
            maxTesters,
            4,
            mode == TestingEventMode.InPerson ? "Main campus" : null,
            mode == TestingEventMode.InPerson ? "Lab 201" : null,
            mode == TestingEventMode.Online ? "https://meet.example.com/testing" : null,
            _tenantId);
        _context.AddRange(testingEvent, slot);
        return (testingEvent, slot);
    }

    private TestingProjectApplication AddApprovedApplication(
        TestingEvent testingEvent,
        TestingEventSlot slot,
        Guid? projectCreatorId = null)
    {
        var project = new Project
        {
            TenantId = _tenantId,
            Title = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            Status = ContentStatus.Draft,
            Visibility = ContentVisibility.Private,
            CreatedById = projectCreatorId ?? _managerId
        };
        var application = TestingProjectApplication.Submit(
            testingEvent.Id,
            project.Id,
            null,
            _managerId,
            null,
            _tenantId);
        application.Approve(_managerId, slot.Id, "Approved");
        _context.AddRange(project, application);
        return application;
    }

    private void AddActor(Guid userId, string role)
    {
        _context.Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Testing Lab actor",
            IsActive = true
        });
        _context.Add(new TenantMember
        {
            UserId = userId,
            TenantId = _tenantId,
            Role = role,
            IsActive = true
        });
    }

    private void SetActor(Guid userId) => _actorAccessor.SetActorContext(
        ActorContextBuilder.ForUser(userId).WithTenantId(_tenantId).Build());

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
        public DbSet<TestingEvent> TestingEvents => Set<TestingEvent>();
        public DbSet<TestingEventSlot> TestingEventSlots => Set<TestingEventSlot>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();
        public DbSet<TestingSlotRegistration> TestingSlotRegistrations => Set<TestingSlotRegistration>();
        public DbSet<TestingFeedbackObligation> TestingFeedbackObligations => Set<TestingFeedbackObligation>();
        public DbSet<TestingFeedback> TestingFeedback => Set<TestingFeedback>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectCollaborator> ProjectCollaborators => Set<ProjectCollaborator>();
        public DbSet<ProjectTeam> ProjectTeams => Set<ProjectTeam>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<GameGuild.Identity.Authorization.ResourceUserPermission> ResourceUserPermissions => Set<GameGuild.Identity.Authorization.ResourceUserPermission>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
