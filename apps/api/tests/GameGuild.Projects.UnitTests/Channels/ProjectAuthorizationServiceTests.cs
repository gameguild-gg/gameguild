using GameGuild.API.Database;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;

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
