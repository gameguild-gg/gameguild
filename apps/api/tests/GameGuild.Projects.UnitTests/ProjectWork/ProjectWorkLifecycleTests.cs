using GameGuild.API.Database;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.ProjectWork;

namespace GameGuild.Projects.UnitTests.ProjectWork;

public sealed class ProjectWorkLifecycleTests : IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IProjectAuthorizationService> _authorization = new();
    private readonly Mock<IActorContextAccessor> _actors = new();

    public ProjectWorkLifecycleTests()
    {
        _authorization.Setup(service => service.HasPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<PermissionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _actors.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = _actorId.ToString(),
            TenantId = _tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true,
        });
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Service_ExposesTheCompleteProjectWorkLifecycleAndRecordsHistory()
    {
        var project = new Project
        {
            TenantId = _tenantId,
            Title = "Project Work",
            Slug = "project-work",
            CreatedById = _actorId,
        };
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();
        var service = new ProjectWorkService(_context, _actors.Object, _authorization.Object);
        var board = (await service.GetBoardAsync(project.Id, true)).Value;

        var customColumn = (await service.ConfigureColumnAsync(
            project.Id, null, "Blocked", ProjectWorkColumnKind.Custom, 5, 2)).Value;
        var milestone = (await service.CreateMilestoneAsync(
            project.Id, "Vertical slice", "Playable build", SystemClock.UtcNow.AddDays(14))).Value;
        var first = (await service.CreateTaskAsync(project.Id, new CreateProjectWorkTask(
            board.Columns.First().Id, "Prototype", null, ProjectWorkTaskPriority.High, null, milestone.Id, null))).Value;
        var second = (await service.CreateTaskAsync(project.Id, new CreateProjectWorkTask(
            customColumn.Id, "QA", null, ProjectWorkTaskPriority.Normal, null, milestone.Id, null))).Value;
        var updated = await service.UpdateTaskAsync(project.Id, first.Id, new UpdateProjectWorkTask(
            "Prototype loop", "Validated loop", ProjectWorkTaskPriority.Urgent, null, milestone.Id, null));
        var label = (await service.CreateLabelAsync(project.Id, "Gameplay", "#22c55e")).Value;
        var labelAssignment = await service.AssignLabelAsync(project.Id, first.Id, label.Id);
        var checklist = (await service.AddChecklistItemAsync(project.Id, first.Id, "Controller support")).Value;
        var checklistUpdate = await service.SetChecklistCompletionAsync(project.Id, first.Id, checklist.Id, true);
        var comment = (await service.AddCommentAsync(project.Id, first.Id, "Initial note")).Value;
        var commentUpdate = await service.UpdateCommentAsync(project.Id, first.Id, comment.Id, "Updated note");
        var dependency = (await service.AddDependencyAsync(project.Id, second.Id, first.Id)).Value;
        var details = await service.GetTaskDetailsAsync(project.Id, first.Id);
        var history = await service.GetHistoryAsync(project.Id, 100);

        updated.IsSuccess.Should().BeTrue();
        updated.Value.Title.Should().Be("Prototype loop");
        updated.Value.Priority.Should().Be(ProjectWorkTaskPriority.Urgent);
        labelAssignment.IsSuccess.Should().BeTrue();
        checklistUpdate.IsSuccess.Should().BeTrue();
        checklistUpdate.Value.IsCompleted.Should().BeTrue();
        commentUpdate.IsSuccess.Should().BeTrue();
        commentUpdate.Value.Body.Should().Be("Updated note");
        dependency.TaskId.Should().Be(second.Id);
        details.IsSuccess.Should().BeTrue();
        details.Value.Checklist.Should().ContainSingle();
        details.Value.Comments.Should().ContainSingle();
        details.Value.Labels.Should().ContainSingle(labelValue => labelValue.Id == label.Id);
        history.IsSuccess.Should().BeTrue();
        history.Value.Select(item => item.Action).Should().Contain(
            "ColumnCreated", "MilestoneCreated", "TaskCreated", "TaskUpdated",
            "LabelCreated", "LabelAssigned", "ChecklistItemAdded", "ChecklistItemUpdated",
            "CommentAdded", "CommentUpdated", "DependencyAdded");
    }

    [Fact]
    public async Task MoveTask_EnforcesTargetWipLimit_AndDeleteRemovesDependencyEdges()
    {
        var project = new Project
        {
            TenantId = _tenantId,
            Title = "WIP",
            Slug = "wip",
            CreatedById = _actorId,
        };
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();
        var service = new ProjectWorkService(_context, _actors.Object, _authorization.Object);
        var board = (await service.GetBoardAsync(project.Id, true)).Value;
        var source = board.Columns.First();
        var limited = (await service.ConfigureColumnAsync(
            project.Id, null, "Limited", ProjectWorkColumnKind.Custom, board.Columns.Count, 1)).Value;
        var dependency = (await service.CreateTaskAsync(project.Id, new CreateProjectWorkTask(
            source.Id, "Dependency", null, ProjectWorkTaskPriority.Normal, null, null, null))).Value;
        var blocked = (await service.CreateTaskAsync(project.Id, new CreateProjectWorkTask(
            source.Id, "Blocked", null, ProjectWorkTaskPriority.Normal, null, null, null))).Value;
        var occupant = (await service.CreateTaskAsync(project.Id, new CreateProjectWorkTask(
            limited.Id, "Occupant", null, ProjectWorkTaskPriority.Normal, null, null, null))).Value;
        await service.AddDependencyAsync(project.Id, blocked.Id, dependency.Id);

        var move = await service.MoveTaskAsync(project.Id, blocked.Id, limited.Id, 0);
        move.IsFailure.Should().BeTrue();
        move.Error.Code.Should().Be("ProjectWork.WipLimit");
        (await _context.Set<ProjectWorkTask>().FindAsync(blocked.Id))!.ColumnId.Should().Be(source.Id);

        (await service.DeleteTaskAsync(project.Id, dependency.Id)).IsSuccess.Should().BeTrue();
        (await _context.Set<ProjectTaskDependency>().ToListAsync()).Should().BeEmpty();
        occupant.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ConfigureColumn_ReordersEveryColumnIntoUniqueContiguousPositions()
    {
        var project = new Project
        {
            TenantId = _tenantId,
            Title = "Column ordering",
            Slug = "column-ordering",
            CreatedById = _actorId,
        };
        _context.Set<Project>().Add(project);
        await _context.SaveChangesAsync();
        var service = new ProjectWorkService(_context, _actors.Object, _authorization.Object);
        var board = (await service.GetBoardAsync(project.Id, true)).Value;
        var last = board.Columns.OrderBy(column => column.Position).Last();

        var updated = await service.ConfigureColumnAsync(
            project.Id, last.Id, last.Name, last.Kind, 1, last.WorkInProgressLimit);

        updated.IsSuccess.Should().BeTrue();
        var positions = await _context.Set<ProjectWorkColumn>()
            .Where(column => column.BoardId == board.Id && column.DeletedAt == null)
            .OrderBy(column => column.Position)
            .Select(column => column.Position)
            .ToArrayAsync();
        positions.Should().Equal(Enumerable.Range(0, positions.Length));
        positions.Should().OnlyHaveUniqueItems();
        updated.Value.Position.Should().Be(1);
    }
}
