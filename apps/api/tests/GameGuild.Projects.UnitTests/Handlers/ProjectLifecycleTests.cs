using GameGuild.API.Database;
using GameGuild.Identity.Context.Actors;
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
        var project = new Project
        {
            Title = "Lifecycle project",
            Slug = $"lifecycle-{Guid.NewGuid():N}",
            Status = ContentStatus.Published,
            Visibility = ContentVisibility.Public
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
        await _context.SaveChangesAsync();
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(_actorId).Build());
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

        var restored = await new ProjectCrudService(_context).RestoreProjectAsync(project.Id);

        restored.Should().BeTrue();
        project.DeletedAt.Should().BeNull();
        link.DeletedAt.Should().Be(deletedAt);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();
}
