using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions.PerformanceTests;

/// <summary>
/// Performance benchmarks for Subscription entity operations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SubscriptionPerformanceBenchmarks
{
    private Subscription _subscription = null!;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [GlobalSetup]
    public void Setup()
    {
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

    [Benchmark]
    public void CreateSubscription()
    {
        var subscription = new Subscription(
            _tenantId,
            _planId,
            _userId,
            BillingCycle.Monthly,
            new Money(2999),
            DateTime.UtcNow
        );
    }

    [Benchmark]
    public void ActivateSubscription()
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
    }

    [Benchmark]
    public void CancelSubscription()
    {
        _subscription.Cancel(CancellationReason.UserRequested, "Performance test");
    }

    [Benchmark]
    public void RecordPayment()
    {
        _subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, $"perf_test_{Guid.NewGuid()}");
    }

    [Benchmark]
    public void UpdateMetadata()
    {
        _subscription.UpdateMetadata("{\"key\":\"value\",\"test\":\"data\"}");
    }

    [Benchmark]
    public void ChangePlan()
    {
        _subscription.ChangePlan(Guid.NewGuid(), new Money(4999));
    }
}


