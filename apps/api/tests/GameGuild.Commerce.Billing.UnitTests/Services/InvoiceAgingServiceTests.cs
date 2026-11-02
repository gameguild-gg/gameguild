using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

/// <summary>
///     Issue #1205 G-2: overdue open invoices age to PastDue; everything else is left
///     alone. Backs the on-next-touch sweep wired into materialization and manual retry.
/// </summary>
public sealed class InvoiceAgingServiceTests
{
    private sealed class TestAgingDbContext(DbContextOptions<TestAgingDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>();
            base.OnModelCreating(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Mock.Of<IDbContextTransaction>());
    }

    private static async Task<TestAgingDbContext> CreateContextAsync()
    {
        var context = new TestAgingDbContext(
            new DbContextOptionsBuilder<TestAgingDbContext>()
                .UseInMemoryDatabase($"invoice-aging-{Guid.NewGuid():N}")
                .Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Invoice CreateIssuedInvoice(Guid tenantId, decimal total, DateTime dueDate)
    {
        var invoice = new Invoice(tenantId, Guid.NewGuid(), total, "USD", $"inv-{Guid.NewGuid():N}");
        invoice.Issue(dueDate);
        return invoice;
    }

    [Fact]
    public async Task MarkOverdueOpenInvoicesPastDue_AgesOnlyOverdueOpenInvoices()
    {
        var tenantId = Guid.NewGuid();
        await using var context = await CreateContextAsync();

        var overdue = CreateIssuedInvoice(tenantId, 29.99m, SystemClock.UtcNow.AddDays(-5));
        var notYetDue = CreateIssuedInvoice(tenantId, 29.99m, SystemClock.UtcNow.AddDays(25));
        var alreadyPastDue = CreateIssuedInvoice(tenantId, 29.99m, SystemClock.UtcNow.AddDays(-5));
        alreadyPastDue.MarkPastDue();
        var paid = CreateIssuedInvoice(tenantId, 29.99m, SystemClock.UtcNow.AddDays(-5));
        paid.RecordPayment(Guid.NewGuid(), 29.99m, SystemClock.UtcNow);
        context.Set<Invoice>().AddRange(overdue, notYetDue, alreadyPastDue, paid);
        await context.SaveChangesAsync();

        var service = new InvoiceAgingService(context, NullLogger<InvoiceAgingService>.Instance);

        var aged = await service.MarkOverdueOpenInvoicesPastDueAsync(cancellationToken: CancellationToken.None);

        aged.Should().BeEquivalentTo([overdue.Id]);
        overdue.Status.Should().Be(InvoiceStatus.PastDue);
        notYetDue.Status.Should().Be(InvoiceStatus.Open);
        alreadyPastDue.Status.Should().Be(InvoiceStatus.PastDue);
        paid.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task MarkOverdueOpenInvoicesPastDue_ScopesToTheRequestedSubscription()
    {
        var tenantId = Guid.NewGuid();
        await using var context = await CreateContextAsync();

        var subscriptionId = Guid.NewGuid();
        var mine = new Invoice(tenantId, subscriptionId, 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        mine.Issue(SystemClock.UtcNow.AddDays(-5));
        var theirs = new Invoice(tenantId, Guid.NewGuid(), 29.99m, "USD", $"inv-{Guid.NewGuid():N}");
        theirs.Issue(SystemClock.UtcNow.AddDays(-5));
        context.Set<Invoice>().AddRange(mine, theirs);
        await context.SaveChangesAsync();

        var service = new InvoiceAgingService(context, NullLogger<InvoiceAgingService>.Instance);

        var aged = await service.MarkOverdueOpenInvoicesPastDueAsync(subscriptionId, CancellationToken.None);

        aged.Should().BeEquivalentTo([mine.Id]);
        mine.Status.Should().Be(InvoiceStatus.PastDue);
        theirs.Status.Should().Be(InvoiceStatus.Open, "the other subscription's invoice stays untouched");
    }
}
