using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public sealed class CohortContentAvailabilityTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task Availability_RequiresEnrollmentAndRelease(
        bool enrolled,
        bool released,
        bool expected)
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(
            enrolled: enrolled,
            availableFrom: released ? SystemClock.UtcNow.AddHours(-1) : SystemClock.UtcNow.AddHours(1));

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Any(content => content.ContentId == fixture.ContentId).Should().Be(expected);
    }

    [Fact]
    public async Task HiddenOverride_ExcludesContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(
            visibilityOverride: CohortVisibilityOverride.Hidden);

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().NotContain(content => content.ContentId == fixture.ContentId);
    }

    [Fact]
    public async Task ExpiredWindow_ExcludesContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(
            availableFrom: SystemClock.UtcNow.AddHours(-2),
            availableUntil: SystemClock.UtcNow.AddHours(-1));

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().NotContain(content => content.ContentId == fixture.ContentId);
    }

    [Fact]
    public async Task DifferentCohort_ExcludesContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(enrollmentCohortId: Guid.NewGuid());

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().NotContain(content => content.ContentId == fixture.ContentId);
    }

    [Fact]
    public async Task DifferentTenant_ExcludesContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(actorTenantId: Guid.NewGuid());

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().NotContain(content => content.ContentId == fixture.ContentId);
    }

    [Fact]
    public async Task DeletedCanonicalContent_ExcludesContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(contentDeleted: true);

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().NotContain(content => content.ContentId == fixture.ContentId);
    }

    [Fact]
    public async Task DifferentActor_ExcludesContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(enrollmentUserId: Guid.NewGuid());

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().NotContain(content => content.ContentId == fixture.ContentId);
    }

    [Fact]
    public async Task VisibleOverride_ReleasesPrivateCanonicalContent()
    {
        await using var fixture = await AvailabilityFixture.CreateAsync(
            contentVisibility: Visibility.Private,
            visibilityOverride: CohortVisibilityOverride.Visible);

        var result = await fixture.Handler.Handle(fixture.Query, CancellationToken.None);

        result.Should().ContainSingle(content => content.ContentId == fixture.ContentId);
    }

    private sealed class AvailabilityFixture : IAsyncDisposable
    {
        private AvailabilityFixture(
            CohortAvailabilityTestDbContext context,
            GetAvailableCohortContentQueryHandler handler,
            GetAvailableCohortContentQuery query,
            Guid contentId)
        {
            Context = context;
            Handler = handler;
            Query = query;
            ContentId = contentId;
        }

        public CohortAvailabilityTestDbContext Context { get; }
        public GetAvailableCohortContentQueryHandler Handler { get; }
        public GetAvailableCohortContentQuery Query { get; }
        public Guid ContentId { get; }

        public static async Task<AvailabilityFixture> CreateAsync(
            bool enrolled = true,
            DateTime? availableFrom = null,
            DateTime? availableUntil = null,
            CohortVisibilityOverride visibilityOverride = CohortVisibilityOverride.Inherited,
            Visibility contentVisibility = Visibility.Internal,
            Guid? enrollmentCohortId = null,
            Guid? actorTenantId = null,
            Guid? enrollmentUserId = null,
            bool contentDeleted = false)
        {
            var options = new DbContextOptionsBuilder<CohortAvailabilityTestDbContext>()
                .UseInMemoryDatabase($"CohortAvailability_{Guid.NewGuid()}")
                .Options;
            var context = new CohortAvailabilityTestDbContext(options);
            var courseId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var actorId = Guid.NewGuid();
            var cohort = Cohort.Create(
                courseId,
                "Evening class",
                SystemClock.UtcNow.AddDays(-7),
                SystemClock.UtcNow.AddDays(90),
                24,
                tenantId);
            var content = new ProgramContent
            {
                ProgramId = courseId,
                TenantId = tenantId,
                Title = "Foundations",
                Type = ProgramContentType.Lesson,
                Visibility = contentVisibility,
                SortOrder = 1,
                DeletedAt = contentDeleted ? SystemClock.UtcNow : null
            };
            var scheduleItem = CohortScheduleItem.Create(
                cohort.Id,
                content.Id,
                null,
                CohortScheduleItemType.ContentRelease,
                content.Title,
                instructionalWeek: 1,
                sortOrder: 1,
                availableFrom: availableFrom ?? SystemClock.UtcNow.AddHours(-1),
                availableUntil: availableUntil,
                status: CohortScheduleItemStatus.Scheduled,
                visibilityOverride: visibilityOverride,
                tenantId: tenantId);

            context.AddRange(cohort, content, scheduleItem);
            if (enrolled)
            {
                var enrollment = Enrollment.Create(
                    courseId,
                    enrollmentUserId ?? actorId,
                    enrollmentCohortId ?? cohort.Id);
                enrollment.TenantId = tenantId;
                context.Add(enrollment);
            }

            await context.SaveChangesAsync();

            var actor = ActorContextBuilder.ForUser(actorId)
                .WithTenantId(actorTenantId ?? tenantId)
                .Build();
            var actorAccessor = new Mock<IActorContextAccessor>();
            actorAccessor.SetupGet(accessor => accessor.ActorContext).Returns(actor);
            var handler = new GetAvailableCohortContentQueryHandler(context, actorAccessor.Object);

            return new AvailabilityFixture(
                context,
                handler,
                new GetAvailableCohortContentQuery(courseId, cohort.Id),
                content.Id);
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}

internal sealed class CohortAvailabilityTestDbContext(DbContextOptions<CohortAvailabilityTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cohort>().HasKey(entity => entity.Id);
        modelBuilder.Entity<CohortScheduleItem>().HasKey(entity => entity.Id);
        modelBuilder.Entity<Enrollment>().HasKey(entity => entity.Id);
        modelBuilder.Entity<ProgramContent>(entity =>
        {
            entity.HasKey(content => content.Id);
            entity.Ignore(content => content.Program);
            entity.Ignore(content => content.Parent);
            entity.Ignore(content => content.Children);
            entity.Ignore(content => content.ContentInteractions);
        });
    }
}
