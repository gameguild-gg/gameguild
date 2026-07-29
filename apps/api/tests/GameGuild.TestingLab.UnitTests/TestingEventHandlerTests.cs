using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventHandlerTests : IDisposable
{
    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public TestingEventHandlerTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-event-handlers-{Guid.NewGuid():N}")
            .Options);
        AddActor(_managerId, TenantRole.Owner);
        SetActor(_managerId);
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateEvent_UsesAuthenticatedActorAsTenantManager()
    {
        var handler = CreateEventHandler();

        var result = await handler.Handle(new CreateTestingEventCommand(
            "Campus showcase",
            "Student projects",
            TestingEventMode.InPerson,
            TestingEventApprovalMode.ManagerOnly,
            SystemClock.UtcNow.AddDays(-1),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            true), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ManagerUserId.Should().Be(_managerId);
        result.Value.TenantId.Should().Be(_tenantId);
        (await _context.Set<TestingEvent>().SingleAsync()).ManagerUserId.Should().Be(_managerId);
    }

    [Fact]
    public async Task SubmitApplication_DoesNotConsumeSlotBeforeApproval()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var slot = AddSlot(testingEvent.Id, maxProjects: 1);
        var project = AddProject(_managerId);
        await _context.SaveChangesAsync();

        var result = await CreateApplicationHandler().Handle(
            new SubmitTestingProjectApplicationCommand(testingEvent.Id, project.Id, null, "Evening"),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TestingApplicationStatus.Pending);
        result.Value.AssignedSlotId.Should().BeNull();
        slot.MaxProjects.Should().Be(1);
    }

    [Fact]
    public async Task SubmitApplication_RequiresProjectEditPermission()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var project = AddProject(Guid.NewGuid());
        await _context.SaveChangesAsync();

        var result = await CreateApplicationHandler().Handle(
            new SubmitTestingProjectApplicationCommand(testingEvent.Id, project.Id, null, null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task ApproveApplication_RequiresEventManager()
    {
        var applicantId = Guid.NewGuid();
        AddActor(applicantId, TenantRole.Member);
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var slot = AddSlot(testingEvent.Id, 2);
        var project = AddProject(applicantId);
        var application = TestingProjectApplication.Submit(
            testingEvent.Id, project.Id, null, applicantId, null, _tenantId);
        _context.Add(application);
        await _context.SaveChangesAsync();
        SetActor(applicantId);

        var result = await CreateApplicationHandler().Handle(
            new ApproveTestingProjectApplicationCommand(application.Id, slot.Id, "Accepted"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task CommitteeApproval_RequiresMajority()
    {
        var reviewerOne = Guid.NewGuid();
        var reviewerTwo = Guid.NewGuid();
        var reviewerThree = Guid.NewGuid();
        AddActor(reviewerOne, TenantRole.Member);
        AddActor(reviewerTwo, TenantRole.Member);
        AddActor(reviewerThree, TenantRole.Member);
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.Committee);
        _context.AddRange(
            TestingCommitteeMember.Create(testingEvent.Id, reviewerOne, false, _tenantId),
            TestingCommitteeMember.Create(testingEvent.Id, reviewerTwo, false, _tenantId),
            TestingCommitteeMember.Create(testingEvent.Id, reviewerThree, false, _tenantId));
        var slot = AddSlot(testingEvent.Id, 2);
        var project = AddProject(_managerId);
        var application = TestingProjectApplication.Submit(testingEvent.Id, project.Id, null, _managerId, null, _tenantId);
        application.BeginReview();
        _context.Add(application);
        await _context.SaveChangesAsync();
        var handler = CreateApplicationHandler();

        SetActor(reviewerOne);
        (await handler.Handle(new CastTestingApplicationVoteCommand(
            application.Id, TestingApplicationVoteDecision.Approve, "Ready"), default)).IsSuccess.Should().BeTrue();
        SetActor(_managerId);
        var beforeMajority = await handler.Handle(
            new ApproveTestingProjectApplicationCommand(application.Id, slot.Id, "Committee review"), default);
        SetActor(reviewerTwo);
        (await handler.Handle(new CastTestingApplicationVoteCommand(
            application.Id, TestingApplicationVoteDecision.Approve, "Ready"), default)).IsSuccess.Should().BeTrue();
        SetActor(_managerId);
        var afterMajority = await handler.Handle(
            new ApproveTestingProjectApplicationCommand(application.Id, slot.Id, "Committee approved"), default);

        beforeMajority.IsFailure.Should().BeTrue();
        beforeMajority.Error.Type.Should().Be(ErrorType.Validation);
        afterMajority.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CommitteeTie_AllowsManagerTieBreakAfterAllVotes()
    {
        var reviewerOne = Guid.NewGuid();
        var reviewerTwo = Guid.NewGuid();
        AddActor(reviewerOne, TenantRole.Member);
        AddActor(reviewerTwo, TenantRole.Member);
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.Committee);
        _context.AddRange(
            TestingCommitteeMember.Create(testingEvent.Id, reviewerOne, false, _tenantId),
            TestingCommitteeMember.Create(testingEvent.Id, reviewerTwo, false, _tenantId));
        var slot = AddSlot(testingEvent.Id, 2);
        var project = AddProject(_managerId);
        var application = TestingProjectApplication.Submit(testingEvent.Id, project.Id, null, _managerId, null, _tenantId);
        application.BeginReview();
        _context.Add(application);
        await _context.SaveChangesAsync();
        var handler = CreateApplicationHandler();

        SetActor(reviewerOne);
        await handler.Handle(new CastTestingApplicationVoteCommand(
            application.Id, TestingApplicationVoteDecision.Approve, null), default);
        SetActor(reviewerTwo);
        await handler.Handle(new CastTestingApplicationVoteCommand(
            application.Id, TestingApplicationVoteDecision.Reject, "Needs work"), default);
        SetActor(_managerId);
        var result = await handler.Handle(
            new ApproveTestingProjectApplicationCommand(application.Id, slot.Id, "Manager tie-break"), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RejectApplication_RequiresRationale()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var project = AddProject(_managerId);
        var application = TestingProjectApplication.Submit(testingEvent.Id, project.Id, null, _managerId, null, _tenantId);
        _context.Add(application);
        await _context.SaveChangesAsync();

        var result = await CreateApplicationHandler().Handle(
            new RejectTestingProjectApplicationCommand(application.Id, " "), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ApproveApplication_RejectsFullProjectSlot()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var slot = AddSlot(testingEvent.Id, 1);
        var firstProject = AddProject(_managerId);
        var secondProject = AddProject(_managerId);
        var first = TestingProjectApplication.Submit(testingEvent.Id, firstProject.Id, null, _managerId, null, _tenantId);
        first.Approve(_managerId, slot.Id, "First");
        var second = TestingProjectApplication.Submit(testingEvent.Id, secondProject.Id, null, _managerId, null, _tenantId);
        _context.AddRange(first, second);
        await _context.SaveChangesAsync();

        var result = await CreateApplicationHandler().Handle(
            new ApproveTestingProjectApplicationCommand(second.Id, slot.Id, "Second"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        second.AssignedSlotId.Should().BeNull();
    }

    [Fact]
    public async Task EventQueries_AreTenantScoped()
    {
        var visible = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var hidden = TestingEvent.Create(
            "Other tenant",
            TestingEventMode.Online,
            _managerId,
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            false,
            TestingEventApprovalMode.ManagerOnly,
            Guid.NewGuid());
        _context.Add(hidden);
        await _context.SaveChangesAsync();

        var result = await CreateEventHandler().Handle(new GetTestingEventsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Id.Should().Be(visible.Id);
    }

    [Fact]
    public async Task CreateAndListSlots_UsesEventScheduleAndTenant()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        await _context.SaveChangesAsync();
        var handler = CreateEventHandler();

        var created = await handler.Handle(new CreateTestingEventSlotCommand(
            testingEvent.Id,
            TestingEventMode.Online,
            testingEvent.StartsAt,
            testingEvent.StartsAt.AddHours(2),
            null,
            4,
            null,
            null,
            "https://meet.example.com/session"), default);
        var listed = await handler.Handle(new GetTestingEventSlotsQuery(testingEvent.Id), default);

        created.IsSuccess.Should().BeTrue();
        listed.IsSuccess.Should().BeTrue();
        listed.Value.Should().ContainSingle().Which.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task UpdateEvent_RequiresEventManager()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        var memberId = Guid.NewGuid();
        AddActor(memberId, TenantRole.Member);
        await _context.SaveChangesAsync();
        SetActor(memberId);

        var result = await CreateEventHandler().Handle(new UpdateTestingEventCommand(
            testingEvent.Id,
            testingEvent.Name,
            testingEvent.Description,
            testingEvent.Mode,
            testingEvent.ApprovalMode,
            testingEvent.ApplicationsOpenAt,
            testingEvent.ApplicationsCloseAt,
            testingEvent.StartsAt,
            testingEvent.EndsAt,
            testingEvent.RequiresFeedback), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteEvent_RejectsEventAfterApplicationsOpen()
    {
        var testingEvent = AddOpenEvent(TestingEventApprovalMode.ManagerOnly);
        await _context.SaveChangesAsync();

        var result = await CreateEventHandler().Handle(new DeleteTestingEventCommand(testingEvent.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
    private TestingEventHandlers CreateEventHandler() => new(_context, _actorAccessor);

    private TestingApplicationHandlers CreateApplicationHandler() => new(
        _context,
        _actorAccessor,
        new ProjectAuthorizationService(_context, _actorAccessor),
        NullLogger<TestingApplicationHandlers>.Instance);

    private TestingEvent AddOpenEvent(TestingEventApprovalMode approvalMode)
    {
        var testingEvent = TestingEvent.Create(
            $"Event {Guid.NewGuid():N}",
            TestingEventMode.Online,
            _managerId,
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            true,
            approvalMode,
            _tenantId);
        testingEvent.OpenApplications();
        _context.Add(testingEvent);
        return testingEvent;
    }

    private TestingEventSlot AddSlot(Guid eventId, int? maxProjects)
    {
        var slot = TestingEventSlot.Create(
            eventId,
            TestingEventMode.Online,
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(2).AddHours(2),
            null,
            maxProjects,
            null,
            null,
            "https://meet.example.com/testing",
            _tenantId);
        _context.Add(slot);
        return slot;
    }

    private Project AddProject(Guid ownerId)
    {
        var project = new Project
        {
            TenantId = _tenantId,
            Title = $"Project {Guid.NewGuid():N}",
            Slug = $"project-{Guid.NewGuid():N}",
            Status = ContentStatus.Draft,
            Visibility = ContentVisibility.Private,
            CreatedById = ownerId
        };
        _context.Add(project);
        return project;
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
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();
        public DbSet<ProjectCollaborator> ProjectCollaborators => Set<ProjectCollaborator>();
        public DbSet<TestingEvent> TestingEvents => Set<TestingEvent>();
        public DbSet<TestingEventSlot> TestingEventSlots => Set<TestingEventSlot>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();
        public DbSet<TestingCommitteeMember> TestingCommitteeMembers => Set<TestingCommitteeMember>();
        public DbSet<TestingApplicationVote> TestingApplicationVotes => Set<TestingApplicationVote>();
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
        public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}