using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.FraudQueries;

/// <summary>
///     Issue #409 remainder: manual-review detail enrichment. The detail read model
///     carries the customer's prior-transaction history, the payment details (method,
///     IP, country) and the device/session facts recovered from the evidence bundle.
/// </summary>
public class GetFraudReviewDetailQueryTests : IAsyncDisposable
{
    private readonly PaymentsPersistenceTestDbContext _context = new(
        new DbContextOptionsBuilder<PaymentsPersistenceTestDbContext>()
            .UseInMemoryDatabase($"fraud-review-detail-{Guid.NewGuid():N}")
            .Options);

    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_Return_The_Enriched_Detail_For_A_Held_Payment()
    {
        var held = NewPayment(amount: 99m, externalCustomerId: "cus_review");
        held.RecordFraudAttemptContext("203.0.113.9", "fp-review");
        held.RecordBillingCountry("PT");
        held.RecordFraudAssessment(
            FraudDecision.Review,
            55,
            "score reached the review threshold",
            EvidenceJson());
        _context.Set<Payment>().Add(held);

        var prior = NewPayment(amount: 40m, externalCustomerId: "cus_review");
        prior.CreatedAt = SystemClock.UtcNow.AddDays(-30);
        prior.MarkAsProcessing();
        prior.MarkAsSucceeded("ch_prior");
        _context.Set<Payment>().Add(prior);
        await _context.SaveChangesAsync();

        var handler = new GetFraudReviewDetailQueryHandler(_context);

        var detail = await handler.Handle(new GetFraudReviewDetailQuery(held.Id), CancellationToken.None);

        detail.Should().NotBeNull();
        detail!.Case.PaymentId.Should().Be(held.Id);
        detail.Case.Score.Should().Be(55);
        detail.History.PriorAttemptCount.Should().Be(1);
        detail.History.PriorSucceededCount.Should().Be(1);
        detail.History.PriorVolume.Should().Be(40m);
        detail.History.AccountAgeDays.Should().BeGreaterThanOrEqualTo(29);
        detail.PaymentContext.PaymentMethodId.Should().Be("pm_test");
        detail.PaymentContext.SourceIpAddress.Should().Be("203.0.113.9");
        detail.PaymentContext.BillingCountry.Should().Be("PT");
        detail.DeviceContext.DeviceFingerprint.Should().Be("fp-review");
        detail.DeviceContext.ClientTimezone.Should().Be("Europe/Lisbon");
        detail.DeviceContext.ResolvedTimezone.Should().Be("America/New_York");
        detail.DeviceContext.AnonymizerDetected.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_Null_For_An_Unassessed_Payment()
    {
        var payment = NewPayment(amount: 10m, externalCustomerId: "cus_none");
        _context.Set<Payment>().Add(payment);
        await _context.SaveChangesAsync();

        var handler = new GetFraudReviewDetailQueryHandler(_context);

        var detail = await handler.Handle(new GetFraudReviewDetailQuery(payment.Id), CancellationToken.None);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Respect_The_Tenant_Scope()
    {
        var payment = NewPayment(amount: 10m, externalCustomerId: "cus_other_tenant");
        payment.RecordFraudAssessment(FraudDecision.Review, 55, "score reached the review threshold", null);
        _context.Set<Payment>().Add(payment);
        await _context.SaveChangesAsync();

        var handler = new GetFraudReviewDetailQueryHandler(_context);

        var detail = await handler.Handle(
            new GetFraudReviewDetailQuery(payment.Id, TenantId: Guid.NewGuid()),
            CancellationToken.None);

        detail.Should().BeNull();
    }

    private Payment NewPayment(decimal amount, string externalCustomerId)
    {
        var payment = Payment.Create(
            _tenantId,
            amount,
            "USD",
            $"key-{Guid.NewGuid():N}",
            subscriptionId: Guid.NewGuid(),
            externalCustomerId: externalCustomerId,
            paymentMethodId: "pm_test");
        return payment;
    }

    private static string EvidenceJson()
    {
        var assessment = new FraudAssessment(
            Guid.NewGuid(),
            Guid.Empty,
            55,
            FraudRiskLevel.Medium,
            FraudDecision.Review,
            "unit test",
            [
                new FraudSignal("GeoIp.TimezoneMismatch", 20, """{"claimedTimezone":"Europe/Lisbon","resolvedTimezone":"America/New_York"}"""),
                new FraudSignal("GeoIp.Anonymizer", 35, """{"isAnonymousVpn":true,"isPublicProxy":false,"isTorExitNode":false,"isHostingProvider":false}""")
            ],
            SystemClock.UtcNow);
        return assessment.ToEvidenceJson();
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
