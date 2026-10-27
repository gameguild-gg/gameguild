using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce;
using GameGuild.Commerce.Payments;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PaymentOutcomePostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownChargeSurvivesCommandTransactionAndAnotherRequestDoesNotChargeAgain(bool retry)
    {
        var marker = Guid.NewGuid().ToString("N");
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Payment outcome " + marker,
            Slug = "payment-outcome-" + marker, AdminEmail = "outcome-" + marker + "@example.test", IsActive = true };
        var subscription = new SubscriptionPaymentContext(Guid.NewGuid(), tenant.Id, 25m, "USD", "synthetic-customer");
        var key = "payment-outcome-" + marker;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        var operation = new UseCaseOperationContextAccessor();
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(value => value.ProviderId).Returns("paypal");
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GatewayPaymentRequest, CancellationToken>((request, _) =>
            {
                if (!retry) { key = request.IdempotencyKey; }
            })
            .ReturnsAsync(new GatewayPaymentResult(false, null, null, "response_unconfirmed", "Synthetic lost response",
                PaymentStatus.Processing, SystemClock.UtcNow, ProviderId: "paypal", FailureKind: GatewayFailureKind.OutcomeUnknown));
        var contextService = new Mock<ISubscriptionPaymentContextService>();
        contextService.Setup(value => value.GetPaymentContextAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        var fraud = new Mock<IFraudDetectionService>();
        fraud.Setup(value => value.AssessPaymentAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .Returns((FraudSignalContext signal, CancellationToken _) => Task.FromResult(new FraudAssessment(
                signal.TenantId, signal.PaymentId, 0, FraudAssessment.RiskLevelForScore(0), FraudDecision.Allow,
                "Synthetic allow assessment", [], SystemClock.UtcNow)));
        var dunning = new Mock<IPaymentDunningLifecycleService>();
        var audit = new Mock<IRevenueAuditService>();
        audit.Setup(value => value.RecordAuditTrailAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var retryOptions = Options.Create(new PaymentRetryOptions());
        Guid paymentId;
        await using (var seed = Context())
        {
            seed.Set<Tenant>().Add(tenant);
            if (retry)
            {
                var payment = Payment.Create(subscription.TenantId, subscription.Amount, subscription.Currency, key,
                    provider: "paypal", subscriptionId: subscription.SubscriptionId);
                payment.MarkAsProcessing();
                payment.MarkAsFailed("Synthetic previous decline", "insufficient_funds");
                seed.Set<Payment>().Add(payment);
            }
            await seed.SaveChangesAsync();
        }

        await using (var first = Context())
        {
            var result = await ExecuteAsync(first);
            Assert.True(CommandOutcome.IsFailure(result));
            Assert.False(CommandOutcome.ShouldRollback(result));
            Assert.Null(operation.Current);
        }
        await using (var verify = Context())
        {
            var payment = await verify.Set<Payment>().AsNoTracking().SingleAsync(value => value.IdempotencyKey == key);
            paymentId = payment.Id;
            Assert.Equal(PaymentStatus.Processing, payment.Status);
            Assert.Null(payment.NextRetryAt);
            Assert.False(payment.CanRetry);
        }
        await using (var second = Context())
        {
            await ExecuteAsync(second);
        }
        await using (var verify = Context())
        {
            Assert.Equal(PaymentStatus.Processing,
                (await verify.Set<Payment>().AsNoTracking().SingleAsync(value => value.Id == paymentId)).Status);
            Assert.Equal(retry ? 1 : 0,
                await verify.Set<PaymentRetryAttempt>().CountAsync(value => value.PaymentId == paymentId));
        }
        gateway.Verify(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        dunning.VerifyNoOtherCalls();

        ApplicationDbContext Context() => new(options, publisher: null, useCaseOperationContextAccessor: operation);

        async Task<object> ExecuteAsync(ApplicationDbContext db)
        {
            var repository = new PaymentRepository(db, NullLogger<PaymentRepository>.Instance);
            var sync = Mock.Of<IPaymentSubscriptionSyncService>();
            var policy = new PaymentRetryPolicyResolver(retryOptions);
            if (retry)
            {
                var payment = await db.Set<Payment>().SingleAsync(value => value.IdempotencyKey == key);
                var request = new RetryPaymentCommand(payment.Id);
                var handler = new RetryPaymentCommandHandler(repository, gateway.Object, sync, retryOptions, policy,
                    dunning.Object, audit.Object, actor.Object, NullLogger<RetryPaymentCommandHandler>.Instance);
                var behavior = new UseCaseOperationBehavior<RetryPaymentCommand, PaymentRetryResult>(db, actor.Object, operation);
                return await behavior.Handle(request, () => handler.Handle(request, CancellationToken.None), CancellationToken.None);
            }
            else
            {
                var request = new ProcessPaymentCommand(subscription.TenantId, subscription.SubscriptionId, subscription.Amount, "synthetic-method");
                var handler = new ProcessPaymentCommandHandler(repository, gateway.Object, sync, contextService.Object,
                    retryOptions, policy, fraud.Object, NullLogger<ProcessPaymentCommandHandler>.Instance);
                var behavior = new UseCaseOperationBehavior<ProcessPaymentCommand, PaymentResult>(db, actor.Object, operation);
                return await behavior.Handle(request, () => handler.Handle(request, CancellationToken.None), CancellationToken.None);
            }
        }
    }
}
