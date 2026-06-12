using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Projects.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Projects.UnitTests.Controllers;

public class ProjectsControllerTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IActorContextAccessor> _actorContextAccessor = new();
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
    }

    [Fact]
    public async Task GetProjects_Should_Send_Filtered_Query()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<IRequest<Result<IEnumerable<Project>>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<Project>>([new Project { Title = "Project", Slug = "project" }]));

        var controller = CreateController();

        var result = await controller.GetProjects(ProjectType.Game, ContentStatus.Published, ContentVisibility.Public, searchTerm: "game", take: 1000);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mediator.Verify(x => x.Send(
            It.Is<GetAllProjectsQuery>(q =>
                q.Type == ProjectType.Game &&
                q.Status == ContentStatus.Published &&
                q.Visibility == ContentVisibility.Public &&
                q.SearchTerm == "game" &&
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
        var projectId = Guid.NewGuid();
        var inviteeId = _actorId;
        var declinedToken = "decline-token";
        var acceptedToken = "accept-token";
        _context.Projects.Add(new Project
        {
            Id = projectId,
            Title = "Launch Candidate",
            Slug = "launch-candidate",
            CreatedById = Guid.NewGuid(),
            Visibility = ContentVisibility.Public,
            Status = ContentStatus.Published
        });
        _context.ProjectInvitations.AddRange(
            new ProjectInvitation
            {
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
                ProjectId = projectId,
                InvitedUserId = inviteeId,
                InvitedByUserId = Guid.NewGuid(),
                Role = "Viewer",
                Permissions = "read",
                Token = declinedToken,
                Status = ProjectInvitationStatus.Pending
            });
        await _context.SaveChangesAsync();

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
        => new(_mediator.Object, _actorContextAccessor.Object, _context, NullLogger<ProjectsController>.Instance);
}
