using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce;
using GameGuild.Commerce.Payments;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PaymentOutcomePostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false, "paypal")]
    [InlineData(true, "paypal")]
    [InlineData(false, "stripe")]
    [InlineData(true, "stripe")]
    public async Task UnknownChargeSurvivesCommandTransactionAndAnotherRequestDoesNotChargeAgain(bool retry, string provider)
    {
        var marker = Guid.NewGuid().ToString("N");
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Payment outcome " + marker,
            Slug = "payment-outcome-" + marker, AdminEmail = "outcome-" + marker + "@example.test", IsActive = true };
        var admin = User.Create("payment-admin-" + marker + "@example.test", "Synthetic payment administrator");
        admin.Username = "payment-admin-" + marker;
        var subscription = new SubscriptionPaymentContext(Guid.NewGuid(), tenant.Id, 25m, "USD", "synthetic-customer");
        var key = "payment-outcome-" + marker;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        var operation = new UseCaseOperationContextAccessor();
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(AuthenticatedAdmin(admin.Id, tenant.Id));
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(value => value.ProviderId).Returns(provider);
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GatewayPaymentRequest, CancellationToken>((request, _) =>
            {
                if (!retry) { key = request.IdempotencyKey; }
            })
            .ReturnsAsync(new GatewayPaymentResult(false, null, null, "response_unconfirmed", "Synthetic lost response",
                PaymentStatus.Processing, SystemClock.UtcNow, ProviderId: provider, FailureKind: GatewayFailureKind.OutcomeUnknown));
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
        PaymentStatus claimStatus;
        await using (var seed = Context())
        {
            seed.Set<Tenant>().Add(tenant);
            seed.Set<User>().Add(admin);
            if (retry)
            {
                var payment = Payment.Create(subscription.TenantId, subscription.Amount, subscription.Currency, key,
                    provider: provider, subscriptionId: subscription.SubscriptionId);
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
            Assert.Contains(payment.Status, new[] { PaymentStatus.Pending, PaymentStatus.Processing });
            claimStatus = payment.Status;
            Assert.Equal(provider, payment.Provider);
            Assert.Null(payment.ProviderObjectId);
            Assert.Null(payment.ExternalPaymentId);
            Assert.Null(payment.ExternalTransactionId);
            Assert.Null(payment.NextRetryAt);
            Assert.False(payment.CanRetry);
        }
        await using (var second = Context())
        {
            await ExecuteAsync(second);
        }
        await using (var verify = Context())
        {
            Assert.Equal(claimStatus,
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifiedStripeMappingAllowsConfirmedOrCustomerActionOutcome(bool requiresAction)
    {
        var marker = Guid.NewGuid().ToString("N");
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Mapped payment " + marker,
            Slug = "mapped-payment-" + marker, AdminEmail = "mapped-" + marker + "@example.test", IsActive = true };
        var admin = User.Create("mapped-admin-" + marker + "@example.test", "Synthetic payment administrator");
        admin.Username = "mapped-admin-" + marker;
        var subscription = new SubscriptionPaymentContext(Guid.NewGuid(), tenant.Id, 25m, "USD", "synthetic-customer");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        var operation = new UseCaseOperationContextAccessor();
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(AuthenticatedAdmin(admin.Id, tenant.Id));
        var gateway = new Mock<IPaymentGateway>();
        gateway.SetupGet(value => value.ProviderId).Returns("stripe");
        var intentId = "pi_mapped_" + marker;
        var mapping = new GatewayProviderMapping("test", "acct_payment_outcome", intentId, "payment_intent", "capture");
        gateway.Setup(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPaymentResult(!requiresAction, intentId, requiresAction ? null : "ch_" + marker,
                null, null, requiresAction ? PaymentStatus.RequiresAction : PaymentStatus.Succeeded, SystemClock.UtcNow,
                ProviderId: "stripe", ProviderMapping: mapping));
        var contextService = new Mock<ISubscriptionPaymentContextService>();
        contextService.Setup(value => value.GetPaymentContextAsync(subscription.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        var fraud = new Mock<IFraudDetectionService>();
        fraud.Setup(value => value.AssessPaymentAsync(It.IsAny<FraudSignalContext>(), It.IsAny<CancellationToken>()))
            .Returns((FraudSignalContext signal, CancellationToken _) => Task.FromResult(new FraudAssessment(
                signal.TenantId, signal.PaymentId, 0, FraudAssessment.RiskLevelForScore(0), FraudDecision.Allow,
                "Synthetic mapped outcome", [], SystemClock.UtcNow)));
        var sync = new Mock<IPaymentSubscriptionSyncService>();
        sync.Setup(value => value.SyncSuccessfulPaymentAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var retryOptions = Options.Create(new PaymentRetryOptions());
        Guid paymentId;
        await using (var seed = new ApplicationDbContext(options))
        {
            seed.AddRange(tenant, admin);
            await seed.SaveChangesAsync();
        }
        await using (var db = new ApplicationDbContext(options, publisher: null, useCaseOperationContextAccessor: operation))
        {
            var repository = new PaymentRepository(db, NullLogger<PaymentRepository>.Instance);
            var handler = new ProcessPaymentCommandHandler(repository, gateway.Object, sync.Object, contextService.Object,
                retryOptions, new PaymentRetryPolicyResolver(retryOptions), fraud.Object, NullLogger<ProcessPaymentCommandHandler>.Instance);
            var request = new ProcessPaymentCommand(subscription.TenantId, subscription.SubscriptionId, subscription.Amount, "synthetic-method");
            var behavior = new UseCaseOperationBehavior<ProcessPaymentCommand, PaymentResult>(db, actor.Object, operation);
            var result = await behavior.Handle(request, () => handler.Handle(request, CancellationToken.None), CancellationToken.None);
            Assert.Equal(!requiresAction, result.Success);
            Assert.Equal(requiresAction ? PaymentStatus.RequiresAction : PaymentStatus.Succeeded, result.Status);
            Assert.False(CommandOutcome.ShouldRollback(result));
            paymentId = Guid.Parse(result.PaymentId!);
        }
        await using var verify = new ApplicationDbContext(options);
        var stored = await verify.Set<Payment>().AsNoTracking().SingleAsync(value => value.Id == paymentId);
        Assert.Equal(requiresAction ? PaymentStatus.RequiresAction : PaymentStatus.Succeeded, stored.Status);
        Assert.Equal(mapping.ProviderEnvironment, stored.ProviderEnvironment);
        Assert.Equal(mapping.ProviderAccountId, stored.ProviderAccountId);
        Assert.Equal(mapping.ProviderObjectId, stored.ProviderObjectId);
        Assert.Equal(mapping.ProviderObjectType, stored.ProviderObjectType);
        Assert.Equal(mapping.ProviderMonetaryLeg, stored.ProviderMonetaryLeg);
        gateway.Verify(value => value.ProcessPaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmedStripePaymentWithoutProviderProofIsRejectedByTheDatabase()
    {
        var marker = Guid.NewGuid().ToString("N");
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Unverified payment " + marker,
            Slug = "unverified-payment-" + marker, AdminEmail = "unverified-" + marker + "@example.test", IsActive = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        db.Set<Tenant>().Add(tenant);
        await db.SaveChangesAsync();
        var payment = Payment.Create(tenant.Id, 25m, "USD", "unverified-" + marker, provider: "stripe");
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("synthetic-unverified-payment", "synthetic-unverified-transaction");
        db.Set<Payment>().Add(payment);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal("ck_payments_stripe_value_mapping_required", postgres.ConstraintName);
    }

    private static ActorContext AuthenticatedAdmin(Guid userId, Guid tenantId) => new()
    {
        ActorKind = ActorKind.User, SubjectId = userId.ToString("D"), TenantId = tenantId,
        Roles = new HashSet<string> { "SystemAdmin" }, Permissions = new HashSet<string>(),
        TypedAttributes = ActorAttributes.Empty, AuthScheme = "SyntheticPaymentTest", IsAuthenticated = true
    };
}
