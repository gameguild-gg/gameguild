using GameGuild.API.Database;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Projects.UnitTests.Handlers;

public sealed class ProjectLifecycleTests : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _actorId = Guid.NewGuid();

    public ProjectLifecycleTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"project-lifecycle-{Guid.NewGuid():N}")
                .Options);
    }

    [Fact]
    public async Task DeleteCommand_ShouldSoftDeleteProjectAndActiveStoreAssociations()
    {
        var tenantId = Guid.NewGuid();
        var project = new Project
        {
            Title = "Lifecycle project",
            Slug = $"lifecycle-{Guid.NewGuid():N}",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public,
            TenantId = tenantId
        };
        var link = new ProjectStoreProduct
        {
            ProjectId = project.Id,
            ProductId = Guid.NewGuid()
        };
        _context.Set<Project>().Add(project);
        _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = _actorId,
            Role = ProjectRoles.Owner,
            Permissions = "Delete",
            IsActive = true
        });
        _context.Set<ProjectStoreProduct>().Add(link);
        _context.Set<User>().Add(new User
        {
            Id = _actorId,
            Email = $"{_actorId:N}@example.com",
            Name = "Lifecycle owner",
            IsActive = true
        });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = tenantId,
            Role = "Member",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(_actorId).WithTenantId(tenantId).Build());
        var handler = new ProjectCommandHandlers(
            _context,
            actorAccessor,
            NullLogger<ProjectCommandHandlers>.Instance);

        var result = await handler.Handle(
            new DeleteProjectCommand { ProjectId = project.Id, DeletedBy = _actorId, SoftDelete = true },
            default);

        result.IsSuccess.Should().BeTrue();
        project.DeletedAt.Should().NotBeNull();
        link.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCommand_ShouldRejectPermanentDeletionWithoutRecentAuthentication()
    {
        var tenantId = Guid.NewGuid();
        var project = new Project
        {
            Title = "Permanent deletion",
            Slug = $"permanent-{Guid.NewGuid():N}",
            TenantId = tenantId,
            CreatedById = _actorId,
        };
        _context.Set<Project>().Add(project);
        _context.Set<ProjectCollaborator>().Add(new ProjectCollaborator
        {
            ProjectId = project.Id,
            UserId = _actorId,
            Role = ProjectRoles.Owner,
            Permissions = "Delete",
            IsActive = true,
        });
        _context.Set<User>().Add(new User { Id = _actorId, IsActive = true });
        _context.Set<TenantMember>().Add(new TenantMember
        {
            UserId = _actorId,
            TenantId = tenantId,
            Role = "Member",
            IsActive = true,
        });
        await _context.SaveChangesAsync();
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(_actorId).WithTenantId(tenantId).Build());
        var handler = new ProjectCommandHandlers(
            _context,
            actorAccessor,
            NullLogger<ProjectCommandHandlers>.Instance);

        var result = await handler.Handle(
            new DeleteProjectCommand { ProjectId = project.Id, DeletedBy = _actorId, SoftDelete = false },
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.ReauthenticationRequired");
        (await _context.Set<Project>().FindAsync(project.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Restore_ShouldNotReactivateClosedChannelAssociations()
    {
        var deletedAt = DateTime.UtcNow.AddHours(-1);
        var project = new Project
        {
            Title = "Restored project",
            Slug = $"restored-{Guid.NewGuid():N}",
            DeletedAt = deletedAt
        };
        var link = new ProjectStoreProduct
        {
            ProjectId = project.Id,
            ProductId = Guid.NewGuid(),
            DeletedAt = deletedAt
        };
        _context.AddRange(project, link);
        await _context.SaveChangesAsync();

        var authorization = new Mock<IProjectAuthorizationService>();
        authorization
            .Setup(service => service.HasPermissionIncludingDeletedAsync(project.Id, PermissionType.Restore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var restored = await new ProjectCrudService(_context, authorization.Object, new ActorContextAccessor())
            .RestoreProjectAsync(project.Id);

        restored.Should().BeTrue();
        project.DeletedAt.Should().BeNull();
        link.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public async Task HardDelete_ShouldRemoveProjectAndAllStoreAssociations()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Hard delete project",
            Slug = $"hard-delete-{Guid.NewGuid():N}"
        };
        var activeLink = new ProjectStoreProduct
        {
            ProjectId = project.Id,
            ProductId = Guid.NewGuid()
        };
        var historicalLink = new ProjectStoreProduct
        {
            ProjectId = project.Id,
            ProductId = Guid.NewGuid(),
            DeletedAt = SystemClock.UtcNow.AddDays(-1)
        };
        _context.AddRange(project, activeLink, historicalLink);
        await _context.SaveChangesAsync();

        var deleted = await new ProjectLifecycleCoordinator(
                _context,
                [new ProjectStoreProductLifecycleParticipant(_context)])
            .DeleteAsync(project.Id, softDelete: false);

        deleted.Should().BeTrue();
        (await _context.Set<Project>().IgnoreQueryFilters().AnyAsync(candidate => candidate.Id == project.Id))
            .Should().BeFalse();
        (await _context.Set<ProjectStoreProduct>().IgnoreQueryFilters().AnyAsync(link => link.ProjectId == project.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentDeleteAndStoreLink_ShouldNotLeaveActiveLinkOnDeletedProject()
    {
        var databaseName = $"project-store-race-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;
        var tenantId = Guid.NewGuid();
        var project = new Project
        {
            Title = "Store race",
            Slug = $"store-race-{Guid.NewGuid():N}",
            TenantId = tenantId,
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public
        };
        var product = Product.Create("Store race product", creatorId: _actorId, tenantId: tenantId);
        product.IsPublished = true;
        await using (var setup = new ApplicationDbContext(options))
        {
            setup.AddRange(project, product);
            await setup.SaveChangesAsync();
        }

        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(_actorId).WithTenantId(tenantId).Build());
        var authorization = new BlockingAuthorizationService();
        await using var linkContext = new ApplicationDbContext(options);
        var linkHandler = new ProjectStoreProductHandlers(
            linkContext,
            actorAccessor,
            new ProjectChannelAvailabilityService(linkContext),
            authorization,
            NullLogger<ProjectStoreProductHandlers>.Instance);
        var linkTask = linkHandler.Handle(new LinkProjectStoreProductCommand(project.Id, product.Id), default);
        await authorization.PermissionRequested;

        await using var deleteContext = new ApplicationDbContext(options);
        var deleteTask = new ProjectLifecycleCoordinator(
                deleteContext,
                [new ProjectStoreProductLifecycleParticipant(deleteContext)])
            .DeleteAsync(project.Id, softDelete: true);
        await Task.WhenAny(deleteTask, Task.Delay(TimeSpan.FromSeconds(2)));
        authorization.AllowPermission();

        (await linkTask).IsSuccess.Should().BeTrue();
        (await deleteTask).Should().BeTrue();
        await using var verify = new ApplicationDbContext(options);
        (await verify.Set<Project>().IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == project.Id))
            .DeletedAt.Should().NotBeNull();
        (await verify.Set<ProjectStoreProduct>().AnyAsync(link =>
            link.ProjectId == project.Id && link.DeletedAt == null)).Should().BeFalse();
    }

    private sealed class BlockingAuthorizationService : IProjectAuthorizationService
    {
        private readonly TaskCompletionSource _permissionRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _permissionAllowed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PermissionRequested => _permissionRequested.Task;

        public Task<bool> IsActorActiveTenantMemberAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public async Task<bool> HasPermissionAsync(
            Guid projectId,
            PermissionType permission,
            CancellationToken cancellationToken = default)
        {
            _permissionRequested.TrySetResult();
            await _permissionAllowed.Task.WaitAsync(cancellationToken);
            return true;
        }

        public void AllowPermission() => _permissionAllowed.TrySetResult();
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();
}
