using FluentAssertions;
using GameGuild.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Support;

/// <summary>
///     Issue #409 remainder: manual-review cases open an in-app support ticket. The
///     coordinator persists one fraud-review ticket on the shared ticket system with the
///     case evidence, at High priority (Urgent for Critical-band scores).
/// </summary>
public sealed class FraudReviewSupportTicketCoordinatorTests
{
    [Fact]
    public async Task OpenReviewTicketAsync_Should_Persist_A_Fraud_Review_Ticket()
    {
        await using var db = CreateDbContext();
        var coordinator = new FraudReviewSupportTicketCoordinator(db, NullLogger<FraudReviewSupportTicketCoordinator>.Instance);
        var tenantId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var ticketId = await coordinator.OpenReviewTicketAsync(
            new FraudReviewTicketRequest(
                tenantId,
                paymentId,
                "Payment",
                "payment-gate",
                Score: 55,
                Reason: "score reached the review threshold",
                Amount: 99m,
                Currency: "USD",
                EvidenceJson: "{\"score\":55}"),
            CancellationToken.None);

        ticketId.Should().HaveValue();

        var stored = await db.Set<SupportTicket>()
            .Include(ticket => ticket.Messages)
            .SingleAsync(CancellationToken.None);

        stored.TenantId.Should().Be(tenantId);
        stored.Category.Should().Be("fraud-review");
        stored.Status.Should().Be(SupportTicketStatus.Open);
        stored.Priority.Should().Be(SupportTicketPriority.High);
        stored.CustomerId.Should().Be(paymentId, "the case subject rides the customer slot (course-support precedent)");
        stored.Subject.Should().Contain("Fraud review required").And.Contain("score 55");
        stored.Messages.Should().ContainSingle(message => message.Body.Contains("\"score\":55"));
    }

    [Fact]
    public async Task OpenReviewTicketAsync_Should_Use_Urgent_Priority_For_Critical_Scores()
    {
        await using var db = CreateDbContext();
        var coordinator = new FraudReviewSupportTicketCoordinator(db, NullLogger<FraudReviewSupportTicketCoordinator>.Instance);

        await coordinator.OpenReviewTicketAsync(
            new FraudReviewTicketRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Payment",
                "payment-gate",
                Score: 91,
                Reason: "unit test",
                Amount: 500m,
                Currency: "USD",
                EvidenceJson: null),
            CancellationToken.None);

        var stored = await db.Set<SupportTicket>().SingleAsync(CancellationToken.None);
        stored.Priority.Should().Be(SupportTicketPriority.Urgent);
    }

    [Fact]
    public async Task OpenReviewTicketAsync_Should_Return_Null_For_Incomplete_Case_Identity()
    {
        await using var db = CreateDbContext();
        var coordinator = new FraudReviewSupportTicketCoordinator(db, NullLogger<FraudReviewSupportTicketCoordinator>.Instance);

        var ticketId = await coordinator.OpenReviewTicketAsync(
            new FraudReviewTicketRequest(
                Guid.Empty,
                Guid.NewGuid(),
                "Payment",
                "payment-gate",
                Score: 55,
                Reason: "unit test",
                Amount: 1m,
                Currency: "USD",
                EvidenceJson: null),
            CancellationToken.None);

        ticketId.Should().BeNull();
        (await db.Set<SupportTicket>().CountAsync(CancellationToken.None)).Should().Be(0);
    }

    private static FraudReviewTicketTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FraudReviewTicketTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FraudReviewTicketTestDbContext(options);
    }

    private sealed class FraudReviewTicketTestDbContext(DbContextOptions<FraudReviewTicketTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ProductsModule.ConfigureProductsModel(modelBuilder);
        }
    }
}
