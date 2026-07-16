using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class SessionProjectChannelTests : IDisposable
{
    private readonly TestContext _context;
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public SessionProjectChannelTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        SetActor();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void Mapping_Should_Enforce_One_Active_Session_Project_Pair()
    {
        var modelBuilder = new ModelBuilder();
        new TestingLabModelConfiguration().Configure(modelBuilder);

        var entity = modelBuilder.Model.FindEntityType(typeof(SessionProject))!;
        entity.GetIndexes().Should().ContainSingle(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(SessionProject.SessionId), nameof(SessionProject.ProjectId) }) &&
            index.GetFilter() == "\"DeletedAt\" IS NULL AND \"IsActive\" = TRUE");
    }

    [Fact]
    public async Task Link_Should_Create_Canonical_Project_Link_And_List_Through_Cqrs()
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        var version = AddVersion(project.Id, _tenantId);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var linked = await handler.Handle(new LinkSessionProjectCommand(session.Id, project.Id, version.Id, "nightly"), default);
        var listed = await handler.Handle(new GetSessionProjectLinksQuery(session.Id), default);

        linked.IsSuccess.Should().BeTrue();
        linked.Value.ProjectVersionId.Should().Be(version.Id);
        listed.IsSuccess.Should().BeTrue();
        listed.Value.Should().ContainSingle(item => item.ProjectId == project.Id && item.IsActive);
    }

    [Fact]
    public async Task Link_Should_Reject_Duplicate_Active_Project()
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        _context.Set<SessionProject>().Add(new SessionProject
        {
            SessionId = session.Id,
            ProjectId = project.Id,
            RegisteredById = _actorId,
            TenantId = _tenantId,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkSessionProjectCommand(session.Id, project.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Link_Should_Reject_Version_From_Another_Project_Or_SoftDeleted_Version()
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        var otherProject = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        var wrongVersion = AddVersion(otherProject.Id, _tenantId);
        var deletedVersion = AddVersion(project.Id, _tenantId);
        deletedVersion.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var mismatch = await handler.Handle(new LinkSessionProjectCommand(session.Id, project.Id, wrongVersion.Id), default);
        var deleted = await handler.Handle(new LinkSessionProjectCommand(session.Id, project.Id, deletedVersion.Id), default);

        mismatch.IsFailure.Should().BeTrue();
        deleted.IsFailure.Should().BeTrue();
        mismatch.Error.Type.Should().Be(ErrorType.Validation);
        deleted.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Link_Should_Require_Project_Version_Selected_Tenant()
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        var crossTenantVersion = AddVersion(project.Id, Guid.NewGuid());
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new LinkSessionProjectCommand(session.Id, project.Id, crossTenantVersion.Id),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TestingLab.ProjectVersionMismatch");
    }

    [Fact]
    public async Task Link_Should_Require_Project_Version_Tenant_To_Match_Project_Tenant()
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        var wrongTenantVersion = AddVersion(project.Id, Guid.NewGuid());
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(
            new LinkSessionProjectCommand(session.Id, project.Id, wrongTenantVersion.Id),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(ContentStatus.Archived)]
    [InlineData(ContentStatus.Deleted)]
    public async Task Link_Should_Reject_Terminal_Project_Lifecycle(ContentStatus status)
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, status);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new LinkSessionProjectCommand(session.Id, project.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Link_Should_Reject_CrossTenant_Session_And_Missing_Project_Permission()
    {
        var crossTenantSession = AddSession(Guid.NewGuid(), _actorId);
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var crossTenant = await handler.Handle(new LinkSessionProjectCommand(crossTenantSession.Id, project.Id), default);
        var forbidden = await handler.Handle(new LinkSessionProjectCommand(session.Id, project.Id), default);

        crossTenant.IsFailure.Should().BeTrue();
        forbidden.IsFailure.Should().BeTrue();
        forbidden.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Link_Should_Fail_Closed_And_Require_Session_Manager_Or_Creator()
    {
        var session = AddSession(_tenantId, Guid.NewGuid());
        var project = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Owner, string.Empty);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var sessionForbidden = await handler.Handle(new LinkSessionProjectCommand(session.Id, project.Id), default);
        _actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        var absent = await handler.Handle(new LinkSessionProjectCommand(session.Id, project.Id), default);

        sessionForbidden.Error.Type.Should().Be(ErrorType.Forbidden);
        absent.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task List_Should_Deny_Inactive_Session_Manager()
    {
        var session = AddSession(_tenantId, _actorId);
        await _context.SaveChangesAsync();
        (await _context.Set<User>().SingleAsync()).IsActive = false;
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetSessionProjectLinksQuery(session.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task List_Should_Exclude_Active_Links_With_Stale_Project_Or_Version_State()
    {
        var session = AddSession(_tenantId, _actorId);
        var validProject = AddProject(_tenantId, ContentStatus.Draft);
        var validVersion = AddVersion(validProject.Id, _tenantId);
        var softDeletedProject = AddProject(_tenantId, ContentStatus.Draft);
        softDeletedProject.DeletedAt = DateTime.UtcNow;
        var archivedProject = AddProject(_tenantId, ContentStatus.Archived);
        var deletedProject = AddProject(_tenantId, ContentStatus.Deleted);
        var movedProject = AddProject(Guid.NewGuid(), ContentStatus.Draft);
        var versionMismatchProject = AddProject(_tenantId, ContentStatus.Draft);
        var otherProjectVersion = AddVersion(validProject.Id, _tenantId);
        var crossTenantVersionProject = AddProject(_tenantId, ContentStatus.Draft);
        var crossTenantVersion = AddVersion(crossTenantVersionProject.Id, Guid.NewGuid());
        var deletedVersionProject = AddProject(_tenantId, ContentStatus.Draft);
        var deletedVersion = AddVersion(deletedVersionProject.Id, _tenantId);
        deletedVersion.DeletedAt = DateTime.UtcNow;

        AddLink(session.Id, validProject.Id, validVersion.Id);
        AddLink(session.Id, softDeletedProject.Id);
        AddLink(session.Id, archivedProject.Id);
        AddLink(session.Id, deletedProject.Id);
        AddLink(session.Id, movedProject.Id);
        AddLink(session.Id, versionMismatchProject.Id, otherProjectVersion.Id);
        AddLink(session.Id, crossTenantVersionProject.Id, crossTenantVersion.Id);
        AddLink(session.Id, deletedVersionProject.Id, deletedVersion.Id);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new GetSessionProjectLinksQuery(session.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.ProjectId.Should().Be(validProject.Id);
    }

    [Fact]
    public async Task Unlink_Should_Deactivate_And_SoftDelete_Link()
    {
        var session = AddSession(_tenantId, _actorId);
        var project = AddProject(_tenantId, ContentStatus.Draft);
        AddCollaborator(project.Id, _actorId, ProjectRoles.Editor, "Edit");
        var link = new SessionProject
        {
            SessionId = session.Id,
            ProjectId = project.Id,
            RegisteredById = _actorId,
            TenantId = _tenantId,
            IsActive = true
        };
        _context.Set<SessionProject>().Add(link);
        await _context.SaveChangesAsync();

        var result = await CreateHandler().Handle(new UnlinkSessionProjectCommand(session.Id, project.Id), default);

        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeFalse();
        link.DeletedAt.Should().NotBeNull();
    }

    private SessionProjectHandlers CreateHandler()
        => new(
            _context,
            _actorAccessor.Object,
            new ProjectChannelAvailabilityService(_context),
            new ProjectAuthorizationService(_context, _actorAccessor.Object),
            NullLogger<SessionProjectHandlers>.Instance);

    private void SetActor()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(_actorId).WithTenantId(_tenantId).Build());
        _context.Set<User>().Add(new User
        {
            Id = _actorId,
            Email = $"{_actorId:N}@example.com",
            Name = "Testing Lab actor",
            IsActive = true
        });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = _tenantId,
            Role = "Member",
            IsActive = true
        });
    }

    private TestingSession AddSession(Guid tenantId, Guid managerId)
    {
        var session = new TestingSession
        {
            TenantId = tenantId,
            TestingRequestId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            SessionName = "Session",
            SessionDate = DateTime.UtcNow,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            MaxTesters = 8,
            ManagerId = managerId,
            ManagerUserId = managerId,
            CreatedById = managerId
        };
        _context.Set<TestingSession>().Add(session);
        return session;
    }

    private Project AddProject(Guid tenantId, ContentStatus status)
    {
        var project = new Project
        {
            TenantId = tenantId,
            Title = Guid.NewGuid().ToString(),
            Slug = Guid.NewGuid().ToString(),
            Status = status,
            Visibility = ContentVisibility.Private
        };
        _context.Set<Project>().Add(project);
        return project;
    }

    private ProjectVersion AddVersion(Guid projectId, Guid tenantId)
    {
        var version = new ProjectVersion
        {
            ProjectId = projectId,
            TenantId = tenantId,
            VersionNumber = Guid.NewGuid().ToString(),
            Status = "testing"
        };
        _context.Set<ProjectVersion>().Add(version);
        return version;
    }

    private void AddCollaborator(Guid projectId, Guid userId, string role, string permissions)
        => _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            Permissions = permissions,
            IsActive = true
        });

    private void AddLink(Guid sessionId, Guid projectId, Guid? projectVersionId = null)
        => _context.Set<SessionProject>().Add(new SessionProject
        {
            SessionId = sessionId,
            ProjectId = projectId,
            ProjectVersionId = projectVersionId,
            RegisteredById = _actorId,
            TenantId = _tenantId,
            IsActive = true
        });

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options), IApplicationDbContext
    {
        public DbSet<TestingSession> TestingSessions => Set<TestingSession>();
        public DbSet<SessionProject> SessionProjects => Set<SessionProject>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectCollaborator> ProjectCollaborators => Set<ProjectCollaborator>();
        public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

public sealed class SessionProjectControllerTests
{
    [Fact]
    public async Task LinkSessionProject_Should_Delegate_Through_Cqrs()
    {
        var mediator = new Mock<IMediator>();
        var sessionId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<IRequest<Result<SessionProjectProjection>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SessionProjectProjection(Guid.NewGuid(), sessionId, projectId, null, true)));
        var controller = new TestingSessionsController(
            Mock.Of<ITestingSessionOperations>(),
            mediator.Object,
            Mock.Of<IActorContextAccessor>(),
            NullLogger<TestingSessionsController>.Instance);

        var result = await controller.LinkSessionProject(sessionId, new LinkSessionProjectRequest(projectId));

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        mediator.Verify(candidate => candidate.Send(
            It.Is<LinkSessionProjectCommand>(command => command.SessionId == sessionId && command.ProjectId == projectId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
