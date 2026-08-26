using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using GameGuild.TestSupport.Economy;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class PostgreSqlComplianceHoldStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ActivationReleaseAndScopeMatchingAreDurableAndIdempotent()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("compliance_holds");
        await using var context = CreateContext(database.ConnectionString);
        var store = new PostgreSqlComplianceHoldStore(context);
        var tenant = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var activation = Activation(tenant, actor);

        var created = await store.ActivateAsync(activation, CancellationToken.None);
        var replay = await store.ActivateAsync(activation, CancellationToken.None);

        created.Should().Be(replay);
        created.Scope.Key.Should().Be($"{tenant:N}:subject-hash:all");
        created.IsActive(Now).Should().BeTrue();
        created.IsActive(Now.AddHours(-2)).Should().BeFalse();
        created.IsActive(Now.AddHours(3)).Should().BeFalse();
        (await store.IsActiveAsync(
            new ComplianceHoldScope(tenant, "subject-hash", EconomyValueMovementCapability.PayoutExecution),
            Now, CancellationToken.None)).Should().BeTrue("a global subject hold covers every capability");
        (await store.IsActiveAsync(
            new ComplianceHoldScope(Guid.NewGuid(), "subject-hash", EconomyValueMovementCapability.PayoutExecution),
            Now, CancellationToken.None)).Should().BeFalse();

        var released = await store.ReleaseAsync(created.Id, actor, " release-evidence ", Now.AddMinutes(1), CancellationToken.None);
        var releaseReplay = await store.ReleaseAsync(created.Id, actor, "release-evidence", Now.AddMinutes(2), CancellationToken.None);
        released.Should().Be(releaseReplay);
        released.IsActive(Now.AddMinutes(1)).Should().BeFalse();
        (await store.IsActiveAsync(activation.Scope, Now.AddMinutes(1), CancellationToken.None)).Should().BeFalse();
        (await context.Set<EconomyComplianceHoldEventRow>().CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ConflictingReplaysOverlappingHoldsAndInvalidReleasesFailClosed()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("compliance_hold_conflicts");
        await using var context = CreateContext(database.ConnectionString);
        var store = new PostgreSqlComplianceHoldStore(context);
        var activation = Activation(Guid.NewGuid(), Guid.NewGuid());
        await store.ActivateAsync(activation, CancellationToken.None);

        await FluentActions.Awaiting(() => store.ActivateAsync(
                activation with { ReasonCode = "different" }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*different inputs*");
        await FluentActions.Awaiting(() => store.ActivateAsync(
                activation with { Id = Guid.NewGuid(), IdempotencyKey = "other-key" }, CancellationToken.None).AsTask())
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
        await FluentActions.Awaiting(() => store.ReleaseAsync(
                Guid.NewGuid(), activation.ActorId, "evidence", Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<KeyNotFoundException>();
        await FluentActions.Awaiting(() => store.ReleaseAsync(
                activation.Id, activation.ActorId, "evidence", Now.AddHours(-2), CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>().WithParameterName("releasedAt");

        await store.ReleaseAsync(activation.Id, activation.ActorId, "evidence", Now, CancellationToken.None);
        await FluentActions.Awaiting(() => store.ReleaseAsync(
                activation.Id, Guid.NewGuid(), "evidence", Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*different inputs*");
        await FluentActions.Awaiting(() => store.ReleaseAsync(
                activation.Id, activation.ActorId, "different", Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*different inputs*");
    }

    [Fact]
    public async Task CapabilityScopedHoldMatchesOnlyItsCapability()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("compliance_capability_hold");
        await using var context = CreateContext(database.ConnectionString);
        var store = new PostgreSqlComplianceHoldStore(context);
        var tenant = Guid.NewGuid();
        var scope = new ComplianceHoldScope(tenant, "subject", EconomyValueMovementCapability.PayoutExecution);
        await store.ActivateAsync(
            Activation(tenant, Guid.NewGuid()) with { Scope = scope }, CancellationToken.None);

        scope.Key.Should().EndWith($":{(int)EconomyValueMovementCapability.PayoutExecution}");
        (await store.IsActiveAsync(scope, Now, CancellationToken.None)).Should().BeTrue();
        (await store.IsActiveAsync(
            scope with { Capability = EconomyValueMovementCapability.MarketplaceSettlement },
            Now, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task BoundaryValidationAndConstructionRejectInvalidInputs()
    {
        Action nullContext = () => new PostgreSqlComplianceHoldStore(null!);
        Action nonRelational = () => new PostgreSqlComplianceHoldStore(new StubApplicationDbContext());
        nullContext.Should().Throw<ArgumentNullException>();
        nonRelational.Should().Throw<InvalidOperationException>().WithMessage("*relational DbContext*");

        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("compliance_hold_validation");
        await using var context = CreateContext(database.ConnectionString);
        var store = new PostgreSqlComplianceHoldStore(context);
        var valid = Activation(Guid.NewGuid(), Guid.NewGuid());
        var invalidCapability = (EconomyValueMovementCapability)int.MaxValue;
        Func<ComplianceHoldActivation, Task> activate = value =>
            store.ActivateAsync(value, CancellationToken.None).AsTask();

        await FluentActions.Awaiting(() => activate(null!)).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => activate(valid with { Id = Guid.Empty })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { Scope = null! })).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => activate(valid with { Scope = valid.Scope with { TenantId = Guid.Empty } })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { Scope = valid.Scope with { SubjectHash = " " } })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { Scope = valid.Scope with { Capability = invalidCapability } })).Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Awaiting(() => activate(valid with { CaseReferenceHash = " " })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { ReasonCode = " " })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { EvidenceHash = " " })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { IdempotencyKey = " " })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { ActorId = Guid.Empty })).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => activate(valid with { ExpiresAt = valid.ActivatedAt })).Should().ThrowAsync<ArgumentException>();

        await FluentActions.Awaiting(() => store.ReleaseAsync(Guid.Empty, Guid.NewGuid(), "e", Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>().WithParameterName("holdId");
        await FluentActions.Awaiting(() => store.ReleaseAsync(Guid.NewGuid(), Guid.Empty, "e", Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>().WithParameterName("actorId");
        await FluentActions.Awaiting(() => store.ReleaseAsync(Guid.NewGuid(), Guid.NewGuid(), " ", Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.IsActiveAsync(null!, Now, CancellationToken.None).AsTask())
            .Should().ThrowAsync<ArgumentNullException>();
    }

    private static ComplianceHoldActivation Activation(Guid tenantId, Guid actorId) => new(
        Guid.NewGuid(),
        new ComplianceHoldScope(tenantId, "subject-hash", null),
        " case-hash ",
        " sanctions ",
        " evidence ",
        " idempotency ",
        actorId,
        Now.AddHours(-1),
        Now.AddHours(2));

    private static ApplicationDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options);

    private sealed class StubApplicationDbContext : IApplicationDbContext
    {
        public DbSet<T> Set<T>() where T : class => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
