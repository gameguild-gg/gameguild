using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GatewayStatus = GameGuild.Commerce.Payments.PaymentStatus;
using GatewayPaymentMethodType = GameGuild.Commerce.Payments.PaymentMethodType;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class SubscriptionChangeSagaPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailedActivationOrOutboxRollsBackLocalStateAndRetryUsesOneProviderCharge(
        bool ambientCommand, bool failAtCompletionEvent)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var owner = Guid.NewGuid();
        var tenant = new Tenant
        {
            Name = "Saga " + marker,
            Slug = "saga-" + marker,
            AdminEmail = marker + "@example.test"
        };
        var oldPlan = new SubscriptionPlan("Old " + marker, "old-" + marker, 10000);
        var newPlan = new SubscriptionPlan("New " + marker, "new-" + marker, 20000);
        db.AddRange(tenant, oldPlan, newPlan);
        await db.SaveChangesAsync();
        var effective = DateTime.UtcNow;
        var subscription = new Subscription(tenant.Id, oldPlan.Id, owner, BillingCycle.Monthly,
            new Money(100m, "USD"), effective.AddDays(-10));
        subscription.Activate();
        var method = new PaymentMethod(tenant.Id, owner, "stripe", "pm_saga_test", GatewayPaymentMethodType.Card, true);
        subscription.SetPaymentMethod(method.Id);
        db.Add(subscription);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var subscriptionAggregateId = subscription.Id.ToString();
        var failedEventIds = new List<Guid>();
        async Task CaptureUncommittedEvents(CancellationToken cancellationToken)
        {
            failedEventIds.AddRange(await db.Set<OutboxMessage>()
                .Where(value => value.TenantId == tenant.Id && value.AggregateId == subscriptionAggregateId)
                .Select(value => value.EventId).ToListAsync(cancellationToken));
            Assert.NotEmpty(failedEventIds);
        }

        var fail = true;
        var repository = new SubscriptionRepository(db);
        var subscriptions = new Mock<ISubscriptionRepository>();
        subscriptions.Setup(value => value.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken ct) => repository.GetByIdAsync(id, ct));
        subscriptions.Setup(value => value.UpdateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
            .Returns(async (Subscription value, CancellationToken ct) =>
            {
                var updated = await repository.UpdateAsync(value, ct);
                // Throw after the real UPDATE succeeded, before transaction commit.
                if (fail && !failAtCompletionEvent)
                {
                    await CaptureUncommittedEvents(ct);
                    throw new DbUpdateException("synthetic post-write failure");
                }
                return updated;
            });
        var plans = new Mock<ISubscriptionPlanService>();
        plans.Setup(value => value.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newPlan);
        var pricing = new Mock<ISubscriptionIntervalPricingService>();
        pricing.Setup(value => value.ResolvePriceForCycleAsync(It.IsAny<SubscriptionPlan>(), It.IsAny<BillingCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(200m, "USD"));
        var calculator = new Mock<IBillingCalculator>();
        calculator.Setup(value => value.CalculateProration(It.IsAny<Money>(), It.IsAny<Money>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new PlanChangeProration(100m, 200m, 100m, effective));
        var methods = new Mock<IPaymentMethodRepository>();
        methods.Setup(value => value.GetByIdAsync(method.Id, It.IsAny<CancellationToken>())).ReturnsAsync(method);
        var charges = new Dictionary<string, GatewayPaymentRequest>();
        var paymentAttempts = new List<GatewayPaymentRequest>();
        var gateway = new Mock<IPaymentGateway>();
        var receipt = new GatewayPaymentResult(true, "txn_" + marker, "ch_" + marker, null, null,
            GatewayStatus.Succeeded, effective, ProviderId: "stripe", SettledAmount: 100m, SettledCurrency: "USD");
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GatewayPaymentRequest request, CancellationToken _) =>
            {
                paymentAttempts.Add(request);
                if (charges.TryGetValue(request.IdempotencyKey, out var original))
                {
                    // Model a provider that returns its original receipt on replay,
                    // even when the catalog price changed after local rollback.
                    Assert.Equal(original.Amount, receipt.SettledAmount);
                    Assert.Equal(original.Currency, request.Currency);
                    Assert.Equal(original.PaymentMethodId, request.PaymentMethodId);
                    Assert.Equal(original.Metadata, request.Metadata);
                }
                else charges.Add(request.IdempotencyKey, request);
                return receipt;
            });
        gateway.Setup(value => value.GetPaymentAsync(receipt.TransactionId!, It.IsAny<CancellationToken>(), "stripe"))
            .ReturnsAsync(receipt);
        var events = new Mock<IBillingIntegrationEventPublisher>();
        var operationAccessor = scope.ServiceProvider.GetRequiredService<IUseCaseOperationContextAccessor>();
        var durableEvents = new BillingIntegrationEventPublisher(new DurableEventProducer(db), operationAccessor,
            NullLogger<BillingIntegrationEventPublisher>.Instance);
        events.Setup(value => value.PublishSubscriptionChangeStepAsync(It.IsAny<SubscriptionChangeSaga>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((SubscriptionChangeSaga saga, string step, string outcome, string detail, CancellationToken ct) =>
                durableEvents.PublishSubscriptionChangeStepAsync(saga, step, outcome, detail, ct));
        events.Setup(value => value.PublishSubscriptionChangeCompletedAsync(It.IsAny<SubscriptionChangeSaga>(), It.IsAny<CancellationToken>()))
            .Returns(async (SubscriptionChangeSaga saga, CancellationToken ct) =>
            {
                await durableEvents.PublishSubscriptionChangeCompletedAsync(saga, ct);
                if (fail && failAtCompletionEvent)
                {
                    await CaptureUncommittedEvents(ct);
                    throw new DbUpdateException("synthetic completion outbox failure");
                }
            });
        var orchestrator = new SubscriptionChangeSagaOrchestrator(db, subscriptions.Object, plans.Object,
            pricing.Object, calculator.Object, gateway.Object, methods.Object, events.Object,
            NullLogger<SubscriptionChangeSagaOrchestrator>.Instance,
            new DataProtectionGatewayPaymentMethodIdProtector(new EphemeralDataProtectionProvider()));
        var request = new SubscriptionChangeSagaRequest(subscription.Id, newPlan.Id, effective, null, owner);
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var behavior = new UseCaseOperationBehavior<OrchestrateSubscriptionPlanChangeCommand, SubscriptionChangeSagaResult>(
            db, actor.Object, operationAccessor);
        Task<SubscriptionChangeSagaResult> Execute() => ambientCommand
            ? behavior.Handle(new OrchestrateSubscriptionPlanChangeCommand(subscription.Id, newPlan.Id, effective),
                () => orchestrator.ExecuteAsync(request), CancellationToken.None)
            : orchestrator.ExecuteAsync(request);

        await Assert.ThrowsAsync<DbUpdateException>(Execute);
        Assert.Single(charges);
        using (var verifyScope = fixture.Factory.Services.CreateScope())
        {
            var verify = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await verify.Set<Subscription>().AsNoTracking().SingleAsync(value => value.Id == subscription.Id);
            Assert.Equal(oldPlan.Id, stored.PlanId);
            Assert.Equal(100m, stored.Amount.Amount);
            Assert.False(await verify.Set<SubscriptionChangeSaga>().AnyAsync(value => value.SubscriptionId == subscription.Id));
            Assert.False(await verify.Set<Invoice>().AnyAsync(value => value.SubscriptionId == subscription.Id));
            // Check the events actually inserted by this attempt. Other tenants' completed
            // messages can be pruned concurrently by the hosted outbox cleanup service.
            Assert.NotEmpty(failedEventIds);
            Assert.False(await verify.Set<OutboxMessage>().AnyAsync(value => failedEventIds.Contains(value.EventId)));
            Assert.False(await verify.Set<OutboxDeliveryReceipt>().AnyAsync(value => failedEventIds.Contains(value.EventId)));
        }
        gateway.Verify(value => value.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        fail = false;
        pricing.Setup(value => value.ResolvePriceForCycleAsync(newPlan, BillingCycle.Monthly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(250m, "USD"));
        calculator.Setup(value => value.CalculateProration(It.IsAny<Money>(), It.IsAny<Money>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new PlanChangeProration(100m, 250m, 150m, effective));
        await Assert.ThrowsAsync<InvalidOperationException>(Execute);
        Assert.Single(charges);
        Assert.Equal(150m, paymentAttempts[^1].Amount);
        Assert.Equal(oldPlan.Id, (await db.Set<Subscription>().AsNoTracking().SingleAsync(value => value.Id == subscription.Id)).PlanId);
        Assert.False(await db.Set<Invoice>().AnyAsync(value => value.SubscriptionId == subscription.Id));
        gateway.Verify(value => value.ProcessRefundAsync(It.IsAny<GatewayRefundRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        pricing.Setup(value => value.ResolvePriceForCycleAsync(newPlan, BillingCycle.Monthly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Money(200m, "USD"));
        calculator.Setup(value => value.CalculateProration(It.IsAny<Money>(), It.IsAny<Money>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new PlanChangeProration(100m, 200m, 100m, effective));
        var completed = await Execute();
        Assert.True(completed.Succeeded);
        Assert.Single(charges);
        Assert.Equal(100m, paymentAttempts[^1].Amount);
        db.ChangeTracker.Clear();
        Assert.Equal(newPlan.Id, (await db.Set<Subscription>().SingleAsync(value => value.Id == subscription.Id)).PlanId);
        Assert.Single(await db.Set<SubscriptionChangeSaga>().Where(value => value.SubscriptionId == subscription.Id).ToListAsync());
        Assert.Single(await db.Set<Invoice>().Where(value => value.SubscriptionId == subscription.Id).ToListAsync());
    }
}
