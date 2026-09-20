using System.Security.Claims;
using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Controllers;

public sealed class CourseSupportTicketsControllerCoverageTests
{
    [Theory]
    [InlineData("identity", "Teacher")]
    [InlineData("email", "teacher@example.com")]
    [InlineData("subject", null)]
    public async Task AddMessage_ResolvesActorNameFromAvailableIdentity(string source, string? expectedName)
    {
        var courseId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        expectedName ??= actorId.ToString();
        var ticket = CreateTicket(ticketId, tenantId, courseId);
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(candidate => candidate.Send(
                It.Is<GetSupportTicketByIdQuery>(query => query.TicketId == ticketId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        sender.Setup(candidate => candidate.Send(
                It.Is<AddSupportTicketMessageCommand>(command =>
                    command.TicketId == ticketId &&
                    command.TenantId == tenantId &&
                    command.AuthorUserId == actorId &&
                    command.AuthorName == expectedName &&
                    command.Body == "Reply"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        var actors = new Mock<IActorContextAccessor>(MockBehavior.Strict);
        actors.SetupGet(candidate => candidate.ActorContext).Returns(CreateActor(actorId));
        var claims = source switch
        {
            "identity" => new[] { new Claim(ClaimTypes.Name, "Teacher"), new Claim(ClaimTypes.Email, "teacher@example.com") },
            "email" => new[] { new Claim(ClaimTypes.Email, "teacher@example.com") },
            _ => Array.Empty<Claim>()
        };
        var controller = CreateController(sender.Object, actors.Object, claims);

        var result = await controller.AddMessage(
            courseId,
            ticketId,
            new CourseSupportTicketMessageRequest("  Reply  "),
            default);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(ticket);
        sender.VerifyAll();
        actors.VerifyAll();
    }

    [Fact]
    public async Task AddMessage_UsesStaffFallbackAndRejectsMissingActorId()
    {
        var courseId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = CreateTicket(ticketId, Guid.NewGuid(), courseId);
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(candidate => candidate.Send(
                It.Is<GetSupportTicketByIdQuery>(query => query.TicketId == ticketId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        var actors = new Mock<IActorContextAccessor>(MockBehavior.Strict);
        actors.SetupGet(candidate => candidate.ActorContext).Returns(ActorContext.Anonymous);
        var controller = CreateController(sender.Object, actors.Object, []);
        controller.HttpContext.User = new ClaimsPrincipal();

        var result = await controller.AddMessage(
            courseId,
            ticketId,
            new CourseSupportTicketMessageRequest("Reply"),
            default);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        sender.VerifyAll();
        actors.VerifyAll();
    }

    [Fact]
    public async Task GetById_HidesTicketOwnedByAnotherCourse()
    {
        var requestedCourseId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = CreateTicket(ticketId, Guid.NewGuid(), Guid.NewGuid());
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(candidate => candidate.Send(
                It.Is<GetSupportTicketByIdQuery>(query => query.TicketId == ticketId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        var controller = CreateController(sender.Object, Mock.Of<IActorContextAccessor>(), []);

        var result = await controller.GetById(requestedCourseId, ticketId, default);

        result.Result.Should().BeOfType<NotFoundResult>();
        sender.VerifyAll();
    }

    private static CourseSupportTicketsController CreateController(
        ISender sender,
        IActorContextAccessor actors,
        IReadOnlyCollection<Claim> claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return new CourseSupportTicketsController(sender, actors, Mock.Of<IApplicationDbContext>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static ActorContext CreateActor(Guid actorId) => new()
    {
        ActorKind = ActorKind.User,
        SubjectId = actorId.ToString(),
        IsAuthenticated = true,
        Roles = new HashSet<string>(),
        Permissions = new HashSet<string>()
    };

    private static SupportTicketDto CreateTicket(Guid id, Guid? tenantId, Guid courseId) => new(
        id,
        tenantId,
        courseId,
        "Course",
        Guid.NewGuid(),
        "Learner",
        "learner@example.com",
        "Help",
        null,
        SupportTicketStatus.Open,
        SupportTicketPriority.Normal,
        null,
        null,
        DateTime.UtcNow,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        0,
        []);
}
