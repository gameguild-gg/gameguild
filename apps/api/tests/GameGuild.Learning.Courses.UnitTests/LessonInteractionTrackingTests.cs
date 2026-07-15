using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class LessonInteractionTrackingTests
{
    [Fact]
    public void AddTimeSpentSeconds_ShouldRetainExactTimeAndMaintainMinuteCompatibility()
    {
        var interaction = CreateInteraction();

        interaction.AddTimeSpentSeconds(45);
        interaction.AddTimeSpentSeconds(75);

        interaction.TimeSpentSeconds.Should().Be(120);
        interaction.TimeSpentMinutes.Should().Be(2);
    }

    [Fact]
    public void AddTimeSpent_ShouldAlsoUpdateExactSeconds()
    {
        var interaction = CreateInteraction();

        interaction.AddTimeSpent(3);

        interaction.TimeSpentSeconds.Should().Be(180);
        interaction.TimeSpentMinutes.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTimeSpentSeconds_WhenDurationIsNotPositive_ShouldRejectIt(int seconds)
    {
        var interaction = CreateInteraction();

        var action = () => interaction.AddTimeSpentSeconds(seconds);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Reset_ShouldClearSecondAndMinuteCounters()
    {
        var interaction = CreateInteraction();
        interaction.AddTimeSpentSeconds(125);

        interaction.Reset();

        interaction.TimeSpentSeconds.Should().Be(0);
        interaction.TimeSpentMinutes.Should().Be(0);
    }

    [Fact]
    public void CreateEvent_WhenPayloadIsNotJson_ShouldRejectIt()
    {
        var action = () => ContentInteractionEvent.Create(
            Guid.NewGuid(),
            ContentInteractionEventType.QuizAnswered,
            payload: "not-json");

        action.Should().Throw<ArgumentException>()
            .WithMessage("Event payload must be valid JSON.*");
    }

    [Fact]
    public async Task RecordEventHandler_WhenHeartbeatIsRetried_ShouldBeIdempotent()
    {
        await using var context = TrackingTestDbContext.Create();
        var lesson = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Title = "Interactive lesson",
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Video,
        };
        var interaction = CreateInteraction();
        interaction.ContentId = lesson.Id;
        interaction.Content = lesson;
        context.Set<ProgramContent>().Add(lesson);
        context.Set<ContentInteraction>().Add(interaction);
        await context.SaveChangesAsync();
        var handler = new RecordContentInteractionEventCommandHandler(context);
        var command = new RecordContentInteractionEventCommand(
            lesson.ProgramId,
            interaction.Id,
            ContentInteractionEventType.Heartbeat,
            DurationSeconds: 45,
            PositionSeconds: 90.5m,
            ProgressPercentage: 25,
            Payload: """{"player":"html5"}""",
            IdempotencyKey: "heartbeat-0001");

        var first = await handler.Handle(command, CancellationToken.None);
        var retried = await handler.Handle(command, CancellationToken.None);

        first.Id.Should().Be(retried.Id);
        (await context.Set<ContentInteractionEvent>().CountAsync()).Should().Be(1);
        var persistedInteraction = await context.Set<ContentInteraction>().SingleAsync();
        persistedInteraction.TimeSpentSeconds.Should().Be(45);
        persistedInteraction.ProgressPercentage.Should().Be(25);
        persistedInteraction.BookmarkPosition.Should().Be("video:90.5");
    }

    [Fact]
    public async Task EventsController_ShouldDispatchRecordThroughCqrs()
    {
        var programId = Guid.NewGuid();
        var interactionId = Guid.NewGuid();
        var expected = new ContentInteractionEventDto(
            Guid.NewGuid(),
            interactionId,
            ContentInteractionEventType.Opened,
            SystemClock.UtcNow,
            null,
            null,
            null,
            null,
            "open-1");
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(
                It.Is<RecordContentInteractionEventCommand>(command =>
                    command.ProgramId == programId &&
                    command.InteractionId == interactionId &&
                    command.Type == ContentInteractionEventType.Opened &&
                    command.IdempotencyKey == "open-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new LessonInteractionEventsController(sender.Object);

        var response = await controller.Record(
            programId,
            interactionId,
            new RecordContentInteractionEventRequest(
                ContentInteractionEventType.Opened,
                IdempotencyKey: "open-1"),
            CancellationToken.None);

        var ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetEventsHandler_ShouldReturnTheInteractionTimelineInOccurrenceOrder()
    {
        await using var context = TrackingTestDbContext.Create();
        var lesson = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Title = "Lesson",
            Type = ProgramContentType.Lesson,
        };
        var interaction = CreateInteraction();
        interaction.ContentId = lesson.Id;
        interaction.Content = lesson;
        context.Set<ProgramContent>().Add(lesson);
        context.Set<ContentInteraction>().Add(interaction);
        var later = ContentInteractionEvent.Create(
            interaction.Id,
            ContentInteractionEventType.Paused,
            positionSeconds: 20,
            occurredAt: new DateTime(2026, 7, 15, 12, 1, 0, DateTimeKind.Utc));
        var earlier = ContentInteractionEvent.Create(
            interaction.Id,
            ContentInteractionEventType.Opened,
            occurredAt: new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
        context.Set<ContentInteractionEvent>().AddRange(later, earlier);
        await context.SaveChangesAsync();
        var handler = new GetContentInteractionEventsQueryHandler(context);

        var result = await handler.Handle(
            new GetContentInteractionEventsQuery(lesson.ProgramId, interaction.Id),
            CancellationToken.None);

        result.Select(item => item.Type).Should().Equal(
            ContentInteractionEventType.Opened,
            ContentInteractionEventType.Paused);
    }

    private static ContentInteraction CreateInteraction() =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ContentId = Guid.NewGuid(),
            ProgramUserId = Guid.NewGuid(),
        };

    private sealed class TrackingTestDbContext(DbContextOptions<TrackingTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public static TrackingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<TrackingTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TrackingTestDbContext(options);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgramContent>(entity =>
            {
                entity.HasKey(content => content.Id);
                entity.Ignore(content => content.Program);
                entity.Ignore(content => content.Parent);
                entity.Ignore(content => content.Children);
                entity.Ignore(content => content.ContentInteractions);
            });
            modelBuilder.Entity<ContentInteraction>(entity =>
            {
                entity.HasKey(interaction => interaction.Id);
                entity.Ignore(interaction => interaction.User);
                entity.Ignore(interaction => interaction.ProgramUser);
                entity.Ignore(interaction => interaction.ActivityGrades);
                entity.HasOne(interaction => interaction.Content)
                    .WithMany()
                    .HasForeignKey(interaction => interaction.ContentId);
            });
            modelBuilder.Entity<ContentInteractionEvent>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.HasOne(item => item.Interaction)
                    .WithMany(interaction => interaction.Events)
                    .HasForeignKey(item => item.InteractionId);
            });
        }
    }
}
