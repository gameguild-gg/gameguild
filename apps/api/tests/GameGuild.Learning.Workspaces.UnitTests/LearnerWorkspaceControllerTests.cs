using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Workspaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Learning.Workspaces.UnitTests;

public sealed class LearnerWorkspaceControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IActorContextAccessor> _actor = new();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Dashboard_returns_unauthorized_without_a_user_actor()
    {
        var controller = CreateController(ActorContext.Anonymous);

        var result = await controller.GetDashboard(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Dashboard_dispatches_the_authenticated_actor_query()
    {
        var expected = new LearnerDashboardDto([], [], [], [], [], []);
        _sender
            .Setup(sender => sender.Send<LearnerDashboardDto>(
                It.Is<GetLearnerDashboardQuery>(query => query.UserId == _userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(AuthenticatedActor());

        var result = await controller.GetDashboard(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Course_workspace_returns_unauthorized_without_a_user_actor()
    {
        var controller = CreateController(ActorContext.Anonymous);

        var result = await controller.GetCourseWorkspace(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Course_workspace_returns_not_found_when_query_has_no_accessible_course()
    {
        var courseId = Guid.NewGuid();
        _sender
            .Setup(sender => sender.Send<LearnerCourseWorkspaceDto?>(
                It.Is<GetLearnerCourseWorkspaceQuery>(query =>
                    query.UserId == _userId && query.CourseId == courseId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearnerCourseWorkspaceDto?)null);
        var controller = CreateController(AuthenticatedActor());

        var result = await controller.GetCourseWorkspace(courseId, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Course_workspace_returns_the_dispatched_workspace()
    {
        var courseId = Guid.NewGuid();
        var expected = CreateWorkspace(courseId);
        _sender
            .Setup(sender => sender.Send<LearnerCourseWorkspaceDto?>(
                It.Is<GetLearnerCourseWorkspaceQuery>(query =>
                    query.UserId == _userId && query.CourseId == courseId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(AuthenticatedActor());

        var result = await controller.GetCourseWorkspace(courseId, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Search_returns_unauthorized_without_a_user_actor()
    {
        var controller = CreateController(ActorContext.Anonymous);

        var result = await controller.Search("lesson", cancellationToken: CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Search_normalizes_a_null_query_and_dispatches_the_requested_limit()
    {
        IReadOnlyList<LearnerSearchResultDto> expected = [];
        _sender
            .Setup(sender => sender.Send<IReadOnlyList<LearnerSearchResultDto>>(
                It.Is<SearchLearnerWorkspaceQuery>(query =>
                    query.UserId == _userId && query.Query == string.Empty && query.Take == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(AuthenticatedActor());

        var result = await controller.Search(null!, 7, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public void Module_registration_preserves_the_service_collection()
    {
        var services = new ServiceCollection();

        services.AddLearningWorkspacesModule().Should().BeSameAs(services);
    }

    private LearnerWorkspaceController CreateController(ActorContext context)
    {
        _actor.Setup(accessor => accessor.ActorContext).Returns(context);
        return new LearnerWorkspaceController(_sender.Object, _actor.Object);
    }

    private ActorContext AuthenticatedActor()
    {
        return new ActorContext
        {
            SubjectId = _userId.ToString(),
            TenantId = Guid.NewGuid(),
            ActorKind = ActorKind.User,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true,
        };
    }

    private static LearnerCourseWorkspaceDto CreateWorkspace(Guid courseId)
    {
        var course = new LearnerCourseSummaryDto(
            courseId,
            Guid.NewGuid(),
            "Course",
            "course",
            "Description",
            null,
            "Programming",
            "Beginner",
            8,
            "Active",
            "InProgress",
            10m,
            null,
            DateTime.UtcNow,
            0,
            0,
            0,
            null,
            null,
            null);

        return new LearnerCourseWorkspaceDto(
            course,
            [],
            [],
            null,
            [],
            [],
            [],
            [],
            [],
            []);
    }
}
