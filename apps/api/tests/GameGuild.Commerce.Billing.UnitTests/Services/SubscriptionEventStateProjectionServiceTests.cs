using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services;

public sealed class SubscriptionEventStateProjectionServiceTests
{
    private readonly SubscriptionEventStateProjectionService _sut = new();

    private static LoggedBillingEvent InvoicePaid(
        DateTimeOffset occurredAt,
        decimal amount = 100m,
        string currency = "USD",
        string provider = "stripe",
        Guid? eventId = null,
        Guid? tenantId = null) => new(
        eventId ?? Guid.NewGuid(),
        "commerce.billing.invoice-paid.v1",
        1,
        TenantId: tenantId ?? Guid.NewGuid(),
        CorrelationId: Guid.NewGuid(),
        occurredAt,
        JsonSerializer.Serialize(new
        {
            subscriptionId = Guid.NewGuid().ToString(),
            provider,
            externalEventId = "evt_test",
            eventType = "invoice.payment_succeeded",
            amount,
            currency
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    private static LoggedBillingEvent Renewed(DateTimeOffset occurredAt) => new(
        Guid.NewGuid(),
        "commerce.billing.subscription-renewed.v1",
        1,
        Guid.NewGuid(),
        Guid.NewGuid(),
        occurredAt,
        """{"subscriptionId":"00000000-0000-0000-0000-000000000001","provider":"stripe","externalEventId":"evt_renew"}""");

    private static LoggedBillingEvent Cancelled(DateTimeOffset occurredAt) => new(
        Guid.NewGuid(),
        "commerce.billing.subscription-cancelled.v1",
        1,
        Guid.NewGuid(),
        Guid.NewGuid(),
        occurredAt,
        """{"subscriptionId":"00000000-0000-0000-0000-000000000001","provider":"stripe","externalEventId":"evt_cancel"}""");

    private static LoggedBillingEvent ChangeCompleted(
        DateTimeOffset occurredAt,
        Guid toPlanId,
        Guid sagaId,
        Guid? tenantId = null) => new(
        Guid.NewGuid(),
        "commerce.billing.subscription-change-completed.v1",
        1,
        tenantId ?? Guid.NewGuid(),
        Guid.NewGuid(),
        occurredAt,
        $$"""{"sagaId":"{{sagaId}}","subscriptionId":"00000000-0000-0000-0000-000000000001","fromPlanId":"00000000-0000-0000-0000-00000000000a","toPlanId":"{{toPlanId}}","invoiceId":"00000000-0000-0000-0000-00000000000b","paymentTransactionId":"txn_1","netAdjustment":50.0,"currency":"USD"}""");

    private static LoggedBillingEvent ChangeStep(DateTimeOffset occurredAt) => new(
        Guid.NewGuid(),
        "commerce.billing.subscription-change-step.v1",
        1,
        Guid.NewGuid(),
        Guid.NewGuid(),
        occurredAt,
        """{"sagaId":"00000000-0000-0000-0000-00000000000c","subscriptionId":"00000000-0000-0000-0000-000000000001","step":"Proration","outcome":"Succeeded","detail":"ok"}""");

    [Fact]
    public void Replay_EmptyLog_ProjectsNoBillingEvents()
    {
        var subscriptionId = Guid.NewGuid();

        var projection = _sut.Replay(subscriptionId, []);

        projection.AppliedEventCount.Should().Be(0);
        projection.ProjectedImage.ProjectedStatus.Should().Be(SubscriptionProjectedStatus.NoBillingEvents);
        projection.ProjectedImage.CurrentPlanId.Should().BeNull();
        projection.ProjectedImage.PaymentCount.Should().Be(0);
    }

    [Fact]
    public void Replay_FoldsPaymentsRenewalsCancellationsAndPlanChanges()
    {
        var subscriptionId = Guid.NewGuid();
        var firstPaymentAt = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
        var renewalAt = DateTimeOffset.Parse("2026-10-01T00:00:00Z");
        var secondPaymentAt = DateTimeOffset.Parse("2026-10-01T00:05:00Z");
        var changeAt = DateTimeOffset.Parse("2026-10-05T00:00:00Z");
        var sagaId = Guid.NewGuid();
        var toPlanId = Guid.NewGuid();

        var projection = _sut.Replay(
            subscriptionId,
            [
                ChangeStep(changeAt.AddMinutes(-5)),
                InvoicePaid(firstPaymentAt, amount: 100m),
                Renewed(renewalAt),
                InvoicePaid(secondPaymentAt, amount: 100m, provider: "paypal"),
                ChangeCompleted(changeAt, toPlanId, sagaId),
                Cancelled(DateTimeOffset.Parse("2026-10-20T00:00:00Z"))
            ]);

        projection.AppliedEventCount.Should().Be(6);
        var image = projection.ProjectedImage;
        image.ProjectedStatus.Should().Be(SubscriptionProjectedStatus.Cancelled);
        image.PaymentCount.Should().Be(2);
        image.RenewalCount.Should().Be(1);
        image.TotalPaidAmount.Should().Be(200m);
        image.LastPaymentAmount.Should().Be(100m);
        image.LastPaymentCurrency.Should().Be("USD");
        image.LastPaymentAtUtc.Should().Be(secondPaymentAt);
        image.LastProvider.Should().Be("stripe", "the cancellation event carries the provider of the last provider-scoped fold");
        image.PlanChangeCount.Should().Be(1);
        image.CurrentPlanId.Should().Be(toPlanId);
        image.LastChangeSagaId.Should().Be(sagaId);
        image.LastChangeOutcome.Should().Be("Completed");
        image.CancelledAtUtc.Should().Be(DateTimeOffset.Parse("2026-10-20T00:00:00Z"));
        projection.SkippedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Replay_IsDeterministicRegardlessOfInsertionOrder()
    {
        var subscriptionId = Guid.NewGuid();
        var payEarly = InvoicePaid(DateTimeOffset.Parse("2026-09-01T00:00:00Z"), amount: 100m, eventId: Guid.NewGuid());
        var payLate = InvoicePaid(DateTimeOffset.Parse("2026-10-01T00:00:00Z"), amount: 250m, eventId: Guid.NewGuid());
        var cancelEvent = Cancelled(DateTimeOffset.Parse("2026-10-02T00:00:00Z"));

        var insertionOrder = _sut.Replay(subscriptionId, [payLate, cancelEvent, payEarly]);
        var reverseOrder = _sut.Replay(subscriptionId, [cancelEvent, payEarly, payLate]);

        insertionOrder.AppliedEventCount.Should().Be(3);
        insertionOrder.ProjectedImage.TotalPaidAmount.Should().Be(350m);
        insertionOrder.ProjectedImage.LastPaymentAmount.Should().Be(250m, "the later payment wins the fold");
        reverseOrder.ProjectedImage.Should().BeEquivalentTo(insertionOrder.ProjectedImage);
    }

    [Fact]
    public void Replay_UnknownEventNamesAreSkippedAndReportedNotFatal()
    {
        var subscriptionId = Guid.NewGuid();
        var unknown = new LoggedBillingEvent(
            Guid.NewGuid(),
            "commerce.billing.future-event.v9",
            9,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-10-01T00:00:00Z"),
            """{"anything":"goes"}""");
        var payment = InvoicePaid(DateTimeOffset.Parse("2026-10-02T00:00:00Z"));

        var projection = _sut.Replay(subscriptionId, [unknown, payment]);

        projection.AppliedEventCount.Should().Be(1, "only the known event is interpreted");
        projection.SkippedEvents.Should().ContainSingle().Which.Should().Be("commerce.billing.future-event.v9");
        projection.ProjectedImage.PaymentCount.Should().Be(1);
    }

    [Fact]
    public void Verify_ConsistentSnapshotReportsNoDrift()
    {
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var paidAt = DateTimeOffset.Parse("2026-10-01T00:00:00Z");
        var toPlanId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();
        var projection = _sut.Replay(
            subscriptionId,
            [
                InvoicePaid(paidAt, eventId: Guid.NewGuid(), tenantId: tenantId),
                ChangeCompleted(DateTimeOffset.Parse("2026-10-02T00:00:00Z"), toPlanId, sagaId, tenantId)
            ]);

        var verification = _sut.Verify(
            projection,
            new SubscriptionStateFacts(subscriptionId, tenantId, toPlanId, IsCancelled: false, LastPaymentAtUtc: paidAt));

        verification.IsConsistent.Should().BeTrue();
        verification.Drifts.Should().BeEmpty();
    }

    [Fact]
    public void Verify_PlanAndCancellationDriftAreReported()
    {
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var logPlanId = Guid.NewGuid();
        var livePlanId = Guid.NewGuid();
        var sagaId = Guid.NewGuid();
        var projection = _sut.Replay(
            subscriptionId,
            [
                InvoicePaid(DateTimeOffset.Parse("2026-10-01T00:00:00Z")),
                ChangeCompleted(DateTimeOffset.Parse("2026-10-02T00:00:00Z"), logPlanId, sagaId),
                Cancelled(DateTimeOffset.Parse("2026-10-03T00:00:00Z"))
            ]);

        var verification = _sut.Verify(
            projection,
            new SubscriptionStateFacts(subscriptionId, tenantId, livePlanId, IsCancelled: false, LastPaymentAtUtc: DateTimeOffset.Parse("2026-10-01T00:00:00Z")));

        verification.IsConsistent.Should().BeFalse();
        verification.Drifts.Should().Contain(drift => drift.Fact == "PlanId"
                                                      && drift.Projected == logPlanId.ToString()
                                                      && drift.Live == livePlanId.ToString());
        verification.Drifts.Should().Contain(drift => drift.Fact == "IsCancelled" && drift.Projected == "True" && drift.Live == "False");
    }

    [Fact]
    public void Verify_NeverPaidSubscriptionCannotDriftOnUnobservedFacts()
    {
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var projection = _sut.Replay(subscriptionId, Array.Empty<LoggedBillingEvent>());

        var verification = _sut.Verify(
            projection,
            new SubscriptionStateFacts(subscriptionId, tenantId, planId, IsCancelled: false, LastPaymentAtUtc: null));

        verification.IsConsistent.Should().BeTrue("a log with no billing events cannot evidence drift");
    }

    [Fact]
    public void Verify_TenantMismatchIsReported()
    {
        var subscriptionId = Guid.NewGuid();
        var projection = _sut.Replay(
            subscriptionId,
            new[] { InvoicePaid(DateTimeOffset.Parse("2026-10-01T00:00:00Z")) });
        var liveFacts = new SubscriptionStateFacts(
            subscriptionId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            IsCancelled: false,
            LastPaymentAtUtc: DateTimeOffset.Parse("2026-10-01T00:00:00Z"));

        var verification = _sut.Verify(projection, liveFacts);

        verification.IsConsistent.Should().BeFalse();
        verification.Drifts.Should().Contain(drift => drift.Fact == "TenantId");
    }

    [Fact]
    public void Replay_FailedChangeDoesNotMoveCurrentPlan()
    {
        var subscriptionId = Guid.NewGuid();
        var failedChange = new LoggedBillingEvent(
            Guid.NewGuid(),
            "commerce.billing.subscription-change-failed.v1",
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-10-02T00:00:00Z"),
            """{"sagaId":"00000000-0000-0000-0000-00000000000c","subscriptionId":"00000000-0000-0000-0000-000000000001","fromPlanId":"00000000-0000-0000-0000-00000000000a","toPlanId":"00000000-0000-0000-0000-00000000000b","failedStep":"Payment","reason":"declined","compensationsApplied":"invoice-voided:INV-1"}""");

        var projection = _sut.Replay(subscriptionId, new[] { failedChange });

        projection.ProjectedImage.CurrentPlanId.Should().BeNull("a failed change never moved the subscription to the new plan");
        projection.ProjectedImage.LastChangeOutcome.Should().Be("Failed");
        projection.ProjectedImage.PlanChangeCount.Should().Be(0);
    }
}
