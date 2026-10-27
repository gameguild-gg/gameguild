using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using GatewayStatus = GameGuild.Commerce.Payments.PaymentStatus;
using PaymentResult = GameGuild.Commerce.Payments.PaymentResult;
using PaymentRetryResult = GameGuild.Commerce.Payments.PaymentRetryResult;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PaymentOutcomePostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false, false, "stripe")]
    [InlineData(false, true, "stripe")]
    [InlineData(true, false, "stripe")]
    [InlineData(true, true, "stripe")]
    [InlineData(false, false, "paypal")]
    [InlineData(true, false, "paypal")]
    public async Task UnknownChargeCommitsItsClaimAndReplayNeverChargesAgain(bool retry, bool inconsistentProviderStatus, string provider)
    {
        var marker = Guid.NewGuid().ToString("N");
        var expectedStatus = provider == "stripe" ? GatewayStatus.Pending : GatewayStatus.Processing;
        Guid tenantId;
        Guid subscriptionId;
        Guid? paymentId = null;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenant = new Tenant { Name = "Payment outcome " + marker, Slug = "payment-outcome-" + marker, AdminEmail = marker + "@example.test" };
            var plan = new SubscriptionPlan("Payment outcome " + marker, "payment-outcome-" + marker, 2500);
            db.AddRange(tenant, plan);
            await db.SaveChangesAsync();
            var subscription = new Subscription(tenant.Id, plan.Id, Guid.NewGuid(), BillingCycle.Monthly,
                new Money(25m, "USD"), DateTime.UtcNow.AddDays(-10));
            subscription.Activate();
            db.Add(subscription);
            await db.SaveChangesAsync();
            tenantId = tenant.Id;
            subscriptionId = subscription.Id;
            if (retry)
            {
                var failed = Payment.Create(tenantId, 25m, "USD", "payment-outcome-" + marker,
                    provider: provider, subscriptionId: subscriptionId,
                    paymentMethodId: provider == "stripe" ? "pm_payment_outcome" : "ba_payment_outcome", maxRetries: 4);
                failed.MarkAsProcessing();
                failed.MarkAsFailed("insufficient funds", "insufficient_funds");
                db.Add(failed);
                await db.SaveChangesAsync();
                paymentId = failed.Id;
            }
        }

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(value => value.ProviderId).Returns(provider);
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(false, null, null, "response_unconfirmed", "Response lost after send",
                inconsistentProviderStatus ? GatewayStatus.Failed : GatewayStatus.Processing, DateTime.UtcNow,
                ProviderId: provider, FailureKind: GatewayFailureKind.OutcomeUnknown));
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var options = Options.Create(new PaymentRetryOptions { MaxRetries = 4, JitterRatio = 0 });
        var fraud = new Mock<IFraudDetectionService>();
        fraud.Setup(value => value.AssessPaymentAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudSignalContext signal, CancellationToken _) => new FraudAssessment(
                signal.TenantId, signal.PaymentId, 0, FraudAssessment.RiskLevelForScore(0), FraudDecision.Allow,
                "controlled provider outcome test", [], DateTime.UtcNow));
        var contextService = new Mock<ISubscriptionPaymentContextService>();
        contextService.Setup(value => value.GetPaymentContextAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPaymentContext(subscriptionId, tenantId, 25m, "USD", "cus_payment_outcome"));
        var dunning = new Mock<IPaymentDunningLifecycleService>();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var repository = new PaymentRepository(db, NullLogger<PaymentRepository>.Instance);
                var operation = scope.ServiceProvider.GetRequiredService<IUseCaseOperationContextAccessor>();
                if (retry)
                {
                    var handler = new RetryPaymentCommandHandler(repository, gateway.Object,
                        Mock.Of<IPaymentSubscriptionSyncService>(), options, new PaymentRetryPolicyResolver(options),
                        dunning.Object, scope.ServiceProvider.GetRequiredService<IRevenueAuditService>(), actor.Object,
                        NullLogger<RetryPaymentCommandHandler>.Instance);
                    var command = new RetryPaymentCommand(paymentId!.Value);
                    var behavior = new UseCaseOperationBehavior<RetryPaymentCommand, PaymentRetryResult>(db, actor.Object, operation);
                    var response = await behavior.Handle(command, () =>
                    {
                        Assert.NotNull(db.Database.CurrentTransaction);
                        return handler.Handle(command, CancellationToken.None);
                    }, CancellationToken.None);
                    Assert.False(response.Success);
                    if (attempt == 0)
                    {
                        Assert.True(CommandOutcome.IsFailure(response));
                        Assert.False(CommandOutcome.ShouldRollback(response));
                        Assert.Equal(expectedStatus, response.PaymentResult!.Status);
                    }
                }
                else
                {
                    var handler = new ProcessPaymentCommandHandler(repository, gateway.Object,
                        Mock.Of<IPaymentSubscriptionSyncService>(), contextService.Object, options,
                        new PaymentRetryPolicyResolver(options), fraud.Object, NullLogger<ProcessPaymentCommandHandler>.Instance);
                    var token = provider == "stripe" ? "pm_payment_outcome" : "ba_payment_outcome";
                    var command = new ProcessPaymentCommand(tenantId, subscriptionId, 25m, token, GatewayProvider: provider);
                    var behavior = new UseCaseOperationBehavior<ProcessPaymentCommand, PaymentResult>(db, actor.Object, operation);
                    var response = await behavior.Handle(command, () =>
                    {
                        Assert.NotNull(db.Database.CurrentTransaction);
                        return handler.Handle(command, CancellationToken.None);
                    }, CancellationToken.None);
                    Assert.False(response.Success);
                    Assert.Equal(expectedStatus, response.Status);
                    paymentId = Guid.Parse(response.PaymentId!);
                    if (attempt == 0) Assert.False(CommandOutcome.ShouldRollback(response));
                }
            }

            // A new scope/connection observes committed storage, not the handler's tracked object.
            using var verificationScope = fixture.Factory.Services.CreateScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await verification.Set<Payment>().AsNoTracking().SingleAsync(value => value.Id == paymentId);
            Assert.Equal(expectedStatus, stored.Status);
            Assert.Equal(provider, stored.Provider);
            Assert.Null(stored.ProviderObjectId);
            Assert.Null(stored.ExternalPaymentId);
            Assert.Null(stored.ExternalTransactionId);
            Assert.Null(stored.NextRetryAt);
            Assert.False(stored.CanRetry);
            Assert.Equal(retry ? 1 : 0, stored.RetryCount);
            var attempts = await verification.Set<PaymentRetryAttempt>().AsNoTracking().Where(value => value.PaymentId == paymentId).ToListAsync();
            Assert.Equal(retry ? 1 : 0, attempts.Count);
            if (retry) Assert.Equal(expectedStatus, Assert.Single(attempts).Outcome);
        }

        gateway.Verify(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        dunning.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthoritativeStripeMappingPermitsConfirmedOrActionRequiredState(bool requiresAction)
    {
        var marker = Guid.NewGuid().ToString("N");
        Guid tenantId;
        Guid subscriptionId;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenant = new Tenant { Name = "Mapped payment " + marker, Slug = "mapped-payment-" + marker, AdminEmail = marker + "@example.test" };
            var plan = new SubscriptionPlan("Mapped payment " + marker, "mapped-payment-" + marker, 2500);
            db.AddRange(tenant, plan);
            await db.SaveChangesAsync();
            var subscription = new Subscription(tenant.Id, plan.Id, Guid.NewGuid(), BillingCycle.Monthly,
                new Money(25m, "USD"), DateTime.UtcNow.AddDays(-10));
            subscription.Activate();
            db.Add(subscription);
            await db.SaveChangesAsync();
            tenantId = tenant.Id;
            subscriptionId = subscription.Id;
        }
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(value => value.ProviderId).Returns("stripe");
        var intentId = "pi_mapped_" + marker;
        var mapping = new GatewayProviderMapping("test", "acct_payment_outcome", intentId, "payment_intent", "capture");
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(!requiresAction, intentId, requiresAction ? null : "ch_" + marker,
                null, null, requiresAction ? GatewayStatus.RequiresAction : GatewayStatus.Succeeded, DateTime.UtcNow,
                ProviderId: "stripe", ProviderMapping: mapping));
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var contextService = new Mock<ISubscriptionPaymentContextService>();
        contextService.Setup(value => value.GetPaymentContextAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPaymentContext(subscriptionId, tenantId, 25m, "USD", "cus_payment_outcome"));
        var fraud = new Mock<IFraudDetectionService>();
        fraud.Setup(value => value.AssessPaymentAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FraudSignalContext signal, CancellationToken _) => new FraudAssessment(
                signal.TenantId, signal.PaymentId, 0, FraudAssessment.RiskLevelForScore(0), FraudDecision.Allow,
                "controlled provider outcome test", [], DateTime.UtcNow));
        var options = Options.Create(new PaymentRetryOptions());
        Guid paymentId;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var repository = new PaymentRepository(db, NullLogger<PaymentRepository>.Instance);
            var handler = new ProcessPaymentCommandHandler(repository, gateway.Object,
                Mock.Of<IPaymentSubscriptionSyncService>(), contextService.Object, options,
                new PaymentRetryPolicyResolver(options), fraud.Object, NullLogger<ProcessPaymentCommandHandler>.Instance);
            var command = new ProcessPaymentCommand(tenantId, subscriptionId, 25m, "pm_mapped_outcome");
            var behavior = new UseCaseOperationBehavior<ProcessPaymentCommand, PaymentResult>(db, actor.Object,
                scope.ServiceProvider.GetRequiredService<IUseCaseOperationContextAccessor>());
            var response = await behavior.Handle(command, () => handler.Handle(command, CancellationToken.None), CancellationToken.None);
            Assert.Equal(!requiresAction, response.Success);
            Assert.Equal(requiresAction ? GatewayStatus.RequiresAction : GatewayStatus.Succeeded, response.Status);
            Assert.False(CommandOutcome.ShouldRollback(response));
            paymentId = Guid.Parse(response.PaymentId!);
        }
        using var verificationScope = fixture.Factory.Services.CreateScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await verification.Set<Payment>().AsNoTracking().SingleAsync(value => value.Id == paymentId);
        Assert.Equal(requiresAction ? GatewayStatus.RequiresAction : GatewayStatus.Succeeded, stored.Status);
        Assert.Equal(mapping.ProviderEnvironment, stored.ProviderEnvironment);
        Assert.Equal(mapping.ProviderAccountId, stored.ProviderAccountId);
        Assert.Equal(mapping.ProviderObjectId, stored.ProviderObjectId);
        Assert.Equal(mapping.ProviderObjectType, stored.ProviderObjectType);
        Assert.Equal(mapping.ProviderMonetaryLeg, stored.ProviderMonetaryLeg);
        gateway.Verify(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
