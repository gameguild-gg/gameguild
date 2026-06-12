using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GameGuild.Commerce.Billing;

namespace GameGuild.Commerce.Billing.PerformanceTests;

/// <summary>
/// Performance benchmarks for Billing module operations.
/// Measures throughput, latency, and resource consumption for critical billing paths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class BillingPerformanceBenchmarks
{
    private StripePaymentWebhookPayload[] _stripePayments = [];
    private PayPalPaymentWebhookPayload[] _payPalPayments = [];
    private StripeSubscriptionWebhookPayload[] _subscriptions = [];
    private RetryQueueItem[] _retryQueue = [];

    [GlobalSetup]
    public void Setup()
    {
        var tenantIds = Enumerable.Range(0, 25).Select(_ => Guid.NewGuid()).ToArray();
        var now = DateTime.UtcNow;

        _stripePayments = Enumerable.Range(0, 1_000)
            .Select(index => new StripePaymentWebhookPayload
            {
                PaymentId = $"pi_{index:N0}",
                ExternalSubscriptionId = $"sub_{index % 250:N0}",
                TenantId = tenantIds[index % tenantIds.Length],
                Amount = 19.99m + index % 13,
                Currency = "USD",
                Status = index % 11 == 0 ? "failed" : "succeeded",
                PaidAt = now.AddMinutes(-index),
                CustomerId = $"cus_{index % 400:N0}",
                InvoiceId = $"in_{index:N0}",
                ChargeId = $"ch_{index:N0}"
            })
            .ToArray();

        _payPalPayments = Enumerable.Range(0, 1_000)
            .Select(index => new PayPalPaymentWebhookPayload
            {
                PaymentId = $"paypal_payment_{index:N0}",
                ExternalSubscriptionId = $"paypal_sub_{index % 250:N0}",
                TenantId = tenantIds[index % tenantIds.Length],
                Amount = 24.99m + index % 17,
                Currency = "USD",
                Status = index % 7 == 0 ? "pending" : "completed",
                PaidAt = now.AddMinutes(-index),
                TransactionId = $"txn_{index:N0}",
                PayerId = $"payer_{index % 300:N0}",
                IsRefund = index % 19 == 0
            })
            .ToArray();

        _subscriptions = Enumerable.Range(0, 2_500)
            .Select(index => new StripeSubscriptionWebhookPayload
            {
                ExternalSubscriptionId = $"sub_{index:N0}",
                TenantId = tenantIds[index % tenantIds.Length],
                PlanId = Guid.NewGuid(),
                Status = index % 13 == 0 ? "canceled" : "active",
                Amount = 29m + index % 9,
                StartDate = now.AddMonths(-index % 12),
                NextBillingDate = now.AddDays(index % 30),
                CustomerId = $"cus_{index % 400:N0}",
                ProductId = $"prod_{index % 20:N0}",
                PriceId = $"price_{index % 40:N0}",
                Interval = index % 4 == 0 ? "year" : "month",
                CancelAtPeriodEnd = index % 23 == 0
            })
            .ToArray();

        _retryQueue = Enumerable.Range(0, 5_000)
            .Select(index => new RetryQueueItem(
                EventId: $"evt_retry_{index:N0}",
                TenantId: tenantIds[index % tenantIds.Length],
                Attempt: index % 5,
                DueAt: now.AddSeconds(index % 3 == 0 ? -index : index)))
            .ToArray();
    }

    /// <summary>
    /// Benchmark: Invoice generation throughput.
    /// Target: Generate 1000 invoices/second under load.
    /// </summary>
    [Benchmark]
    public decimal InvoiceGeneration_Throughput()
    {
        return _subscriptions
            .Where(subscription => subscription.Status == "active" && subscription.CancelAtPeriodEnd == false)
            .GroupBy(subscription => subscription.TenantId)
            .Select(group => new InvoiceDraft(group.Key, group.Count(), group.Sum(subscription => subscription.Amount)))
            .Sum(invoice => invoice.TotalAmount);
    }

    /// <summary>
    /// Benchmark: Webhook processing latency.
    /// Target: Process webhook within 100ms P99.
    /// </summary>
    [Benchmark]
    public int WebhookProcessing_Latency()
    {
        var processed = 0;

        foreach (var payment in _stripePayments)
        {
            var normalized = UnifiedWebhookEvent.FromStripePayment(payment, "invoice.payment_succeeded", $"evt_{processed}");
            if (normalized.Status is WebhookEventStatus.Success or WebhookEventStatus.Failed)
            {
                processed++;
            }
        }

        foreach (var payment in _payPalPayments)
        {
            var normalized = UnifiedWebhookEvent.FromPayPalPayment(payment, "PAYMENT.CAPTURE.COMPLETED", $"paypal_evt_{processed}");
            if (normalized.Status is WebhookEventStatus.Success or WebhookEventStatus.Pending)
            {
                processed++;
            }
        }

        return processed;
    }

    /// <summary>
    /// Benchmark: Payment retry queue throughput.
    /// Target: Process 500 retries/second.
    /// </summary>
    [Benchmark]
    public int PaymentRetryQueue_Throughput()
    {
        var now = DateTime.UtcNow;
        return _retryQueue
            .Where(item => item.DueAt <= now && item.Attempt < 4)
            .OrderBy(item => item.DueAt)
            .Take(500)
            .Count();
    }

    /// <summary>
    /// Benchmark: Concurrent billing cycle processing.
    /// Target: Handle 100 concurrent billing cycles.
    /// </summary>
    [Benchmark]
    public decimal BillingCycle_ConcurrentProcessing()
    {
        return _subscriptions
            .AsParallel()
            .Where(subscription => subscription.Status == "active")
            .GroupBy(subscription => subscription.TenantId)
            .Select(group => group.Sum(subscription => subscription.Amount))
            .Sum();
    }

    private sealed record InvoiceDraft(Guid TenantId, int LineCount, decimal TotalAmount);

    private sealed record RetryQueueItem(string EventId, Guid TenantId, int Attempt, DateTime DueAt);
}
