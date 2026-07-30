using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLearningEvidencePublicationTests : IDisposable
{
    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly Guid _testerId = Guid.NewGuid();

    public TestingLearningEvidencePublicationTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-learning-evidence-{Guid.NewGuid():N}")
            .Options);
        AddActor(_managerId, TenantRole.Owner);
        AddActor(_testerId, TenantRole.Member);
        SetActor(_testerId);
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Complete_WhenLinkedRequirementsAreSatisfied_ShouldPublishLearningEvidence()
    {
        var courseId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var (testingEvent, slot, registration) = AddAttendedRegistration();
        testingEvent.ConfigureLearning(
            courseId,
            null,
            activityId,
            TestingLearningCompletionRequirement.Attendance);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompleteTestingEventParticipationCommand(registration.Id),
            default);

        result.IsSuccess.Should().BeTrue();
        _publisher.Verify(candidate => candidate.Publish(
                It.Is<TestingLearningEvidenceCompletedEvent>(notification =>
                    notification.EvidenceId == registration.Id &&
                    notification.TestingEventId == testingEvent.Id &&
                    notification.SlotId == slot.Id &&
                    notification.UserId == _testerId &&
                    notification.CourseId == courseId &&
                    notification.LearningActivityId == activityId &&
                    notification.Requirement == TestingLearningCompletionRequirement.Attendance),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Complete_WhenLearningRequirementsAreNotSatisfied_ShouldCompleteWithoutPublishingEvidence()
    {
        var (testingEvent, _, registration) = AddAttendedRegistration();
        testingEvent.ConfigureLearning(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            TestingLearningCompletionRequirement.ProjectPresented);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompleteTestingEventParticipationCommand(registration.Id),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TestingSlotRegistrationStatus.Completed);
        _publisher.Verify(candidate => candidate.Publish(
                It.IsAny<TestingLearningEvidenceCompletedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Complete_WhenRetried_ShouldRemainSuccessfulAndRepublishIdempotentEvidence()
    {
        var (testingEvent, _, registration) = AddAttendedRegistration();
        testingEvent.ConfigureLearning(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            TestingLearningCompletionRequirement.Attendance);
        await _context.SaveChangesAsync();
        var handler = CreateHandler();

        var first = await handler.Handle(
            new CompleteTestingEventParticipationCommand(registration.Id),
            default);
        var retry = await handler.Handle(
            new CompleteTestingEventParticipationCommand(registration.Id),
            default);

        first.IsSuccess.Should().BeTrue();
        retry.IsSuccess.Should().BeTrue();
        _publisher.Verify(candidate => candidate.Publish(
                It.Is<TestingLearningEvidenceCompletedEvent>(notification =>
                    notification.EvidenceId == registration.Id),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private TestingParticipationHandlers CreateHandler() => new(
        _context,
        _actorAccessor,
        NullLogger<TestingParticipationHandlers>.Instance,
        null,
        _publisher.Object);

    private (TestingEvent Event, TestingEventSlot Slot, TestingSlotRegistration Registration)
        AddAttendedRegistration()
    {
        var testingEvent = TestingEvent.Create(
            "Learning-linked test",
            TestingEventMode.InPerson,
            _managerId,
            SystemClock.UtcNow.AddDays(-4),
            SystemClock.UtcNow.AddDays(-2),
            SystemClock.UtcNow.AddDays(2),
            SystemClock.UtcNow.AddDays(3),
            false,
            TestingEventApprovalMode.ManagerOnly,
            _tenantId);
        testingEvent.OpenApplications();
        testingEvent.CloseApplications();
        var slot = TestingEventSlot.Create(
            testingEvent.Id,
            TestingEventMode.InPerson,
            testingEvent.StartsAt,
            testingEvent.StartsAt.AddHours(2),
            10,
            4,
            "Main campus",
            "Lab 201",
            null,
            _tenantId);
        var registration = TestingSlotRegistration.Register(
            testingEvent.Id,
            slot.Id,
            _testerId,
            null,
            _tenantId);
        registration.CheckIn();
        registration.CheckOut();
        _context.AddRange(testingEvent, slot, registration);
        return (testingEvent, slot, registration);
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
        public DbSet<TestingSlotRegistration> TestingSlotRegistrations => Set<TestingSlotRegistration>();
        public DbSet<TestingFeedbackObligation> TestingFeedbackObligations => Set<TestingFeedbackObligation>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
