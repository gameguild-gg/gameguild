using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace GameGuild.GameJams.UnitTests;

public sealed class GameJamsCoverageCompletionTests
{
    [Fact]
    public async Task Repository_Should_Persist_Query_And_Order_All_GameJam_Data()
    {
        await using var context = CreateContext();
        var repository = new GameJamRepository(context);
        var upcoming = CreateJam("Upcoming", JamStatus.Upcoming, SystemClock.UtcNow.AddDays(7));
        var active = CreateJam("Active", JamStatus.Active, SystemClock.UtcNow.AddDays(-1));

        await repository.AddJamAsync(upcoming);
        await repository.AddJamAsync(active);
        active.ParticipantCount = 2;
        await repository.UpdateJamAsync(active);

        (await repository.GetJamAsync(active.Id)).Should().BeEquivalentTo(active);
        (await repository.ListJamsAsync(null, -10, 0)).Should().ContainSingle().Which.Name.Should().Be("Upcoming");
        (await repository.ListJamsAsync(JamStatus.Active, 0, 100)).Should().ContainSingle().Which.Id.Should().Be(active.Id);

        var submission = new JamSubmission { Id = Guid.NewGuid(), JamId = active.Id, ProjectVersionId = Guid.NewGuid(), UserId = Guid.NewGuid(), SubmissionNotes = "ship" };
        await repository.AddSubmissionAsync(submission);
        (await repository.GetSubmissionAsync(submission.Id)).Should().BeEquivalentTo(submission);
        (await repository.GetSubmissionsAsync(active.Id)).Should().ContainSingle().Which.SubmissionNotes.Should().Be("ship");

        var criteria = new JamJudgingCriteria { Id = Guid.NewGuid(), JamId = active.Id, Name = "Fun", Weight = 2m, MaxScore = 10 };
        await repository.AddCriteriaAsync(criteria);
        (await repository.GetCriteriaAsync(active.Id)).Should().ContainSingle().Which.Name.Should().Be("Fun");

        var score = new JamScore { Id = Guid.NewGuid(), SubmissionId = submission.Id, CriteriaId = criteria.Id, JudgeUserId = Guid.NewGuid(), Score = 9, Feedback = "great" };
        await repository.AddScoreAsync(score);
        context.Set<JamScore>().Should().ContainSingle(item => item.Feedback == "great");
    }

    [Fact]
    public async Task Service_Should_Cover_Success_And_Error_Branches()
    {
        await using var context = CreateContext();
        var service = new GameJamService(new GameJamRepository(context));
        var creatorId = Guid.NewGuid();

        var future = await service.CreateAsync(new CreateJamCommand(new CreateJamRequest(
            " Future Jam ", " future-jam ", SystemClock.UtcNow.AddDays(1), SystemClock.UtcNow.AddDays(3), creatorId,
            Theme: "Theme", Description: "Desc", Rules: "Rules", SubmissionCriteria: "Criteria", VotingEndDate: SystemClock.UtcNow.AddDays(4), MaxParticipants: 1)));
        future.Name.Should().Be("Future Jam");
        future.Slug.Should().Be("future-jam");
        future.Status.Should().Be(JamStatus.Upcoming);

        var active = await service.CreateAsync(new CreateJamCommand(new CreateJamRequest(
            "Active", "active", SystemClock.UtcNow.AddDays(-1), SystemClock.UtcNow.AddDays(1), creatorId)));
        active.Status.Should().Be(JamStatus.Active);
        (await service.GetAsync(active.Id)).Should().NotBeNull();
        (await service.GetAsync(Guid.NewGuid())).Should().BeNull();
        (await service.ListAsync(new GetJamsQuery(Take: 25))).Should().HaveCount(2);
        (await service.SetStatusAsync(active.Id, JamStatus.Voting))!.Status.Should().Be(JamStatus.Voting);
        (await service.SetStatusAsync(Guid.NewGuid(), JamStatus.Completed)).Should().BeNull();

        var fallbackCriteria = await service.AddCriteriaAsync(new AddJamCriteriaCommand(active.Id, " Score ", null, 0m, 0));
        fallbackCriteria.Name.Should().Be("Score");
        fallbackCriteria.Weight.Should().Be(1m);
        fallbackCriteria.MaxScore.Should().Be(1);
        (await service.GetCriteriaAsync(active.Id)).Should().ContainSingle();

        var submission = await service.SubmitAsync(new SubmitJamEntryCommand(active.Id, Guid.NewGuid(), Guid.NewGuid(), "notes"));
        submission.Notes().Should().Be("notes");
        (await service.GetSubmissionsAsync(active.Id)).Should().ContainSingle();

        await service.Invoking(s => s.SubmitAsync(new SubmitJamEntryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Jam * was not found.");

        var futureEntity = await context.Set<Jam>().FindAsync(future.Id);
        futureEntity!.ParticipantCount = futureEntity.MaxParticipants!.Value;
        await context.SaveChangesAsync();

        await service.Invoking(s => s.SubmitAsync(new SubmitJamEntryCommand(future.Id, Guid.NewGuid(), Guid.NewGuid(), null)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("The jam has reached its participant limit.");

        var score = await service.ScoreAsync(new ScoreJamSubmissionCommand(submission.Id, fallbackCriteria.Id, Guid.NewGuid(), 100, "high"));
        score.Score.Should().Be(1);

        await service.Invoking(s => s.ScoreAsync(new ScoreJamSubmissionCommand(Guid.NewGuid(), fallbackCriteria.Id, Guid.NewGuid(), 1, null)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Submission * was not found.");
        await service.Invoking(s => s.ScoreAsync(new ScoreJamSubmissionCommand(submission.Id, Guid.NewGuid(), Guid.NewGuid(), 1, null)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Criteria * was not found.");
    }

    [Fact]
    public async Task Handlers_Controllers_Di_And_ModelConfiguration_Should_Cover_Public_Module_Surface()
    {
        var jam = new JamDto(Guid.NewGuid(), "Jam", "jam", null, null, SystemClock.UtcNow, SystemClock.UtcNow.AddDays(1), null, null, 0, JamStatus.Active, Guid.NewGuid());
        var submission = new JamSubmissionDto(Guid.NewGuid(), jam.Id, Guid.NewGuid(), Guid.NewGuid(), "notes");
        var criteria = new JamCriteriaDto(Guid.NewGuid(), jam.Id, "Fun", null, 1m, 5);
        var score = new JamScoreDto(Guid.NewGuid(), submission.Id, criteria.Id, Guid.NewGuid(), 4, "ok");
        var service = new Mock<IGameJamService>();
        service.Setup(s => s.CreateAsync(It.IsAny<CreateJamCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(jam);
        service.Setup(s => s.GetAsync(jam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(jam);
        service.Setup(s => s.GetAsync(Guid.Empty, It.IsAny<CancellationToken>())).ReturnsAsync((JamDto?)null);
        service.Setup(s => s.ListAsync(It.IsAny<GetJamsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([jam]);
        service.Setup(s => s.SetStatusAsync(jam.Id, JamStatus.Completed, It.IsAny<CancellationToken>())).ReturnsAsync(jam);
        service.Setup(s => s.SetStatusAsync(Guid.Empty, JamStatus.Completed, It.IsAny<CancellationToken>())).ReturnsAsync((JamDto?)null);
        service.Setup(s => s.SubmitAsync(It.IsAny<SubmitJamEntryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(submission);
        service.Setup(s => s.GetSubmissionsAsync(jam.Id, It.IsAny<CancellationToken>())).ReturnsAsync([submission]);
        service.Setup(s => s.AddCriteriaAsync(It.IsAny<AddJamCriteriaCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(criteria);
        service.Setup(s => s.GetCriteriaAsync(jam.Id, It.IsAny<CancellationToken>())).ReturnsAsync([criteria]);
        service.Setup(s => s.ScoreAsync(It.IsAny<ScoreJamSubmissionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(score);

        (await new CreateJamCommandHandler(service.Object).Handle(new CreateJamCommand(new CreateJamRequest("Jam", "jam", SystemClock.UtcNow, SystemClock.UtcNow.AddDays(1), Guid.NewGuid())), CancellationToken.None)).Should().Be(jam);
        (await new GetJamQueryHandler(service.Object).Handle(new GetJamQuery(jam.Id), CancellationToken.None)).Should().Be(jam);
        (await new GetJamsQueryHandler(service.Object).Handle(new GetJamsQuery(), CancellationToken.None)).Should().ContainSingle();
        (await new SetJamStatusCommandHandler(service.Object).Handle(new SetJamStatusCommand(jam.Id, JamStatus.Completed), CancellationToken.None)).Should().Be(jam);
        (await new SubmitJamEntryCommandHandler(service.Object).Handle(new SubmitJamEntryCommand(jam.Id, submission.ProjectVersionId, submission.UserId, submission.SubmissionNotes), CancellationToken.None)).Should().Be(submission);
        (await new AddJamCriteriaCommandHandler(service.Object).Handle(new AddJamCriteriaCommand(jam.Id, criteria.Name, criteria.Description, criteria.Weight, criteria.MaxScore), CancellationToken.None)).Should().Be(criteria);
        (await new ScoreJamSubmissionCommandHandler(service.Object).Handle(new ScoreJamSubmissionCommand(submission.Id, criteria.Id, score.JudgeUserId, score.Score, score.Feedback), CancellationToken.None)).Should().Be(score);

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetJamsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([jam]);
        sender.Setup(s => s.Send(It.Is<GetJamQuery>(q => q.JamId == jam.Id), It.IsAny<CancellationToken>())).ReturnsAsync(jam);
        sender.Setup(s => s.Send(It.Is<GetJamQuery>(q => q.JamId == Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync((JamDto?)null);
        sender.Setup(s => s.Send(It.IsAny<CreateJamCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(jam);
        sender.Setup(s => s.Send(It.Is<SetJamStatusCommand>(c => c.JamId == jam.Id), It.IsAny<CancellationToken>())).ReturnsAsync(jam);
        sender.Setup(s => s.Send(It.Is<SetJamStatusCommand>(c => c.JamId == Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync((JamDto?)null);
        sender.Setup(s => s.Send(It.IsAny<SubmitJamEntryCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(submission);
        sender.Setup(s => s.Send(It.IsAny<AddJamCriteriaCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(criteria);
        sender.Setup(s => s.Send(It.IsAny<ScoreJamSubmissionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(score);
        var controller = new GameJamsController(sender.Object, service.Object);

        (await controller.List(null, 0, 0, CancellationToken.None)).Should().ContainSingle();
        (await controller.List(JamStatus.Active, 1, 10, CancellationToken.None)).Should().ContainSingle();
        (await controller.Get(jam.Id, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.Get(Guid.Empty, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Create(new CreateJamRequest("Jam", "jam", SystemClock.UtcNow, SystemClock.UtcNow.AddDays(1), Guid.NewGuid()), CancellationToken.None)).Should().Be(jam);
        (await controller.SetStatus(jam.Id, JamStatus.Completed, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.SetStatus(Guid.Empty, JamStatus.Completed, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.GetSubmissions(jam.Id, CancellationToken.None)).Should().ContainSingle();
        (await controller.Submit(jam.Id, new SubmitJamEntryRequest(submission.ProjectVersionId, submission.UserId, submission.SubmissionNotes), CancellationToken.None)).Should().Be(submission);
        (await controller.GetCriteria(jam.Id, CancellationToken.None)).Should().ContainSingle();
        (await controller.AddCriteria(jam.Id, new AddJamCriteriaRequest(criteria.Name, criteria.Description, criteria.Weight, criteria.MaxScore), CancellationToken.None)).Should().Be(criteria);
        (await controller.Score(submission.Id, new ScoreJamSubmissionRequest(criteria.Id, score.JudgeUserId, score.Score, score.Feedback), CancellationToken.None)).Should().Be(score);

        var modelBuilder = new ModelBuilder();
        new GameJamsModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.FindEntityType(typeof(Jam))!.GetTableName().Should().Be("game_jams");
        modelBuilder.Model.FindEntityType(typeof(JamSubmission))!.GetIndexes().Should().Contain(index => index.IsUnique);
        modelBuilder.Model.FindEntityType(typeof(JamJudgingCriteria))!.FindProperty(nameof(JamJudgingCriteria.Weight))!.GetPrecision().Should().Be(8);
        modelBuilder.Model.FindEntityType(typeof(JamScore))!.GetIndexes().Should().Contain(index => index.IsUnique);

        var services = new ServiceCollection();
        services.AddGameJamsModule();
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IGameJamService));

        var module = new GameJamsModule();
        module.Name.Should().Be("GameJams");
        module.Order.Should().Be(170);
        module.ConfigureServices(new ServiceCollection(), new ConfigurationBuilder().Build()).Should().NotBeNull();
        var endpoints = new Mock<IEndpointRouteBuilder>();
        module.MapEndpoints(endpoints.Object).Should().BeSameAs(endpoints.Object);
    }

    private static TestGameJamsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestGameJamsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestGameJamsDbContext(options);
    }

    private static Jam CreateJam(string name, JamStatus status, DateTime startDate)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant(),
            StartDate = startDate,
            EndDate = startDate.AddDays(2),
            Status = status,
            CreatedBy = Guid.NewGuid()
        };

    private sealed class TestGameJamsDbContext(DbContextOptions<TestGameJamsDbContext> options) : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder) => new GameJamsModelConfiguration().Configure(modelBuilder);
    }
}

file static class JamSubmissionDtoExtensions
{
    public static string? Notes(this JamSubmissionDto dto) => dto.SubmissionNotes;
}
