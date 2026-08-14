using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Projects.UnitTests.Controllers;

public class ProjectsControllerTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IActorContextAccessor> _actorContextAccessor = new();
    private readonly Mock<IProjectAuthorizationService> _authorizationService = new();
    private readonly TestProjectsDbContext _context;

    public ProjectsControllerTests()
    {
        var options = new DbContextOptionsBuilder<TestProjectsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestProjectsDbContext(options);
        _actorContextAccessor
            .SetupGet(x => x.ActorContext)
            .Returns(ActorContextBuilder.ForUser(_actorId).WithRole("Admin").Build());
        _authorizationService
            .Setup(service => service.HasPermissionAsync(
                It.IsAny<Guid>(),
                It.IsAny<GameGuild.Identity.Authorization.PermissionType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authorizationService
            .Setup(service => service.HasPermissionIncludingDeletedAsync(
                It.IsAny<Guid>(),
                It.IsAny<GameGuild.Identity.Authorization.PermissionType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authorizationService
            .Setup(service => service.ApplyWorkspaceAccess(It.IsAny<IQueryable<Project>>(), It.IsAny<bool>()))
            .Returns((IQueryable<Project> query, bool _) => query);
    }

    [Fact]
    public async Task GetProjects_Should_Send_Filtered_Query()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<IRequest<Result<IEnumerable<Project>>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<Project>>([new Project { Title = "Project", Slug = "project" }]));

        var controller = CreateController();

        var result = await controller.GetProjects(
            ProjectType.Game,
            ContentStatus.Published,
            ContentVisibility.Public,
            searchTerm: "game",
            currentTenantOnly: true,
            take: 1000);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(x => x.Send(
            It.Is<GetAllProjectsQuery>(q =>
                q.Type == ProjectType.Game &&
                q.Status == ContentStatus.Published &&
                q.Visibility == ContentVisibility.Public &&
                q.SearchTerm == "game" &&
                q.CurrentTenantOnly &&
                q.Take == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProject_Should_Return_Created_For_Success()
    {
        var project = new Project { Title = "Created", Slug = "created" };
        _mediator
            .Setup(x => x.Send(It.IsAny<IRequest<Result<Project>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(project));

        var controller = CreateController();

        var result = await controller.CreateProject(new CreateProjectRequest { Title = "Created", Type = ProjectType.Game });

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _mediator.Verify(x => x.Send(
            It.Is<CreateProjectCommand>(c => c.Title == "Created" && c.CreatedById == _actorId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProjectVersions_Should_CreateAndListWithinProjectTenant()
    {
        var tenantId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Versioned project",
            Slug = "versioned-project",
            CreatedById = _actorId
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        var controller = CreateController();

        var created = await controller.CreateProjectVersion(project.Id, new CreateProjectVersionRequest
        {
            VersionNumber = "1.0.0",
            Status = "ready",
            ReleaseNotes = "First testable build"
        });
        var listed = await controller.GetProjectVersions(project.Id);

        created.Result.Should().BeOfType<CreatedAtActionResult>();
        var version = await _context.Set<ProjectVersion>().SingleAsync();
        version.TenantId.Should().Be(tenantId);
        version.CreatedById.Should().Be(_actorId);
        listed.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IReadOnlyList<ProjectVersionApiResponse>>()
            .Which.Should().ContainSingle(item => item.VersionNumber == "1.0.0");

        var options = await controller.GetAccessibleProjectVersions();
        options.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IReadOnlyList<ProjectVersionOptionProjection>>()
            .Which.Should().ContainSingle(item => item.ProjectId == project.Id && item.Id == version.Id);
    }

    [Fact]
    public async Task AccessibleProjectVersions_Should_ExcludeArchivedProjects()
    {
        var archivedProject = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Title = "Archived project",
            Slug = "archived-project",
            Status = ContentStatus.Archived,
            CreatedById = _actorId
        };
        _context.Projects.Add(archivedProject);
        _context.Set<ProjectVersion>().Add(new ProjectVersion
        {
            Id = Guid.NewGuid(),
            TenantId = archivedProject.TenantId,
            ProjectId = archivedProject.Id,
            Project = archivedProject,
            VersionNumber = "1.0.0",
            CreatedById = _actorId
        });
        await _context.SaveChangesAsync();

        var result = await CreateController().GetAccessibleProjectVersions();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IReadOnlyList<ProjectVersionOptionProjection>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectCollaborators_Should_Return_Active_Collaborators()
    {
        var projectId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _context.Users.Add(new GameGuild.Identity.Users.User
        {
            Id = ownerId,
            Email = "owner@example.com",
            Name = "Owner User"
        });
        _context.ProjectCollaborators.Add(new ProjectCollaborator
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = ownerId,
            Role = "Owner",
            Permissions = "all",
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });
        _context.ProjectCollaborators.Add(new ProjectCollaborator
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = Guid.NewGuid(),
            Role = "Viewer",
            Permissions = "read",
            IsActive = false
        });
        await _context.SaveChangesAsync();

        var controller = CreateController();

        var result = await controller.GetProjectCollaborators(projectId);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var collaborators = ok.Value.Should().BeAssignableTo<IEnumerable<CollaboratorDto>>().Subject;
        collaborators.Should().ContainSingle(c => c.Role == "Owner");
    }

    [Fact]
    public async Task Project_Invitations_Should_List_Accept_And_Decline_Persisted_Invitations()
    {
        var tenantId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var inviteeId = _actorId;
        var declinedToken = "decline-token";
        var acceptedToken = "accept-token";
        _context.Projects.Add(new Project
        {
            Id = projectId,
            TenantId = tenantId,
            Title = "Launch Candidate",
            Slug = "launch-candidate",
            CreatedById = Guid.NewGuid(),
            Visibility = ContentVisibility.Public,
            Status = ContentStatus.Published
        });
        _context.Users.Add(new User
        {
            Id = inviteeId,
            Email = "invitee@example.com",
            Name = "Invited User"
        });
        _context.TenantMembers.Add(new TenantMember
        {
            TenantId = tenantId,
            UserId = inviteeId,
            Role = "Member",
            IsActive = true
        });
        _context.ProjectInvitations.AddRange(
            new ProjectInvitation
            {
                TenantId = tenantId,
                ProjectId = projectId,
                InvitedUserId = inviteeId,
                InvitedByUserId = Guid.NewGuid(),
                Role = "Reviewer",
                Permissions = "read,comment",
                Token = acceptedToken,
                Status = ProjectInvitationStatus.Pending
            },
            new ProjectInvitation
            {
                TenantId = tenantId,
                ProjectId = projectId,
                InvitedUserId = inviteeId,
                InvitedByUserId = Guid.NewGuid(),
                Role = "Viewer",
                Permissions = "read",
                Token = declinedToken,
                Status = ProjectInvitationStatus.Pending
            });
        await _context.SaveChangesAsync();
        _actorContextAccessor
            .SetupGet(x => x.ActorContext)
            .Returns(ActorContextBuilder.ForUser(inviteeId).WithTenantId(tenantId).WithRole("Member").Build());

        var controller = CreateController();

        var invitations = await controller.GetMyProjectInvitations();
        var invitationOk = invitations.Result.Should().BeOfType<OkObjectResult>().Subject;
        invitationOk.Value.Should().BeAssignableTo<IEnumerable<ProjectInvitationDto>>()
            .Subject.Should().HaveCount(2);

        var accepted = await controller.AcceptProjectInvitation(acceptedToken);
        accepted.Result.Should().BeOfType<OkObjectResult>();
        _context.ProjectCollaborators.Should().ContainSingle(c =>
            c.ProjectId == projectId &&
            c.UserId == inviteeId &&
            c.Role == "Reviewer" &&
            c.IsActive);

        var declined = await controller.DeclineProjectInvitation(declinedToken);
        declined.Result.Should().BeOfType<OkObjectResult>();

        var savedAccepted = await _context.ProjectInvitations.SingleAsync(i => i.Token == acceptedToken);
        savedAccepted.Status.Should().Be(ProjectInvitationStatus.Accepted);
        savedAccepted.RespondedAt.Should().NotBeNull();

        var savedDeclined = await _context.ProjectInvitations.SingleAsync(i => i.Token == declinedToken);
        savedDeclined.Status.Should().Be(ProjectInvitationStatus.Declined);
        savedDeclined.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void ProjectInvitation_Should_Reject_Expired_And_NonPending_Responses()
    {
        var available = new ProjectInvitation { ExpiresAt = DateTime.UtcNow.AddMinutes(5) };
        available.IsExpired.Should().BeFalse();
        available.CanRespond.Should().BeTrue();

        var expired = new ProjectInvitation { ExpiresAt = DateTime.UtcNow.AddMinutes(-5) };
        expired.IsExpired.Should().BeTrue();
        expired.CanRespond.Should().BeFalse();
        expired.Invoking(invitation => invitation.Accept()).Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only pending, non-expired invitations can be accepted.");
        expired.Invoking(invitation => invitation.Decline()).Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only pending, non-expired invitations can be declined.");

        var accepted = new ProjectInvitation { Status = ProjectInvitationStatus.Accepted };
        accepted.CanRespond.Should().BeFalse();
        accepted.Invoking(invitation => invitation.Accept()).Should().Throw<InvalidOperationException>();
        accepted.Invoking(invitation => invitation.Decline()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProjectInvitationDto_Should_Handle_Missing_Project_Navigation()
    {
        var invitation = new ProjectInvitation
        {
            ProjectId = Guid.NewGuid(),
            InvitedByUserId = Guid.NewGuid(),
            InvitedUserId = Guid.NewGuid(),
            Role = "Viewer",
            Permissions = "read"
        };

        var dto = ProjectInvitationDto.FromInvitation(invitation);

        dto.ProjectTitle.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProjectCollaborator_Should_Return_NotFound_For_Missing_Project()
    {
        var controller = CreateController();

        var result = await controller.AddProjectCollaborator(Guid.NewGuid(), new AddProjectCollaboratorRequest { UserId = Guid.NewGuid() });

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AddProjectCollaborator_Should_RejectUnknownPermissions_AndNormalizeExactValues()
    {
        var tenantId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var project = new Project
        {
            TenantId = tenantId,
            Title = "Permission adapter",
            Slug = "permission-adapter",
            CreatedById = _actorId,
        };
        _context.Projects.Add(project);
        _context.TenantMembers.Add(new TenantMember
        {
            TenantId = tenantId,
            UserId = collaboratorId,
            Role = "Member",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        var controller = CreateController();

        var rejected = await controller.AddProjectCollaborator(project.Id, new AddProjectCollaboratorRequest
        {
            UserId = Guid.NewGuid(),
            Permissions = "read,editor",
        });
        var accepted = await controller.AddProjectCollaborator(project.Id, new AddProjectCollaboratorRequest
        {
            UserId = collaboratorId,
            Permissions = "read|COMMENT|read",
        });

        rejected.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
        accepted.Result.Should().BeOfType<CreatedAtActionResult>();
        (await _context.ProjectCollaborators.SingleAsync()).Permissions.Should().Be("Read,Comment");
    }

    [Fact]
    public async Task RestoreProject_Should_Use_Central_Authorization_And_Clear_Soft_Delete()
    {
        var project = new Project
        {
            TenantId = Guid.NewGuid(),
            Title = "Restore centrally",
            Slug = "restore-centrally",
            CreatedById = _actorId,
            DeletedAt = SystemClock.UtcNow.AddMinutes(-5),
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await CreateController().RestoreProject(project.Id, default);

        result.Result.Should().BeOfType<OkObjectResult>();
        project.DeletedAt.Should().BeNull();
        _authorizationService.Verify(service => service.HasPermissionIncludingDeletedAsync(
            project.Id,
            GameGuild.Identity.Authorization.PermissionType.Restore,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProject_PermanentDelete_RequiresRecentAuthentication()
    {
        _actorContextAccessor.SetupGet(x => x.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = _actorId.ToString(),
            IsAuthenticated = true,
            Roles = new HashSet<string> { "Admin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
        });

        var result = await CreateController().DeleteProject(Guid.NewGuid(), softDelete: false);

        result.Result.Should().BeOfType<ForbidResult>();
        _mediator.Verify(mediator => mediator.Send(
            It.IsAny<IRequest<Result<bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("owner", true)]
    [InlineData("viewer", true)]
    [InlineData("missing", false)]
    public void GetRolePermissions_Should_Return_Known_Roles_And_Reject_Unknown(string role, bool known)
    {
        var controller = CreateController();

        var result = controller.GetRolePermissions(role);

        if (known)
            result.Result.Should().BeOfType<OkObjectResult>();
        else
            result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    private ProjectsController CreateController()
        => new(_mediator.Object, _actorContextAccessor.Object, _context, _authorizationService.Object, NullLogger<ProjectsController>.Instance);
}
