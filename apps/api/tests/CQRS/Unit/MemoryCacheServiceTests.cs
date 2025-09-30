using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for MemoryCacheService
/// </summary>
public class MemoryCacheServiceTests
{
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _cacheService = new MemoryCacheService(cache);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Null_When_Key_NotExists()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("nonexistent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_Should_Store_And_Retrieve_Value()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_Should_Work_With_Complex_Objects()
    {
        // Arrange
        var key = "complex-object-key";
        var value = new TestObject { Id = 123, Name = "Test", CreatedAt = DateTime.UtcNow };
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(value.Id);
        result.Name.Should().Be(value.Name);
        result.CreatedAt.Should().BeCloseTo(value.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task RemoveAsync_Should_Remove_Cached_Item()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMinutes(5);

        await _cacheService.SetAsync(key, value, expiration);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_Should_Not_Throw_When_Key_NotExists()
    {
        // Act & Assert
        var act = async () => await _cacheService.RemoveAsync("nonexistent-key");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_Should_Overwrite_Existing_Value()
    {
        // Arrange
        var key = "test-key";
        var initialValue = "initial-value";
        var newValue = "new-value";
        var expiration = TimeSpan.FromMinutes(5);

        await _cacheService.SetAsync(key, initialValue, expiration);

        // Act
        await _cacheService.SetAsync(key, newValue, expiration);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(newValue);
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Set_And_Return_Value_When_Key_NotExists()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMinutes(5);
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(value);
        }, expiration);

        // Assert
        result.Should().Be(value);
        factoryCalled.Should().BeTrue();

        // Verify it's cached
        var cachedResult = await _cacheService.GetAsync<string>(key);
        cachedResult.Should().Be(value);
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Return_Cached_Value_When_Key_Exists()
    {
        // Arrange
        var key = "test-key";
        var cachedValue = "cached-value";
        var expiration = TimeSpan.FromMinutes(5);
        var factoryCalled = false;

        await _cacheService.SetAsync(key, cachedValue, expiration);

        // Act
        var result = await _cacheService.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult("new-value");
        }, expiration);

        // Assert
        result.Should().Be(cachedValue);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Handle_Null_Factory_Result()
    {
        // Arrange
        var key = "test-key";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        var result = await _cacheService.GetOrSetAsync<string>(key, () => Task.FromResult<string>(null!), expiration);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Cache_Should_Expire_Items_After_Expiration_Time()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);

        // Wait for expiration
        await Task.Delay(TimeSpan.FromMilliseconds(150));

        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Null_Values()
    {
        // Arrange
        var key = "null-value-key";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync<string>(key, null!, expiration);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Concurrent_Operations_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "concurrent-key-";
        var expiration = TimeSpan.FromMinutes(5);

        // Act - Multiple concurrent set operations
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(_cacheService.SetAsync($"{keyPrefix}{index}", $"value-{index}", expiration));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify all values were set correctly
        for (int i = 0; i < 100; i++)
        {
            var result = await _cacheService.GetAsync<string>($"{keyPrefix}{i}");
            result.Should().Be($"value-{i}");
        }
    }

    // Test helper class
    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class LargeTestObject
    {
        public Guid Id { get; set; }
        public string LargeData { get; set; } = string.Empty;
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public List<TestObject> NestedObjects { get; set; } = new();
    }
}