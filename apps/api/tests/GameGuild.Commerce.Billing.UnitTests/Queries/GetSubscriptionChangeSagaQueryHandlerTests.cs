using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Queries;

public sealed class GetSubscriptionChangeSagaQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly List<SubscriptionChangeSaga> _sagas = [];

    public GetSubscriptionChangeSagaQueryHandlerTests()
    {
        _context
            .Setup(context => context.Set<SubscriptionChangeSaga>())
            .Returns(_sagas.AsQueryable().BuildMockDbSet().Object);
    }

    private static SubscriptionChangeSaga NewSaga(
        Guid subscriptionId, Guid sagaId, DateTime startedAt, string idempotencyKey, Guid? tenantId = null)
    {
        var saga = new SubscriptionChangeSaga(
            tenantId ?? Guid.NewGuid(), subscriptionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), idempotencyKey);
        typeof(SubscriptionChangeSaga).GetProperty("Id")!.SetValue(saga, sagaId);
        saga.Start(SubscriptionChangeSagaOrchestrator.ProrationStep, startedAt);
        return saga;
    }

    private static Mock<IActorContextAccessor> ActorAccessor(Guid? tenantId, bool systemAdmin = false)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(systemAdmin ? ["SystemAdmin"] : []),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });
        return accessor;
    }

    private GetSubscriptionChangeSagaQueryHandler CreateHandler(Guid? tenantId = null, bool systemAdmin = true)
        => new(_context.Object, ActorAccessor(tenantId, systemAdmin).Object);

    [Fact]
    public async Task Handle_ReturnsNull_WhenSubscriptionNeverChangedPlans()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSubscriptionChangeSagaQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsLatestSaga_WithJournaledStepsInOrder()
    {
        var subscriptionId = Guid.NewGuid();
        var olderId = Guid.NewGuid();
        var latestId = Guid.NewGuid();
        var earlier = NewSaga(subscriptionId, olderId, DateTime.UtcNow.AddDays(-2), "key-older");
        var latest = NewSaga(subscriptionId, latestId, DateTime.UtcNow.AddDays(-1), "key-latest");
        latest.AppendStep(new SubscriptionChangeSagaStep(
            latestId, 1, SubscriptionChangeSagaOrchestrator.ProrationStep, "Completed", "credit 10.00", DateTime.UtcNow.AddMinutes(-1)));
        latest.RecordInvoice(Guid.NewGuid());
        latest.AppendStep(new SubscriptionChangeSagaStep(
            latestId, 2, SubscriptionChangeSagaOrchestrator.InvoiceStep, "Completed", "invoice issued", DateTime.UtcNow));
        _sagas.AddRange(earlier, latest);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSubscriptionChangeSagaQuery(subscriptionId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(latestId);
        result.SubscriptionId.Should().Be(subscriptionId);
        result.IdempotencyKey.Should().Be("key-latest");
        result.InvoiceId.Should().NotBeNull();
        result.Steps.Should().HaveCount(2).And.BeInAscendingOrder(step => step.Sequence);
        result.Steps.Should().ContainSingle(step => step.Step == SubscriptionChangeSagaOrchestrator.ProrationStep);
        result.Steps.Should().ContainSingle(step => step.Step == SubscriptionChangeSagaOrchestrator.InvoiceStep);
    }

    [Fact]
    public async Task Handle_ReturnsFailureAndCompensationRecord()
    {
        var subscriptionId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();
        var saga = NewSaga(subscriptionId, sagaId, DateTime.UtcNow, "key-failed");
        saga.BeginCompensation(SubscriptionChangeSagaOrchestrator.PaymentStep, "gateway declined", DateTime.UtcNow);
        saga.Fail("Payment:compensated");
        _sagas.Add(saga);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSubscriptionChangeSagaQuery(subscriptionId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.FailedStep.Should().Be(SubscriptionChangeSagaOrchestrator.PaymentStep);
        result.FailureReason.Should().Be("gateway declined");
        result.CompensationsApplied.Should().Be("Payment:compensated");
        result.Status.Should().Be(SubscriptionChangeSagaStatus.Failed.ToString());
        // CompletedAtUtc is the TERMINAL timestamp (completed or failed): BeginCompensation
        // stamps it when the saga leaves the in-flight states, per the entity contract.
        result.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_KeepsSagasOfOtherSubscriptionsSeparate()
    {
        var ownSaga = NewSaga(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "own");
        var foreignSaga = NewSaga(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(1), "foreign");
        _sagas.AddRange(ownSaga, foreignSaga);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetSubscriptionChangeSagaQuery(ownSaga.SubscriptionId), CancellationToken.None);

        result!.Id.Should().Be(ownSaga.Id);
    }

    /// <summary>
    ///     Issue #1281 R-B6: the plan-change saga surface is tenant-scoped — another
    ///     tenant's saga (proration amounts, payment references, failure reasons) is not
    ///     found, and a tenant-less actor sees nothing at all.
    /// </summary>
    [Fact]
    public async Task Handle_ForTenantActor_ReturnsNullForForeignTenantSaga()
    {
        var saga = NewSaga(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "foreign-key");
        _sagas.Add(saga);
        var handler = CreateHandler(tenantId: Guid.NewGuid(), systemAdmin: false);

        var result = await handler.Handle(new GetSubscriptionChangeSagaQuery(saga.SubscriptionId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ForTenantActor_ReturnsOwnTenantSaga()
    {
        var tenantId = Guid.NewGuid();
        var saga = NewSaga(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "own-key", tenantId);
        _sagas.Add(saga);
        var handler = CreateHandler(tenantId: tenantId, systemAdmin: false);

        var result = await handler.Handle(new GetSubscriptionChangeSagaQuery(saga.SubscriptionId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdempotencyKey.Should().Be("own-key");
    }

    [Fact]
    public async Task Handle_ForTenantlessActor_ReturnsNullFailClosed()
    {
        var saga = NewSaga(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "any");
        _sagas.Add(saga);
        var handler = CreateHandler(tenantId: null, systemAdmin: false);

        var result = await handler.Handle(new GetSubscriptionChangeSagaQuery(saga.SubscriptionId), CancellationToken.None);

        result.Should().BeNull();
    }
}
