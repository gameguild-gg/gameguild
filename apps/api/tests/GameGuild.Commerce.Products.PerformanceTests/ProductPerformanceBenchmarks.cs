using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GameGuild.Commerce.Products;

namespace GameGuild.Commerce.Products.PerformanceTests;

/// <summary>
/// Performance benchmarks for Products module operations.
/// Measures throughput, latency, and resource consumption for catalog operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ProductPerformanceBenchmarks
{
    private Guid _tenantId;
    private Product[] _products = [];
    private Dictionary<Guid, Product> _productsById = [];
    private ProductPricing[] _pricing = [];
    private InventorySnapshot[] _inventory = [];

    [GlobalSetup]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        _products = Enumerable.Range(0, 100_000)
            .Select(index => Product.Create(
                $"GameGuild Product {index:D5}",
                type: (ProductType)(index % 13),
                description: $"Catalog entry {index}",
                shortDescription: $"SKU {index}",
                isBundle: index % 25 == 0,
                tenantId: _tenantId))
            .ToArray();

        _productsById = _products.ToDictionary(product => product.Id);
        _pricing = _products.Take(10_000)
            .Select((product, index) => ProductPricing.CreateWithVersion(
                product.Id,
                "Default",
                basePrice: 15m + index % 300,
                salePrice: index % 5 == 0 ? 9m + index % 100 : null,
                currency: "USD",
                isDefault: true,
                tenantId: _tenantId).Pricing)
            .ToArray();

        _inventory = _products.Take(10_000)
            .Select((product, index) => new InventorySnapshot(product.Id, 25 + index % 200, DateTime.UtcNow.AddMinutes(-index)))
            .ToArray();
    }

    /// <summary>
    /// Benchmark: Product creation throughput.
    /// Target: Create 1000 products/second under load.
    /// </summary>
    [Benchmark]
    public int ProductCreation_Throughput()
    {
        return Enumerable.Range(0, 1_000)
            .Select(index => Product.Create($"Created Product {index}", ProductType.Course, tenantId: _tenantId))
            .Count(product => product.Id != Guid.Empty && product.TenantId == _tenantId);
    }

    /// <summary>
    /// Benchmark: Product lookup by ID latency.
    /// Target: Retrieve product within 5ms P99.
    /// </summary>
    [Benchmark]
    public int ProductLookup_Latency()
    {
        return _products.Take(1_000).Count(product => _productsById.ContainsKey(product.Id));
    }

    /// <summary>
    /// Benchmark: Product catalog search performance.
    /// Target: Search 100000 products in under 100ms.
    /// </summary>
    [Benchmark]
    public int ProductCatalogSearch_Performance()
    {
        return _products.Count(product =>
            product.IsPublished &&
            product.Name.Contains("Product 09", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Benchmark: Product filtering with multiple criteria.
    /// Target: Filter 10000 products in under 50ms.
    /// </summary>
    [Benchmark]
    public int ProductFiltering_WithMultipleCriteria()
    {
        return _products.Take(10_000).Count(product =>
            product.TenantId == _tenantId &&
            product.IsPublished &&
            product.Type is ProductType.Course or ProductType.Program or ProductType.Workshop &&
            !product.IsBundle);
    }

    /// <summary>
    /// Benchmark: Bulk product import.
    /// Target: Import 10000 products in under 30 seconds.
    /// </summary>
    [Benchmark]
    public int BulkProductImport_Throughput()
    {
        var imported = new List<Product>(capacity: 10_000);
        for (var index = 0; index < 10_000; index++)
        {
            imported.Add(Product.Create($"Imported Product {index}", ProductType.ResourcePack, tenantId: _tenantId));
        }

        return imported.Count(product => product.TenantId == _tenantId);
    }

    /// <summary>
    /// Benchmark: Product pricing calculation at scale.
    /// Target: Calculate 1000 product prices in under 100ms.
    /// </summary>
    [Benchmark]
    public decimal PricingCalculation_AtScale()
    {
        return _pricing.Take(1_000).Sum(price => price.GetCurrentPrice());
    }

    /// <summary>
    /// Benchmark: Inventory update throughput.
    /// Target: Update 500 inventory records/second.
    /// </summary>
    [Benchmark]
    public int InventoryUpdate_Throughput()
    {
        var updatedAt = DateTime.UtcNow;
        return _inventory.Take(500)
            .Select(item => item with { Available = item.Available - 1, UpdatedAt = updatedAt })
            .Count(item => item.Available >= 0 && item.UpdatedAt == updatedAt);
    }

    private sealed record InventorySnapshot(Guid ProductId, int Available, DateTime UpdatedAt);
}
