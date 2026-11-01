using GameGuild.CQRS;
using GameGuild.GameJams;
using GameGuild.Identity.Context.Actors;
using GameGuild.Projects.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Projects.UnitTests.Handlers;

public sealed class JamProjectFactoryTests : IDisposable
{
    private readonly TestProjectsDbContext _context;
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Jam _jam = new()
    {
        Name = "Winter Jam",
        Slug = "winter-jam",
        Theme = "Ice and snow",
        Description = "A chilly game jam.",
        StartDate = SystemClock.UtcNow.AddDays(-1),
        EndDate = SystemClock.UtcNow.AddDays(6),
        Status = JamStatus.Active,
        CreatedBy = Guid.NewGuid()
    };

    public JamProjectFactoryTests()
    {
        var options = new DbContextOptionsBuilder<TestProjectsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TestProjectsDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_Rejects_Anonymous_Callers_Without_Sending_The_Create_Command()
    {
        SetActor(userId: null);
        var sender = new CapturingSender(_ => Result.Success(new Project()));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(_jam.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.Unauthenticated");
        sender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Returns_NotFound_When_The_Jam_Does_Not_Exist()
    {
        SetActor(userId: _userId);
        var sender = new CapturingSender(_ => Result.Success(new Project()));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.JamNotFound");
        sender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Creates_A_Private_Draft_Project_Linked_To_The_Jam()
    {
        await SeedJamAsync();
        SetActor(userId: _userId);
        var sender = new CapturingSender(command =>
            Result.Success(new TestDataBuilder().CreateProject(createdById: _userId, title: command.Title)));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(_jam.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var createCommand = sender.Sent.Should().ContainSingle().Subject;
        createCommand.Title.Should().Be("Winter Jam - jam entry");
        createCommand.ShortDescription.Should().Contain("Winter Jam");
        createCommand.Visibility.Should().Be(ContentVisibility.Private);
        createCommand.Status.Should().Be(ContentStatus.Draft);
        createCommand.Type.Should().Be(GameGuild.ProjectType.Game);
        createCommand.Tags.Should().ContainInOrder("jam", "winter-jam");
        createCommand.CreatedById.Should().Be(_userId);

        var link = await _context.Set<ProjectJamSubmission>().SingleAsync();
        link.ProjectId.Should().Be(result.Value.Id);
        link.JamId.Should().Be(_jam.Id);
        link.SubmissionNotes.Should().Be("Auto-created jam workspace");
        link.IsEligible.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Uses_The_Requested_Title_When_Provided()
    {
        await SeedJamAsync();
        SetActor(userId: _userId);
        var sender = new CapturingSender(command =>
            Result.Success(new TestDataBuilder().CreateProject(createdById: _userId, title: command.Title)));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(_jam.Id, "Frostpunk-like"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sender.Sent.Should().ContainSingle().Which.Title.Should().Be("Frostpunk-like");
    }

    [Fact]
    public async Task Handle_Propagates_The_Create_Failure()
    {
        await SeedJamAsync();
        SetActor(userId: _userId);
        var sender = new CapturingSender(_ =>
            Result.Failure<Project>(Error.Forbidden("Project.TenantMembershipRequired", "An active tenant membership is required")));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(_jam.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.TenantMembershipRequired");
        (await _context.Set<ProjectJamSubmission>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_Returns_The_Existing_Project_When_The_Jam_Link_Already_Exists()
    {
        await SeedJamAsync();
        SetActor(userId: _userId);
        var existing = new TestDataBuilder().CreateProject(createdById: _userId, title: "Winter Jam - jam entry");
        _context.Set<Project>().Add(existing);
        _context.Set<ProjectJamSubmission>().Add(new ProjectJamSubmission { ProjectId = existing.Id, JamId = _jam.Id });
        await _context.SaveChangesAsync();
        var sender = new CapturingSender(_ => Result.Success(new TestDataBuilder().CreateProject(createdById: _userId)));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(_jam.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existing.Id);
        sender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Creates_A_New_Project_For_A_Different_Creator_Of_The_Same_Jam()
    {
        await SeedJamAsync();
        var otherUser = Guid.NewGuid();
        var otherProject = new TestDataBuilder().CreateProject(createdById: otherUser);
        _context.Set<Project>().Add(otherProject);
        _context.Set<ProjectJamSubmission>().Add(new ProjectJamSubmission { ProjectId = otherProject.Id, JamId = _jam.Id });
        await _context.SaveChangesAsync();
        SetActor(userId: _userId);
        var sender = new CapturingSender(command =>
            Result.Success(new TestDataBuilder().CreateProject(createdById: _userId, title: command.Title)));
        var handler = CreateHandler(sender);

        var result = await handler.Handle(new CreateJamProjectCommand(_jam.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sender.Sent.Should().ContainSingle();
        (await _context.Set<ProjectJamSubmission>().CountAsync()).Should().Be(2);
    }

    private void SetActor(Guid? userId) =>
        _actorAccessor.SetupGet(accessor => accessor.ActorContext)
            .Returns(userId is { } id
                ? ActorContextBuilder.ForUser(id).Build()
                : ActorContextBuilder.Create().Build());

    private CreateJamProjectCommandHandler CreateHandler(CapturingSender sender) =>
        new(_context, _actorAccessor.Object, sender, NullLogger<CreateJamProjectCommandHandler>.Instance);

    private async Task SeedJamAsync()
    {
        _context.Set<Jam>().Add(_jam);
        await _context.SaveChangesAsync();
    }

    private sealed class CapturingSender(Func<CreateProjectCommand, Result<Project>> responder) : ISender
    {
        public List<CreateProjectCommand> Sent { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is CreateProjectCommand command)
            {
                Sent.Add(command);
                return Task.FromResult((TResponse)(object)responder(command));
            }

            throw new NotSupportedException($"Unexpected request type {request.GetType().Name}.");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException($"Unexpected request type {request.GetType().Name}.");

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Unexpected request type {request.GetType().Name}.");
    }
}
