using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLearningConfigurationTests : IDisposable
{
    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();

    public TestingLearningConfigurationTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-learning-configuration-{Guid.NewGuid():N}")
            .Options);
        AddActor(_managerId, TenantRole.Owner);
        AddActor(_memberId, TenantRole.Member);
        SetActor(_managerId);
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task ConfigureLearning_WhenActorManagesEvent_ShouldPersistLinkage()
    {
        var testingEvent = AddEvent();
        await _context.SaveChangesAsync();
        var command = new ConfigureTestingEventLearningCommand(
            testingEvent.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestingLearningCompletionRequirement.Attendance |
            TestingLearningCompletionRequirement.FeedbackSubmitted);

        var result = await CreateHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CourseId.Should().Be(command.CourseId);
        result.Value.CohortId.Should().Be(command.CohortId);
        result.Value.LearningActivityId.Should().Be(command.LearningActivityId);
        result.Value.LearningCompletionRequirement.Should().Be(command.Requirement);
    }

    [Fact]
    public async Task ConfigureLearning_WhenActorDoesNotManageEvent_ShouldBeForbidden()
    {
        var testingEvent = AddEvent();
        await _context.SaveChangesAsync();
        SetActor(_memberId);

        var result = await CreateHandler().Handle(
            new ConfigureTestingEventLearningCommand(
                testingEvent.Id,
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                TestingLearningCompletionRequirement.Attendance),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Theory]
    [InlineData(TestingLearningCompletionRequirement.None)]
    [InlineData((TestingLearningCompletionRequirement)8)]
    public void ConfigureLearning_WhenRequirementIsEmptyOrUnsupported_ShouldRejectIt(
        TestingLearningCompletionRequirement requirement)
    {
        var testingEvent = AddEvent();

        var act = () => testingEvent.ConfigureLearning(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            requirement);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private TestingEventHandlers CreateHandler() => new(_context, _actorAccessor);

    private TestingEvent AddEvent()
    {
        var testingEvent = TestingEvent.Create(
            "Learning linked event",
            TestingEventMode.Online,
            _managerId,
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(1),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            true,
            TestingEventApprovalMode.ManagerOnly,
            _tenantId);
        _context.Add(testingEvent);
        return testingEvent;
    }

    private void AddActor(Guid userId, string role)
    {
        _context.Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Testing Lab actor",
            IsActive = true
        });
        _context.Add(new TenantMember
        {
            UserId = userId,
            TenantId = _tenantId,
            Role = role,
            IsActive = true
        });
    }

    private void SetActor(Guid userId) => _actorAccessor.SetActorContext(
        ActorContextBuilder.ForUser(userId).WithTenantId(_tenantId).Build());

    private sealed class TestContext(DbContextOptions<TestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
        public DbSet<TestingEvent> TestingEvents => Set<TestingEvent>();
        public DbSet<TestingEventSlot> TestingEventSlots => Set<TestingEventSlot>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
