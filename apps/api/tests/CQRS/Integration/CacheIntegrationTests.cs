using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Tests.CQRS.Integration;

/// <summary>
/// Integration tests for CQRS cache functionality
/// </summary>
public class CacheIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly MemoryCacheService _cacheService;

    public CacheIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddCqrs(Assembly.GetExecutingAssembly());
        services.AddSingleton<MemoryCacheService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _cacheService = _scope.ServiceProvider.GetRequiredService<MemoryCacheService>();
    }

    [Fact]
    public async Task Cache_Should_Integrate_With_Dependency_Injection()
    {
        // Act
        var cacheFromDI = _scope.ServiceProvider.GetService<MemoryCacheService>();

        // Assert
        cacheFromDI.Should().NotBeNull();
        cacheFromDI.Should().BeSameAs(_cacheService);
    }

    [Fact]
    public async Task Multiple_Cache_Instances_Should_Be_Independent()
    {
        // Arrange
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var cache1 = scope1.ServiceProvider.GetRequiredService<MemoryCacheService>();
        var cache2 = scope2.ServiceProvider.GetRequiredService<MemoryCacheService>();

        var key = "test-key";
        var value1 = "value1";
        var value2 = "value2";

        // Act
        await cache1.SetAsync(key, value1, TimeSpan.FromMinutes(5));
        await cache2.SetAsync(key, value2, TimeSpan.FromMinutes(5));

        var result1 = await cache1.GetAsync<string>(key);
        var result2 = await cache2.GetAsync<string>(key);

        // Assert
        result1.Should().Be(value1);
        result2.Should().Be(value2);
    }

    [Fact]
    public async Task Cache_Should_Handle_Complex_Serialization_Scenarios()
    {
        // Arrange
        var key = "complex-object";
        var complexObject = new ComplexTestObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            CreatedAt = DateTime.UtcNow,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "string value" },
                { "key2", 42 },
                { "key3", true },
                { "key4", DateTime.UtcNow }
            },
            NestedObject = new NestedTestObject
            {
                Value = "nested value",
                Number = 123.45
            }
        };

        // Act
        await _cacheService.SetAsync(key, complexObject, TimeSpan.FromMinutes(5));
        var result = await _cacheService.GetAsync<ComplexTestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(complexObject.Id);
        result.Name.Should().Be(complexObject.Name);
        result.Tags.Should().BeEquivalentTo(complexObject.Tags);
        result.Metadata.Should().HaveCount(4);
        result.NestedObject.Should().NotBeNull();
        result.NestedObject.Value.Should().Be(complexObject.NestedObject.Value);
    }

    [Fact]
    public async Task Cache_Should_Handle_Concurrent_Access_Correctly()
    {
        // Arrange
        const int taskCount = 50;
        const int operationsPerTask = 20;
        var tasks = new List<Task>();

        // Act - Multiple concurrent operations
        for (int i = 0; i < taskCount; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < operationsPerTask; j++)
                {
                    var key = $"concurrent-key-{taskIndex}-{j}";
                    var value = $"value-{taskIndex}-{j}";

                    await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
                    var retrieved = await _cacheService.GetAsync<string>(key);

                    retrieved.Should().Be(value);
                }
            }));
        }

        // Assert
        await Task.WhenAll(tasks);
        // If we reach here without exceptions, concurrent access is working correctly
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Work_With_Factory_Pattern()
    {
        // Arrange
        var key = "factory-test";
        var expensiveOperationCalled = false;
        var expensiveValue = "expensive-result";

        // Act - First call should execute factory
        var result1 = await _cacheService.GetOrSetAsync(key, async () =>
        {
            expensiveOperationCalled = true;
            await Task.Delay(100); // Simulate expensive operation
            return expensiveValue;
        }, TimeSpan.FromMinutes(5));

        // Reset flag
        expensiveOperationCalled = false;

        // Second call should use cached value
        var result2 = await _cacheService.GetOrSetAsync(key, async () =>
        {
            expensiveOperationCalled = true;
            await Task.Delay(100);
            return "should not be called";
        }, TimeSpan.FromMinutes(5));

        // Assert
        result1.Should().Be(expensiveValue);
        result2.Should().Be(expensiveValue);
        expensiveOperationCalled.Should().BeFalse(); // Factory should not be called second time
    }

    [Fact]
    public async Task Cache_Should_Properly_Handle_Memory_Pressure()
    {
        // Arrange - Fill cache with many items
        const int itemCount = 1000;
        var keys = new List<string>();

        // Act - Add many items to cache
        for (int i = 0; i < itemCount; i++)
        {
            var key = $"memory-test-{i}";
            var value = new string('x', 1000); // 1KB strings
            keys.Add(key);

            await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(30));
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Check that we can still retrieve recently added items
        var lastKey = keys.Last();
        var lastValue = await _cacheService.GetAsync<string>(lastKey);

        // Assert
        lastValue.Should().NotBeNull();
        lastValue!.Length.Should().Be(1000);
    }

    [Fact]
    public async Task Cache_Expiration_Should_Work_Reliably()
    {
        // Arrange
        var key = "expiration-test";
        var value = "test-value";
        var shortExpiration = TimeSpan.FromMilliseconds(200);

        // Act
        await _cacheService.SetAsync(key, value, shortExpiration);

        // Verify item is initially cached
        var initialResult = await _cacheService.GetAsync<string>(key);

        // Wait for expiration
        await Task.Delay(TimeSpan.FromMilliseconds(300));

        // Try to retrieve after expiration
        var expiredResult = await _cacheService.GetAsync<string>(key);

        // Assert
        initialResult.Should().Be(value);
        expiredResult.Should().BeNull();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    // Test helper classes
    public class ComplexTestObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public NestedTestObject NestedObject { get; set; } = new();
    }

    public class NestedTestObject
    {
        public string Value { get; set; } = string.Empty;
        public double Number { get; set; }
    }
}