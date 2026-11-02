using FluentAssertions;
using GameGuild.Commerce.Payments;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Commerce.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Commands;

/// <summary>
///     Issue #1205 G-1/G-2: the manual invoice-payment retry dispatches the real payment
///     retry (the same RetryPaymentCommand the automated queue uses), enforces invoice
///     tenant + user ownership, and ages an overdue open invoice to PastDue on the retry
///     touch instead of acknowledging a scheduling that never existed.
/// </summary>
public class RetryInvoicePaymentHandlerTests
{
    private sealed class TestRetryDbContext(DbContextOptions<TestRetryDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>();
            modelBuilder.Entity<Subscription>().Ignore(subscription => subscription.Plan);
            modelBuilder.Entity<Subscription>().OwnsOne(subscription => subscription.Amount);
            base.OnModelCreating(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Mock.Of<IDbContextTransaction>());
    }

    private static async Task<TestRetryDbContext> CreateContextAsync()
    {
        var context = new TestRetryDbContext(
            new DbContextOptionsBuilder<TestRetryDbContext>()
                .UseInMemoryDatabase($"invoice-retry-{Guid.NewGuid():N}")
                .Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Mock<IActorContextAccessor> ActorAccessor(Guid tenantId, Guid userId)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });
        return accessor;
    }

    private static async Task<(TestRetryDbContext Context, RetryInvoicePaymentHandler Handler, Mock<ISender> Sender, Guid TenantId, Guid OwnerId)> ArrangeAsync()
    {
        var context = await CreateContextAsync();
        var tenantId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var subscription = new Subscription(
            tenantId: tenantId,
            planId: Guid.NewGuid(),
            createdByUserId: ownerId,
            billingCycle: BillingCycle.Monthly,
            amount: new GameGuild.Money(29.99m, "USD"),
            startDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Set<Subscription>().Add(subscription);
        await context.SaveChangesAsync();

        var sender = new Mock<ISender>();
        var handler = new RetryInvoicePaymentHandler(
            context,
            sender.Object,
            ActorAccessor(tenantId, ownerId).Object);

        return (context, handler, sender, tenantId, ownerId);
    }

    private static Invoice CreatePastDueInvoice(Guid tenantId, Guid subscriptionId, Guid paymentId)
    {
        var invoice = new Invoice(tenantId, subscriptionId, 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        invoice.Issue(SystemClock.UtcNow.AddDays(-10)); // due date already elapsed
        invoice.RecordPayment(paymentId, 10m, SystemClock.UtcNow.AddDays(-9)); // partial: stays unpaid, binds the payment
        invoice.MarkPastDue();
        return invoice;
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenInvoiceIsOutsideTheActorsTenant()
    {
        var (context, handler, sender, tenantId, ownerId) = await ArrangeAsync();
        var foreignInvoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        context.Set<Invoice>().Add(foreignInvoice);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new RetryInvoicePaymentCommand(foreignInvoice.Id), CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.Code.Should().Be("NotFound");
        sender.Verify(senderMock => senderMock.Send(It.IsAny<IRequest<PaymentRetryResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRejectForeignOwner_EvenWithinTheSameTenant()
    {
        var (context, handler, sender, tenantId, ownerId) = await ArrangeAsync();
        var strangerSubscription = new Subscription(
            tenantId: tenantId,
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(), // different owner
            billingCycle: BillingCycle.Monthly,
            amount: new GameGuild.Money(29.99m, "USD"),
            startDate: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Set<Subscription>().Add(strangerSubscription);
        var invoice = new Invoice(tenantId, strangerSubscription.Id, 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        invoice.Issue();
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new RetryInvoicePaymentCommand(invoice.Id), CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.Code.Should().Be("NotOwned");
        sender.Verify(senderMock => senderMock.Send(It.IsAny<IRequest<PaymentRetryResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDispatchTheRealPaymentRetry_ForTheBoundPayment()
    {
        var (context, handler, sender, tenantId, ownerId) = await ArrangeAsync();
        var paymentId = Guid.NewGuid();
        var subscriptionId = (await context.Set<Subscription>().FirstAsync()).Id;
        var invoice = CreatePastDueInvoice(tenantId, subscriptionId, paymentId);
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();

        RetryPaymentCommand? dispatched = null;
        sender
            .Setup(senderMock => senderMock.Send(It.IsAny<RetryPaymentCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PaymentRetryResult>, CancellationToken>((command, _) => dispatched = (RetryPaymentCommand)command)
            .ReturnsAsync(new PaymentRetryResult { Success = true, RetryAttempt = 2 });

        var result = await handler.Handle(new RetryInvoicePaymentCommand(invoice.Id), CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.Code.Should().Be("RetryAccepted");
        result.RetryScheduledAt.Should().NotBeNull();
        dispatched.Should().NotBeNull();
        dispatched!.PaymentId.Should().Be(paymentId, "the retry must target the invoice's bound payment");
    }

    [Fact]
    public async Task Handle_ShouldSurfaceRetryFailure_WhenThePaymentRetryFails()
    {
        var (context, handler, sender, tenantId, ownerId) = await ArrangeAsync();
        var paymentId = Guid.NewGuid();
        var subscriptionId = (await context.Set<Subscription>().FirstAsync()).Id;
        var invoice = CreatePastDueInvoice(tenantId, subscriptionId, paymentId);
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();

        sender
            .Setup(senderMock => senderMock.Send(It.IsAny<RetryPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRetryResult { Success = false, FailureReason = "insufficient funds" });

        var result = await handler.Handle(new RetryInvoicePaymentCommand(invoice.Id), CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.Code.Should().Be("RetryFailed");
        result.Message.Should().Contain("insufficient funds");
    }

    [Fact]
    public async Task Handle_ShouldAgeOverdueOpenInvoiceToPastDue_OnTheRetryTouch()
    {
        // Issue #1205 G-2: the retry arriving after the due date is the retry-window
        // expiry signal — the open invoice transitions to PastDue before the retry runs.
        var (context, handler, sender, tenantId, ownerId) = await ArrangeAsync();
        var paymentId = Guid.NewGuid();
        var subscriptionId = (await context.Set<Subscription>().FirstAsync()).Id;
        var invoice = new Invoice(tenantId, subscriptionId, 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        invoice.Issue(SystemClock.UtcNow.AddDays(-10));
        invoice.RecordPayment(paymentId, 5m, SystemClock.UtcNow.AddDays(-9)); // partial, still Open
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();
        invoice.Status.Should().Be(InvoiceStatus.Open);

        sender
            .Setup(senderMock => senderMock.Send(It.IsAny<RetryPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentRetryResult { Success = true, RetryAttempt = 1 });

        var result = await handler.Handle(new RetryInvoicePaymentCommand(invoice.Id), CancellationToken.None);

        result.Accepted.Should().BeTrue();
        result.InvoiceStatus.Should().Be(InvoiceStatus.PastDue, "the overdue open invoice aged on the retry touch");
    }

    [Fact]
    public async Task Handle_ShouldRejectInvoice_WithoutPaymentBinding()
    {
        var (context, handler, sender, tenantId, ownerId) = await ArrangeAsync();
        var subscriptionId = (await context.Set<Subscription>().FirstAsync()).Id;
        var invoice = new Invoice(tenantId, subscriptionId, 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        invoice.Issue();
        context.Set<Invoice>().Add(invoice);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new RetryInvoicePaymentCommand(invoice.Id), CancellationToken.None);

        result.Accepted.Should().BeFalse();
        result.Code.Should().Be("NoPaymentBinding");
        sender.Verify(senderMock => senderMock.Send(It.IsAny<IRequest<PaymentRetryResult>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
