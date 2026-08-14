using GameGuild.ProjectWork;

namespace GameGuild.Projects.UnitTests.ProjectWork;

public sealed class ProjectWorkDomainTests
{
    [Fact]
    public void Create_Should_Seed_The_Five_Default_Kanban_Columns()
    {
        var board = ProjectBoard.Create(Guid.NewGuid(), Guid.NewGuid());

        board.Columns.OrderBy(column => column.Position).Select(column => column.Name)
            .Should().Equal("Backlog", "Ready", "In Progress", "In Review", "Done");
    }

    [Fact]
    public void AddDependency_Should_Reject_A_Cycle()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        var dependencies = new[]
        {
            new ProjectTaskDependency { TaskId = second, DependsOnTaskId = first },
            new ProjectTaskDependency { TaskId = third, DependsOnTaskId = second }
        };

        var action = () => ProjectDependencyGraph.EnsureCanAdd(dependencies, first, third);

        action.Should().Throw<InvalidOperationException>().WithMessage("*cyclic*");
    }

    [Fact]
    public void Complete_Should_Reject_A_Task_With_Incomplete_Dependencies()
    {
        var task = new ProjectWorkTask { Title = "Ship build" };
        var dependency = new ProjectWorkTask { Title = "QA", Status = ProjectWorkTaskStatus.InProgress };

        var action = () => task.Complete([dependency]);

        action.Should().Throw<InvalidOperationException>().WithMessage("*blocked*");
    }

    [Fact]
    public void Complete_Should_Succeed_When_All_Dependencies_Are_Done()
    {
        var task = new ProjectWorkTask { Title = "Ship build" };
        var dependency = new ProjectWorkTask { Title = "QA", Status = ProjectWorkTaskStatus.Done };

        task.Complete([dependency]);

        task.Status.Should().Be(ProjectWorkTaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
    }
}
