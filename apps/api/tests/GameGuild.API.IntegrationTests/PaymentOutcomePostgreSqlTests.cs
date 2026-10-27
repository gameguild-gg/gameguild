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
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UnknownChargeCommitsItsProcessingClaimAndReplayNeverChargesAgain(bool retry, bool inconsistentProviderStatus)
    {
        var marker = Guid.NewGuid().ToString("N");
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
                    subscriptionId: subscriptionId, maxRetries: 4);
                failed.MarkAsProcessing();
                failed.MarkAsFailed("insufficient funds", "insufficient_funds");
                db.Add(failed);
                await db.SaveChangesAsync();
                paymentId = failed.Id;
            }
        }

        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(value => value.ProviderId).Returns("stripe");
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(false, null, null, "response_unconfirmed", "Response lost after send",
                inconsistentProviderStatus ? GatewayStatus.Failed : GatewayStatus.Processing, DateTime.UtcNow,
                ProviderId: "stripe", FailureKind: GatewayFailureKind.OutcomeUnknown));
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
                        Assert.Equal(GatewayStatus.Processing, response.PaymentResult!.Status);
                    }
                }
                else
                {
                    var handler = new ProcessPaymentCommandHandler(repository, gateway.Object,
                        Mock.Of<IPaymentSubscriptionSyncService>(), contextService.Object, options,
                        new PaymentRetryPolicyResolver(options), fraud.Object, NullLogger<ProcessPaymentCommandHandler>.Instance);
                    var command = new ProcessPaymentCommand(tenantId, subscriptionId, 25m, "pm_payment_outcome");
                    var behavior = new UseCaseOperationBehavior<ProcessPaymentCommand, PaymentResult>(db, actor.Object, operation);
                    var response = await behavior.Handle(command, () =>
                    {
                        Assert.NotNull(db.Database.CurrentTransaction);
                        return handler.Handle(command, CancellationToken.None);
                    }, CancellationToken.None);
                    Assert.False(response.Success);
                    Assert.Equal(GatewayStatus.Processing, response.Status);
                    paymentId = Guid.Parse(response.PaymentId!);
                    if (attempt == 0) Assert.False(CommandOutcome.ShouldRollback(response));
                }
            }

            // A new scope/connection observes committed storage, not the handler's tracked object.
            using var verificationScope = fixture.Factory.Services.CreateScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stored = await verification.Set<Payment>().AsNoTracking().SingleAsync(value => value.Id == paymentId);
            Assert.Equal(GatewayStatus.Processing, stored.Status);
            Assert.Null(stored.NextRetryAt);
            Assert.False(stored.CanRetry);
            Assert.Equal(retry ? 1 : 0, stored.RetryCount);
            var attempts = await verification.Set<PaymentRetryAttempt>().AsNoTracking().Where(value => value.PaymentId == paymentId).ToListAsync();
            Assert.Equal(retry ? 1 : 0, attempts.Count);
            if (retry) Assert.Equal(GatewayStatus.Processing, Assert.Single(attempts).Outcome);
        }

        gateway.Verify(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        dunning.VerifyNoOtherCalls();
    }
}
