using FluentAssertions;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public class CourseStudentManagementTests
{
    [Fact]
    public async Task SendCourseStudentMessageHandler_ShouldOnlyNotifyActiveCourseMembers()
    {
        await using var context = CourseStudentDbContext.Create();
        var courseId = Guid.NewGuid();
        var enrolledUserId = Guid.NewGuid();
        var unrelatedUserId = Guid.NewGuid();
        context.Set<ProgramUser>().Add(new ProgramUser
        {
            Id = Guid.NewGuid(),
            ProgramId = courseId,
            UserId = enrolledUserId,
            IsActive = true,
            JoinedAt = SystemClock.UtcNow,
        });
        await context.SaveChangesAsync();
        var notifications = new Mock<INotificationService>();
        notifications
            .Setup(service => service.SendBulkAsync(
                It.IsAny<IEnumerable<Guid>>(),
                NotificationType.DirectMessage,
                "Milestone update",
                "The critique moved to Friday.",
                NotificationChannel.InApp,
                null,
                It.IsAny<string?>(),
                NotificationPriority.Normal,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<Notification>>([]));
        var handler = new SendCourseStudentMessageCommandHandler(context, notifications.Object);

        var sent = await handler.Handle(
            new SendCourseStudentMessageCommand(
                courseId,
                [enrolledUserId, unrelatedUserId],
                "Milestone update",
                "The critique moved to Friday.",
                null),
            CancellationToken.None);

        sent.Should().Be(1);
        notifications.Verify(service => service.SendBulkAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(new[] { enrolledUserId })),
            NotificationType.DirectMessage,
            "Milestone update",
            "The critique moved to Friday.",
            NotificationChannel.InApp,
            null,
            It.IsAny<string?>(),
            NotificationPriority.Normal,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CourseSupportTicketsController_ShouldScopeTheQueueToTheCourse()
    {
        var courseId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(
                It.Is<GetSupportTicketsQuery>(query => query.CustomerId == courseId && query.Take == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SupportTicketDto>([], 0, 0, 100));
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        var controller = new CourseSupportTicketsController(sender.Object, actor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var response = await controller.List(courseId, cancellationToken: CancellationToken.None);

        response.Result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    private sealed class CourseStudentDbContext(DbContextOptions<CourseStudentDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public static CourseStudentDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CourseStudentDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new CourseStudentDbContext(options);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgramUser>(builder =>
            {
                builder.HasKey(entity => entity.Id);
                builder.Ignore(entity => entity.User);
                builder.Ignore(entity => entity.Program);
                builder.Ignore(entity => entity.ContentInteractions);
                builder.Ignore(entity => entity.ReceivedGrades);
                builder.Ignore(entity => entity.GivenGrades);
                builder.Ignore(entity => entity.ProgramRatings);
            });
        }
    }
}
