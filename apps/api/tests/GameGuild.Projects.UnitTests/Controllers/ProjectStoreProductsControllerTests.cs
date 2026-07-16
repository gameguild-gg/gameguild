using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Projects.UnitTests.Controllers;

public sealed class ProjectStoreProductsControllerTests
{
    [Fact]
    public async Task Link_Should_Delegate_Through_Cqrs()
    {
        var mediator = new Mock<IMediator>();
        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<IRequest<Result<ProjectStoreProductProjection>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ProjectStoreProductProjection(Guid.NewGuid(), projectId, productId)));
        var controller = new ProjectStoreProductsController(mediator.Object);

        var result = await controller.Link(projectId, new LinkProjectStoreProductRequest(productId), cancellation.Token);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        mediator.Verify(candidate => candidate.Send(
            It.Is<LinkProjectStoreProductCommand>(command => command.ProjectId == projectId && command.ProductId == productId),
            cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Actions_Should_Forward_CancellationToken_To_Cqrs()
    {
        var mediator = new Mock<IMediator>();
        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<IRequest<Result<IReadOnlyList<ProjectStoreProductProjection>>>>(),
                cancellation.Token))
            .ReturnsAsync(Result.Success<IReadOnlyList<ProjectStoreProductProjection>>([]));
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<IRequest<Result<bool>>>(),
                cancellation.Token))
            .ReturnsAsync(Result.Success(true));
        var controller = new ProjectStoreProductsController(mediator.Object);

        await controller.List(projectId, cancellation.Token);
        await controller.Unlink(projectId, productId, cancellation.Token);
        await controller.ListPublicProductProjects(productId, cancellation.Token);

        mediator.Verify(candidate => candidate.Send(
            It.Is<GetProjectStoreProductsQuery>(query => query.ProjectId == projectId),
            cancellation.Token), Times.Once);
        mediator.Verify(candidate => candidate.Send(
            It.Is<UnlinkProjectStoreProductCommand>(command => command.ProjectId == projectId && command.ProductId == productId),
            cancellation.Token), Times.Once);
        mediator.Verify(candidate => candidate.Send(
            It.Is<GetPublicStoreProductProjectsQuery>(query => query.ProductId == productId),
            cancellation.Token), Times.Once);
    }
}
