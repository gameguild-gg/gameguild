using FluentAssertions;
using GameGuild.Learning.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

/// <summary>
/// Cohort capacity gate (issue #1226 N17): <see cref="CohortEnrollmentGuard"/> enforces
/// <see cref="Cohort.CanEnroll"/> before an enrollment may store a CohortId, and keeps
/// <c>CurrentEnrollmentCount</c> in step via staged increment/decrement on the shared
/// context (persisted by the Enrollments module's save, atomically with the row).
/// </summary>
public class CohortEnrollmentGuardTests
{
    [Fact]
    public async Task Reserve_FailsWhenCohortDoesNotExist()
    {
        await using var context = CreateContext();
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReserveCapacityAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Reserve_FailsWhenCohortIsClosed()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Closed", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10);
        await using var context = CreateContext(cohort, out var cohortId);
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReserveCapacityAsync(cohortId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Description.Should().Be("Cohort is not open for enrollment");
    }

    [Fact]
    public async Task Reserve_FailsWhenCohortIsNotActive()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Scheduled", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10);
        cohort.Open();
        cohort.Cancel();
        await using var context = CreateContext(cohort, out var cohortId);
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReserveCapacityAsync(cohortId);

        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Be("Cohort is not open for enrollment");
    }

    [Fact]
    public async Task Reserve_FailsWhenCohortIsAtCapacity()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Full", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), maxCapacity: 1);
        cohort.Open();
        cohort.IncrementEnrollment();
        await using var context = CreateContext(cohort, out var cohortId);
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReserveCapacityAsync(cohortId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Description.Should().Be("Cohort is at capacity");
        cohort.CurrentEnrollmentCount.Should().Be(1);
    }

    [Fact]
    public async Task Reserve_StagesTheSeatIncrementForTheCallersSave()
    {
        // The reservation must be staged on the shared context (not saved here) so the
        // Enrollments module's save persists it in the same transaction as the row.
        var cohort = Cohort.Create(Guid.NewGuid(), "Open", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 2);
        cohort.Open();
        await using var context = CreateContext(cohort, out var cohortId);
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReserveCapacityAsync(cohortId);

        result.IsSuccess.Should().BeTrue();
        cohort.CurrentEnrollmentCount.Should().Be(1);
        var tracked = context.ChangeTracker.Entries<Cohort>().Single();
        tracked.State.Should().Be(EntityState.Modified, "the increment is staged, not saved");
    }

    [Fact]
    public async Task Reserve_AtExactlyCapacityTheSecondReservationRefuses()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "One seat", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1);
        cohort.Open();
        await using var context = CreateContext(cohort, out var cohortId);
        var guard = new CohortEnrollmentGuard(context);

        var first = await guard.ReserveCapacityAsync(cohortId);
        var second = await guard.ReserveCapacityAsync(cohortId);

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Description.Should().Be("Cohort is at capacity");
        cohort.CurrentEnrollmentCount.Should().Be(1);
    }

    [Fact]
    public async Task Release_StagesTheSeatDecrement()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), "Open", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 2);
        cohort.Open();
        cohort.IncrementEnrollment();
        await using var context = CreateContext(cohort, out var cohortId);
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReleaseCapacityAsync(cohortId);

        result.IsSuccess.Should().BeTrue();
        cohort.CurrentEnrollmentCount.Should().Be(0);
    }

    [Fact]
    public async Task Release_FailsWhenCohortDoesNotExist()
    {
        await using var context = CreateContext();
        var guard = new CohortEnrollmentGuard(context);

        var result = await guard.ReleaseCapacityAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Guard_IsRegisteredAsTheCohortEnrollmentGuardPort()
    {
        // Wiring pin: the Cohorts module supplies the ICohortEnrollmentGuard
        // implementation consumed by the Enrollments module.
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddCohortsModule();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICohortEnrollmentGuard>().Should().BeOfType<CohortEnrollmentGuard>();
    }

    private static GuardTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GuardTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new GuardTestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static GuardTestDbContext CreateContext(Cohort cohort, out Guid cohortId)
    {
        var context = CreateContext();
        context.Set<Cohort>().Add(cohort);
        context.SaveChanges();
        cohortId = cohort.Id;
        return context;
    }

    private sealed class GuardTestDbContext(DbContextOptions<GuardTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cohort>(cohort => cohort.HasKey(entity => entity.Id));
        }
    }
}
