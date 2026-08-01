using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabAnalyticsReportTests : IDisposable
{
    private static readonly DateTime PeriodStart = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc);

    private readonly TestContext _context;
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    public TestingLabAnalyticsReportTests()
    {
        _context = new TestContext(new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"testing-analytics-{Guid.NewGuid():N}")
            .Options);
        AddActor(_managerId, _tenantId);
        _context.SaveChanges();
        _actorAccessor.SetActorContext(
            ActorContextBuilder.ForUser(_managerId).WithTenantId(_tenantId).Build());
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Report_IsTenantScopedAndComparesThePreviousEquivalentPeriod()
    {
        await AddCompletedEventAsync(_tenantId, _managerId, "Current campus lab", PeriodStart.AddDays(2), 8, 4, 3, 2, 9);
        await AddCompletedEventAsync(_tenantId, _managerId, "Previous campus lab", PeriodStart.AddDays(-3), 4, 2, 1, 1, 6);

        var otherTenant = Guid.NewGuid();
        var otherManager = AddActor(Guid.NewGuid(), otherTenant);
        await AddCompletedEventAsync(otherTenant, otherManager.Id, "Hidden lab", PeriodStart.AddDays(2), 30, 20, 20, 20, 10);

        var result = await CreateHandler().Handle(
            new GetTestingLabAnalyticsReportQuery(PeriodStart, PeriodEnd, IncludeComparison: true),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Events.Should().Be(1);
        result.Value.Current.Applications.Should().Be(4);
        result.Value.Current.RegisteredTesters.Should().Be(3);
        result.Value.Current.AttendedTesters.Should().Be(2);
        result.Value.Current.Feedback.Should().Be(2);
        result.Value.Current.AverageRating.Should().Be(9);
        result.Value.Current.Capacity.Should().Be(8);
        result.Value.Current.FillRate.Should().Be(37.5m);
        result.Value.Previous.Should().NotBeNull();
        result.Value.Previous!.Events.Should().Be(1);
        result.Value.Previous.Applications.Should().Be(2);
        result.Value.Events.Should().ContainSingle(item => item.Name == "Current campus lab");
        result.Value.Trend.Sum(item => item.Registrations).Should().Be(3);
    }

    [Fact]
    public async Task Report_RejectsAnInvalidPeriod()
    {
        var result = await CreateHandler().Handle(
            new GetTestingLabAnalyticsReportQuery(PeriodEnd, PeriodStart),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TestingLab.InvalidAnalyticsPeriod");
    }

    [Fact]
    public async Task CsvExport_UsesTheTenantReportAndEscapesEventNames()
    {
        await AddCompletedEventAsync(_tenantId, _managerId, "Lab, \"Alpha\"", PeriodStart.AddDays(2), 10, 2, 2, 1, 8);

        var result = await CreateHandler().Handle(
            new ExportTestingLabAnalyticsReportQuery(PeriodStart, PeriodEnd),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.FileName.Should().Be("testing-lab-20260701-20260707.csv");
        var csv = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        csv.Should().Contain("\"Lab, \"\"Alpha\"\"\"");
        csv.Should().Contain("Event,Status,Mode,Starts at,Applications,Approved projects,Registered testers,Attended testers,Feedback,Average rating,Capacity,Fill rate");
    }

    private TestingLabAnalyticsHandlers CreateHandler() => new(
        _context,
        _actorAccessor);

    private async Task AddCompletedEventAsync(
        Guid tenantId,
        Guid managerId,
        string name,
        DateTime startsAt,
        int capacity,
        int applications,
        int registrations,
        int attended,
        int rating)
    {
        var testingEvent = TestingEvent.Create(
            name,
            TestingEventMode.InPerson,
            managerId,
            startsAt.AddDays(-8),
            startsAt.AddDays(-1),
            startsAt,
            startsAt.AddHours(3),
            true,
            TestingEventApprovalMode.ManagerOnly,
            tenantId);
        testingEvent.CreatedAt = startsAt;
        _context.Add(testingEvent);
        var slot = TestingEventSlot.Create(
            testingEvent.Id,
            TestingEventMode.InPerson,
            startsAt,
            startsAt.AddHours(3),
            capacity,
            applications,
            "Main campus",
            "Lab 1",
            null,
            tenantId);
        slot.CreatedAt = startsAt;
        _context.Add(slot);

        for (var index = 0; index < applications; index++)
        {
            var applicant = AddActor(Guid.NewGuid(), tenantId);
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = $"Project {index}",
                Slug = $"project-{Guid.NewGuid():N}",
                Description = "Testing project",
                CreatedById = applicant.Id,
                TenantId = tenantId,
                CreatedAt = startsAt
            };
            _context.Add(project);
            var application = TestingProjectApplication.Submit(
                testingEvent.Id,
                project.Id,
                null,
                applicant.Id,
                null,
                tenantId);
            application.CreatedAt = startsAt;
            _context.Add(application);
        }

        for (var index = 0; index < registrations; index++)
        {
            var tester = AddActor(Guid.NewGuid(), tenantId);
            var registration = TestingSlotRegistration.Register(
                testingEvent.Id,
                slot.Id,
                tester.Id,
                null,
                tenantId);
            registration.CreatedAt = startsAt;
            if (index < attended)
            {
                registration.CheckIn();
                registration.CheckOut();
            }
            _context.Add(registration);

            if (index < attended)
            {
                var feedback = TestingFeedback.CreateForEvent(
                    testingEvent.Id,
                    Guid.NewGuid(),
                    tester.Id,
                    TestingContext.InPerson,
                    "{}",
                    rating,
                    true,
                    null,
                    tenantId);
                feedback.CreatedAt = startsAt;
                _context.Add(feedback);
            }
        }

        await _context.SaveChangesAsync();
    }

    private User AddActor(Guid userId, Guid tenantId)
    {
        var user = new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Testing Lab user",
            IsActive = true
        };
        _context.Add(user);
        _context.Add(new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            Role = "Member",
            IsActive = true
        });
        return user;
    }

    private sealed class TestContext(DbContextOptions<TestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TestingEvent> TestingEvents => Set<TestingEvent>();
        public DbSet<TestingEventSlot> TestingEventSlots => Set<TestingEventSlot>();
        public DbSet<TestingProjectApplication> TestingProjectApplications => Set<TestingProjectApplication>();
        public DbSet<TestingSlotRegistration> TestingSlotRegistrations => Set<TestingSlotRegistration>();
        public DbSet<TestingFeedback> TestingFeedback => Set<TestingFeedback>();
        public DbSet<TestingLocation> TestingLocations => Set<TestingLocation>();

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
