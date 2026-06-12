using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GameGuild.Commerce.Orders;

namespace GameGuild.Commerce.Orders.PerformanceTests;

/// <summary>
/// Performance benchmarks for Orders module operations.
/// Measures throughput and latency for critical order processing paths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class OrderPerformanceBenchmarks
{
    private Guid _tenantId;
    private Guid _userId;
    private Order[] _orders = [];
    private Dictionary<Guid, Order> _ordersById = [];

    [GlobalSetup]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _orders = Enumerable.Range(0, 10_000)
            .Select(index =>
            {
                var order = Order.Create(_userId, $"order-benchmark-{index}", _tenantId);
                order.AddLineItem(Guid.NewGuid(), $"Product {index % 200}", 10m + index % 75, quantity: 1 + index % 4);
                if (index % 9 == 0)
                {
                    order.PlaceOnHold("manual review");
                }

                return order;
            })
            .ToArray();
        _ordersById = _orders.ToDictionary(order => order.Id);
    }

    /// <summary>
    /// Benchmark: Order creation throughput.
    /// Target: Create 500 orders/second under load.
    /// </summary>
    [Benchmark]
    public decimal OrderCreation_Throughput()
    {
        var total = 0m;
        for (var index = 0; index < 500; index++)
        {
            var order = Order.Create(_userId, $"created-{Guid.NewGuid():N}", _tenantId);
            order.AddLineItem(Guid.NewGuid(), "Benchmark SKU", 29.99m, quantity: 2);
            total += order.Total;
        }

        return total;
    }

    /// <summary>
    /// Benchmark: Order lookup by ID latency.
    /// Target: Retrieve order within 10ms P99.
    /// </summary>
    [Benchmark]
    public decimal OrderLookup_Latency()
    {
        var total = 0m;
        foreach (var id in _orders.Take(1_000).Select(order => order.Id))
        {
            total += _ordersById[id].Total;
        }

        return total;
    }

    /// <summary>
    /// Benchmark: Order list pagination performance.
    /// Target: Return 100 orders within 50ms.
    /// </summary>
    [Benchmark]
    public decimal OrderListPagination_Performance()
    {
        return _orders
            .OrderByDescending(order => order.CreatedAt)
            .Skip(400)
            .Take(100)
            .Sum(order => order.Total);
    }

    /// <summary>
    /// Benchmark: Concurrent order modifications.
    /// Target: Handle 100 concurrent modifications without conflicts.
    /// </summary>
    [Benchmark]
    public int OrderModification_ConcurrentLoad()
    {
        var modified = 0;
        Parallel.For(0, 100, index =>
        {
            var order = Order.Create(_userId, $"modify-{index}-{Guid.NewGuid():N}", _tenantId);
            order.AddLineItem(Guid.NewGuid(), "Reviewable SKU", 15m + index, quantity: 1);
            order.PlaceOnHold("benchmark hold");
            order.Release();
            Interlocked.Increment(ref modified);
        });

        return modified;
    }

    /// <summary>
    /// Benchmark: Order search with filters.
    /// Target: Search 10000 orders in under 100ms.
    /// </summary>
    [Benchmark]
    public int OrderSearch_WithFilters()
    {
        return _orders.Count(order =>
            order.TenantId == _tenantId &&
            order.UserId == _userId &&
            order.Total >= 80m &&
            order.Status is OrderStatus.Pending or OrderStatus.OnHold);
    }
}
