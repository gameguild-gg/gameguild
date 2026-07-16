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
        mediator.Setup(candidate => candidate.Send(
                It.IsAny<IRequest<Result<ProjectStoreProductProjection>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ProjectStoreProductProjection(Guid.NewGuid(), projectId, productId)));
        var controller = new ProjectStoreProductsController(mediator.Object);

        var result = await controller.Link(projectId, new LinkProjectStoreProductRequest(productId));

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        mediator.Verify(candidate => candidate.Send(
            It.Is<LinkProjectStoreProductCommand>(command => command.ProjectId == projectId && command.ProductId == productId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
