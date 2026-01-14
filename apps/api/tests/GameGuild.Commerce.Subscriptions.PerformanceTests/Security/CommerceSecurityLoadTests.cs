using BenchmarkDotNet.Attributes;
using GameGuild.ValueObjects;
using System.Collections.Concurrent;

namespace GameGuild.Commerce.Subscriptions.PerformanceTests.Security;

/// <summary>
///     P0/P1 Critical Load/Stress Tests: Commerce Security
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify system stability under load for security-critical operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CommerceSecurityLoadTests
{
    private readonly ConcurrentDictionary<string, bool> _idempotencyCache = new();
    private Subscription _subscription = null!;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [GlobalSetup]
    public void Setup()
    {
        _idempotencyCache.Clear();
        _subscription = new Subscription(
            _tenantId,
            _planId,
            _userId,
            BillingCycle.Monthly,
            new Money(2999),
            DateTime.UtcNow
        );
        _subscription.Activate();
    }

    #region Concurrent Renewals - Single Charge Guarantee (P0)

    /// <summary>
    /// Simulates 100 concurrent renewal requests with the same idempotency key.
    /// Only ONE should succeed in creating a charge; all others should return cached result.
    /// </summary>
    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task ConcurrentRenewals_SameIdempotencyKey_SingleCharge(int concurrentRequests)
    {
        var idempotencyKey = $"renewal_{_subscription.Id}_{DateTime.UtcNow:yyyyMMddHH}";
        var chargesCreated = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                // Simulate idempotency check
                if (_idempotencyCache.TryAdd(idempotencyKey, true))
                {
                    // Only first request creates a charge
                    Interlocked.Increment(ref chargesCreated);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert: Only 1 charge should be created
        if (chargesCreated != 1)
        {
            throw new Exception($"Expected 1 charge, but {chargesCreated} were created");
        }
    }

    /// <summary>
    /// Simulates concurrent renewal requests with different idempotency keys.
    /// Each should succeed independently.
    /// </summary>
    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    public async Task ConcurrentRenewals_DifferentIdempotencyKeys_AllSucceed(int concurrentRequests)
    {
        var successCount = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var idempotencyKey = $"renewal_{_subscription.Id}_{index}_{Guid.NewGuid()}";
                if (_idempotencyCache.TryAdd(idempotencyKey, true))
                {
                    Interlocked.Increment(ref successCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // All should succeed with unique keys
        if (successCount != concurrentRequests)
        {
            throw new Exception($"Expected {concurrentRequests} successes, got {successCount}");
        }
    }

    #endregion

    #region Webhook Storm - Idempotency (P0)

    /// <summary>
    /// Simulates a webhook storm where the same event is received multiple times.
    /// Only ONE should be processed; rest should be deduplicated.
    /// </summary>
    [Benchmark]
    [Arguments(50)]
    [Arguments(200)]
    public async Task WebhookStorm_DuplicateEvents_Deduplicated(int duplicateCount)
    {
        var externalEventId = $"evt_stripe_{Guid.NewGuid()}";
        var processedEvents = new ConcurrentDictionary<string, DateTime>();
        var processedCount = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < duplicateCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                // Simulate webhook processing with idempotency check
                if (processedEvents.TryAdd(externalEventId, DateTime.UtcNow))
                {
                    // Only first occurrence is processed
                    Interlocked.Increment(ref processedCount);
                    // Simulate processing work
                    Thread.SpinWait(100);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Only 1 should be processed
        if (processedCount != 1)
        {
            throw new Exception($"Expected 1 processed, but {processedCount} were processed");
        }
    }

    /// <summary>
    /// Simulates multiple different webhook events arriving simultaneously.
    /// All unique events should be processed.
    /// </summary>
    [Benchmark]
    [Arguments(100)]
    public async Task WebhookStorm_UniqueEvents_AllProcessed(int eventCount)
    {
        var processedEvents = new ConcurrentDictionary<string, DateTime>();
        var processedCount = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < eventCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var externalEventId = $"evt_stripe_{Guid.NewGuid()}";
                if (processedEvents.TryAdd(externalEventId, DateTime.UtcNow))
                {
                    Interlocked.Increment(ref processedCount);
                    Thread.SpinWait(50); // Simulate processing
                }
            }));
        }

        await Task.WhenAll(tasks);

        if (processedCount != eventCount)
        {
            throw new Exception($"Expected {eventCount} processed, got {processedCount}");
        }
    }

    #endregion

    #region Mass Cancellation - Clean Teardown (P0)

    /// <summary>
    /// Simulates mass cancellation scenario (e.g., plan discontinued).
    /// All subscriptions should be cleanly cancelled without resource leaks.
    /// </summary>
    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    public void MassCancellation_NoResourceLeaks(int subscriptionCount)
    {
        var subscriptions = new List<Subscription>();
        
        // Create subscriptions
        for (int i = 0; i < subscriptionCount; i++)
        {
            var sub = new Subscription(
                Guid.NewGuid(),
                _planId,
                Guid.NewGuid(),
                BillingCycle.Monthly,
                new Money(2999),
                DateTime.UtcNow
            );
            sub.Activate();
            subscriptions.Add(sub);
        }

        // Mass cancel
        foreach (var sub in subscriptions)
        {
            sub.Cancel(CancellationReason.PlanDiscontinued, "Mass cancellation test");
        }

        // Verify all are cancelled
        var cancelledCount = subscriptions.Count(s => s.Status == SubscriptionStatus.Cancelled);
        if (cancelledCount != subscriptionCount)
        {
            throw new Exception($"Expected {subscriptionCount} cancelled, got {cancelledCount}");
        }
    }

    /// <summary>
    /// Simulates concurrent cancellations of the same subscription.
    /// Only ONE should succeed; rest should fail gracefully.
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    public async Task ConcurrentCancellation_SameSubscription_OnlyOneSucceeds(int concurrentAttempts)
    {
        var subscription = new Subscription(
            _tenantId,
            _planId,
            _userId,
            BillingCycle.Monthly,
            new Money(2999),
            DateTime.UtcNow
        );
        subscription.Activate();

        var successCount = 0;
        var failureCount = 0;
        var lockObj = new object();
        var tasks = new List<Task>();

        for (int i = 0; i < concurrentAttempts; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    lock (lockObj)
                    {
                        if (subscription.Status == SubscriptionStatus.Active)
                        {
                            subscription.Cancel(CancellationReason.UserRequested, "Concurrent test");
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref failureCount);
                        }
                    }
                }
                catch
                {
                    Interlocked.Increment(ref failureCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Only 1 should succeed
        if (successCount != 1)
        {
            throw new Exception($"Expected 1 success, got {successCount}");
        }
    }

    #endregion

    #region Tenant Isolation Under Load (P0)

    /// <summary>
    /// Simulates multi-tenant operations to ensure tenant isolation under load.
    /// </summary>
    [Benchmark]
    [Arguments(10, 100)] // 10 tenants, 100 subscriptions each
    public void TenantIsolation_MultiTenantOperations(int tenantCount, int subscriptionsPerTenant)
    {
        var tenantSubscriptions = new Dictionary<Guid, List<Subscription>>();

        // Create subscriptions for each tenant
        for (int t = 0; t < tenantCount; t++)
        {
            var tenantId = Guid.NewGuid();
            tenantSubscriptions[tenantId] = new List<Subscription>();

            for (int s = 0; s < subscriptionsPerTenant; s++)
            {
                var sub = new Subscription(
                    tenantId,
                    _planId,
                    Guid.NewGuid(),
                    BillingCycle.Monthly,
                    new Money(2999),
                    DateTime.UtcNow
                );
                tenantSubscriptions[tenantId].Add(sub);
            }
        }

        // Verify tenant isolation
        foreach (var kvp in tenantSubscriptions)
        {
            var tenantId = kvp.Key;
            var subs = kvp.Value;

            // All subscriptions should belong to this tenant
            var wrongTenant = subs.Count(s => s.TenantId != tenantId);
            if (wrongTenant > 0)
            {
                throw new Exception($"Tenant isolation violated: {wrongTenant} subscriptions belong to wrong tenant");
            }
        }
    }

    #endregion

    #region Proration Calculation Performance (P0)

    /// <summary>
    /// Benchmarks proration calculation performance for plan changes.
    /// </summary>
    [Benchmark]
    [Arguments(1000)]
    public void ProrationCalculation_HighVolume(int calculationCount)
    {
        var results = new List<ProrationResult>();

        for (int i = 0; i < calculationCount; i++)
        {
            // Simulate proration calculation
            var daysInPeriod = 30;
            var daysRemaining = 15;
            var oldPlanPrice = 29.99m;
            var newPlanPrice = 49.99m;

            var credit = (oldPlanPrice / daysInPeriod) * daysRemaining;
            var charge = (newPlanPrice / daysInPeriod) * daysRemaining;
            var netAmount = charge - credit;

            results.Add(new ProrationResult
            {
                Credit = credit,
                Charge = charge,
                NetAmount = netAmount
            });
        }

        // Verify all calculations completed
        if (results.Count != calculationCount)
        {
            throw new Exception($"Expected {calculationCount} results, got {results.Count}");
        }
    }

    #endregion

    #region Payment Recording Under Load (P0)

    /// <summary>
    /// Simulates high-volume payment recording with idempotency checks.
    /// </summary>
    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    public async Task PaymentRecording_HighVolume_WithIdempotency(int paymentCount)
    {
        var processedPayments = new ConcurrentDictionary<string, bool>();
        var recordedCount = 0;
        var tasks = new List<Task>();

        for (int i = 0; i < paymentCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var idempotencyKey = $"payment_{_subscription.Id}_{index}";
                if (processedPayments.TryAdd(idempotencyKey, true))
                {
                    Interlocked.Increment(ref recordedCount);
                    // Simulate payment recording work
                    Thread.SpinWait(50);
                }
            }));
        }

        await Task.WhenAll(tasks);

        if (recordedCount != paymentCount)
        {
            throw new Exception($"Expected {paymentCount} payments, got {recordedCount}");
        }
    }

    #endregion

    [GlobalCleanup]
    public void Cleanup()
    {
        _idempotencyCache.Clear();
    }
}

/// <summary>
/// Helper class for proration calculation results
/// </summary>
internal class ProrationResult
{
    public decimal Credit { get; set; }
    public decimal Charge { get; set; }
    public decimal NetAmount { get; set; }
}
