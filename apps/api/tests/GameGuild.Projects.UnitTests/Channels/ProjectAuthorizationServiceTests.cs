using GameGuild.API.Database;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using ResourceTenantId = GameGuild.CQRS.Models.TenantId;

namespace GameGuild.Projects.UnitTests.Channels;

public sealed class ProjectAuthorizationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public ProjectAuthorizationServiceTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(_actorId).WithTenantId(_tenantId).Build());
    }

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData(ContentStatus.Draft, false)]
    [InlineData(ContentStatus.Published, true)]
    public async Task HasPermission_Read_Should_Expose_Only_Published_Public_Projects_To_Anonymous_Users(
        ContentStatus status,
        bool expected)
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.Create().Build());
        var project = new Project
        {
            Title = "Public project",
            Slug = Guid.NewGuid().ToString(),
            TenantId = _tenantId,
            CreatedById = Guid.NewGuid(),
            Visibility = ContentVisibility.Public,
            Status = status
        };
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasPermissionAsync(project.Id, PermissionType.Read);

        allowed.Should().Be(expected);
    }

    [Theory]
    [InlineData("Read", true)]
    [InlineData("ReadAll", false)]
    [InlineData("Edit", false)]
    public async Task HasPermission_Read_Should_Require_An_Exact_Active_Collaborator_Permission(
        string permissions,
        bool expected)
    {
        var project = AddPrivateProject();
        AddIdentity();
        _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = _actorId,
            Role = ProjectRoles.Viewer,
            Permissions = permissions,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasPermissionAsync(project.Id, PermissionType.Read);

        allowed.Should().Be(expected);
    }

    [Fact]
    public async Task HasPermission_Read_Should_Allow_An_Active_Project_Team_Member()
    {
        var project = AddPrivateProject();
        AddIdentity();
        var team = new Team { Name = "Project team", IsActive = true };
        _context.Set<Team>().Add(team);
        _context.Set<TeamMember>().Add(new TeamMember
        {
            TeamId = team.Id,
            UserId = _actorId,
            IsActive = true
        });
        _context.Set<ProjectTeam>().Add(new ProjectTeam
        {
            ProjectId = project.Id,
            TeamId = team.Id,
            IsActive = true,
            Permissions = "Read"
        });
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasPermissionAsync(project.Id, PermissionType.Read);

        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task HasPermission_Read_Should_Honor_Only_Effective_Direct_Project_Grants(
        bool isActive,
        bool isExpired,
        bool expected)
    {
        var project = AddPrivateProject();
        AddIdentity();
        _context.Set<ResourceUserPermission>().Add(new ResourceUserPermission
        {
            TenantId = new ResourceTenantId(_tenantId),
            UserId = _actorId,
            ResourceType = nameof(Project),
            ResourceId = project.Id.ToString(),
            Permissions = [PermissionType.Read.ToString()],
            GrantedByUserId = Guid.NewGuid(),
            RevokedAt = isActive ? null : DateTime.UtcNow,
            ExpiresAt = isExpired ? DateTime.UtcNow.AddMinutes(-1) : null
        });
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasPermissionAsync(project.Id, PermissionType.Read);

        allowed.Should().Be(expected);
    }

    [Fact]
    public async Task Direct_Grant_Should_Not_Reactivate_A_Removed_Collaborator()
    {
        var project = AddCreatorProject();
        AddIdentity();
        var targetUser = new User
        {
            Email = $"target-{Guid.NewGuid():N}@example.test",
            Name = "Target user",
            IsActive = true,
        };
        _context.Set<User>().Add(targetUser);
        var removedCollaborator = new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = targetUser.Id,
            Role = ProjectRoles.Viewer,
            Permissions = "Read",
            IsActive = false,
            LeftAt = DateTime.UtcNow,
        };
        _context.Set<ProjectCollaborator>().Add(removedCollaborator);
        await _context.SaveChangesAsync();
        var canonicalPermissions = new Mock<GameGuild.Identity.Authorization.IResourcePermissionService>();
        var service = new ProjectResourcePermissionService(
            _context,
            CreateService(),
            canonicalPermissions.Object);

        var result = await service.InviteUserToResourceAsync(
            "projects",
            project.Id,
            new InviteUserRequest
            {
                Email = targetUser.Email,
                Permissions = [PermissionType.Read],
                RequireAcceptance = false,
            },
            _actorId);

        result.Success.Should().BeTrue();
        removedCollaborator.IsActive.Should().BeFalse();
        (await _context.Set<ResourceUserPermission>().SingleAsync(grant =>
            grant.UserId == targetUser.Id && grant.ResourceId == project.Id.ToString()))
            .Permissions.Should().ContainSingle().Which.Should().Be(nameof(PermissionType.Read));
    }

    [Fact]
    public async Task HasPermission_Should_Allow_Active_User_With_Active_Selected_Tenant_Membership()
    {
        var project = AddCreatorProject();
        AddIdentity();
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasPermissionAsync(project.Id, PermissionType.Edit);

        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, false, true, false, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, false, true, false, true)]
    public async Task HasPermission_Should_Deny_Invalid_Identity_Or_Membership(
        bool userActive,
        bool userDeleted,
        bool membershipActive,
        bool membershipDeleted,
        bool wrongTenant)
    {
        var project = AddCreatorProject();
        AddIdentity(userActive, userDeleted, membershipActive, membershipDeleted, wrongTenant);
        await _context.SaveChangesAsync();

        var allowed = await CreateService().HasPermissionAsync(project.Id, PermissionType.Edit);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyReadAccess_Should_Not_Trust_A_Stale_TenantAdmin_Claim()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(_actorId)
                .WithTenantId(_tenantId)
                .WithRole("TenantAdmin")
                .Build());
        var project = AddPrivateProject();
        AddIdentity(membershipActive: false);
        await _context.SaveChangesAsync();

        var visible = await CreateService().ApplyReadAccess(_context.Set<Project>()).ToListAsync();

        visible.Should().NotContain(item => item.Id == project.Id);
    }

    [Fact]
    public async Task ApplyReadAccess_Should_Not_Trust_A_Stale_SystemAdmin_Claim()
    {
        _actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(ActorContextBuilder.ForUser(_actorId)
                .WithTenantId(_tenantId)
                .WithRole("SystemAdmin")
                .Build());
        var project = AddPrivateProject();
        AddIdentity(userActive: false);
        await _context.SaveChangesAsync();

        var visible = await CreateService().ApplyReadAccess(_context.Set<Project>()).ToListAsync();

        visible.Should().NotContain(item => item.Id == project.Id);
    }

    private ProjectAuthorizationService CreateService() => new(_context, _actorAccessor.Object);

    private Project AddCreatorProject()
    {
        var project = new Project
        {
            Title = "Owned project",
            Slug = Guid.NewGuid().ToString(),
            TenantId = _tenantId,
            CreatedById = _actorId
        };
        _context.Set<Project>().Add(project);
        return project;
    }

    private Project AddPrivateProject()
    {
        var project = new Project
        {
            Title = "Private project",
            Slug = Guid.NewGuid().ToString(),
            TenantId = _tenantId,
            CreatedById = Guid.NewGuid(),
            Visibility = ContentVisibility.Private,
            Status = ContentStatus.Draft
        };
        _context.Set<Project>().Add(project);
        return project;
    }

    private void AddIdentity(
        bool userActive = true,
        bool userDeleted = false,
        bool membershipActive = true,
        bool membershipDeleted = false,
        bool wrongTenant = false)
    {
        _context.Set<User>().Add(new User
        {
            Id = _actorId,
            Email = $"{_actorId:N}@example.com",
            Name = "Channel actor",
            IsActive = userActive,
            DeletedAt = userDeleted ? DateTime.UtcNow : null
        });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = wrongTenant ? Guid.NewGuid() : _tenantId,
            Role = "Member",
            IsActive = membershipActive,
            DeletedAt = membershipDeleted ? DateTime.UtcNow : null
        });
    }
}
