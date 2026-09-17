using System.Security.Claims;
using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Controllers;

public sealed class CourseControllerCoverageTests
{
    [Fact]
    public async Task PrerequisitesController_RejectsAnonymousCheck()
    {
        var service = new Mock<IPrerequisiteService>(MockBehavior.Strict);
        var actors = new Mock<IActorContextAccessor>(MockBehavior.Strict);
        actors.SetupGet(candidate => candidate.ActorContext).Returns(ActorContext.Anonymous);
        var controller = new PrerequisitesController(
            service.Object,
            actors.Object,
            NullLogger<PrerequisitesController>.Instance,
            Mock.Of<ISender>());

        var result = await controller.CheckPrerequisites(Guid.NewGuid());

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        actors.VerifyAll();
    }

    [Fact]
    public async Task PrerequisitesController_MapsCurrentActorCheckResult()
    {
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var status = new PrerequisiteStatus(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Foundations",
            PrerequisiteType.Required,
            true,
            Percent(70),
            Percent(85),
            null);
        var service = new Mock<IPrerequisiteService>(MockBehavior.Strict);
        service.Setup(candidate => candidate.CheckPrerequisitesAsync(courseId, userId, tenantId))
            .ReturnsAsync(new PrerequisiteCheckResult(true, [status]));
        var actors = new Mock<IActorContextAccessor>(MockBehavior.Strict);
        actors.SetupGet(candidate => candidate.ActorContext).Returns(CreateActor(userId, tenantId));
        var controller = new PrerequisitesController(
            service.Object,
            actors.Object,
            NullLogger<PrerequisitesController>.Instance,
            Mock.Of<ISender>());

        var result = await controller.CheckPrerequisites(courseId);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PrerequisiteCheckResultDto>().Subject;
        dto.IsSatisfied.Should().BeTrue();
        dto.Prerequisites.Should().ContainSingle().Which.CourseName.Should().Be("Foundations");
        service.VerifyAll();
        actors.VerifyAll();
    }

    [Fact]
    public async Task CourseCheckoutController_RejectsMissingUserIdentity()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var controller = CreateCheckoutController(sender.Object, new ClaimsPrincipal());

        var result = await controller.CompleteCheckout(
            Guid.NewGuid(),
            new CompleteCourseCheckoutRequest(Guid.NewGuid()),
            default);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, typeof(BadRequestObjectResult))]
    [InlineData(StatusCodes.Status404NotFound, typeof(NotFoundObjectResult))]
    [InlineData(StatusCodes.Status409Conflict, typeof(ConflictObjectResult))]
    public async Task CourseCheckoutController_MapsFailureStatusCodes(int statusCode, Type expectedResultType)
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(candidate => candidate.Send(
                It.Is<CompleteCourseCheckoutCommand>(command =>
                    command.CourseId == courseId && command.UserId == userId && command.ProductId == productId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompleteCourseCheckoutOutcome.Failure(statusCode, "Failure", "Details"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test"));
        var controller = CreateCheckoutController(sender.Object, principal);

        var result = await controller.CompleteCheckout(
            courseId,
            new CompleteCourseCheckoutRequest(productId),
            default);

        result.Result.Should().BeOfType(expectedResultType);
        sender.VerifyAll();
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("userId")]
    public async Task CourseCheckoutController_AcceptsFallbackUserClaims(string claimType)
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var response = new CompleteCourseCheckoutResponse(
            courseId,
            productId,
            Guid.NewGuid(),
            [],
            false,
            0,
            "USD",
            "/course",
            null);
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(candidate => candidate.Send(
                It.Is<CompleteCourseCheckoutCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompleteCourseCheckoutOutcome.Success(response));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(claimType, userId.ToString())],
            "test"));
        var controller = CreateCheckoutController(sender.Object, principal);

        var result = await controller.CompleteCheckout(
            courseId,
            new CompleteCourseCheckoutRequest(productId),
            default);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        sender.VerifyAll();
    }

    [Fact]
    public void CourseServicesAndCheckoutHandler_ConstructWithRequiredDependencies()
    {
        var prerequisiteService = new PrerequisiteService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<PrerequisiteService>.Instance);
        var checkoutHandler = new CompleteCourseCheckoutCommandHandler(
            Mock.Of<IProgramCrudService>(),
            Mock.Of<IProgramEnrollmentService>(),
            Mock.Of<IEntitlementService>(),
            Mock.Of<IProductRepository>());

        prerequisiteService.Should().NotBeNull();
        checkoutHandler.Should().NotBeNull();
    }

    private static CourseCheckoutController CreateCheckoutController(ISender sender, ClaimsPrincipal user) =>
        new(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

    private static ActorContext CreateActor(Guid userId, Guid? tenantId) => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = userId.ToString(),
        TenantId = tenantId,
        IsAuthenticated = true,
        Roles = new HashSet<string>(),
        Permissions = new HashSet<string>()
    };
}
