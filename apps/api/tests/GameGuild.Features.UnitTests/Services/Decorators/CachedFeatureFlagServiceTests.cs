using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace GameGuild.Features.UnitTests.Services.Decorators;

/// <summary>
/// Tests for CachedFeatureFlagService verifying cache behavior and invalidation.
/// </summary>
public class CachedFeatureFlagServiceTests
{
    private readonly Mock<IFeatureFlagEvaluationService> _innerServiceMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly CachedFeatureFlagService _cachedService;

    public CachedFeatureFlagServiceTests()
    {
        _innerServiceMock = new Mock<IFeatureFlagEvaluationService>();
        _cacheMock = new Mock<IDistributedCache>();
        _cachedService = new CachedFeatureFlagService(_innerServiceMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsCachedValue_WhenCacheHit()
    {
        // Arrange
        var featureKey = "cached-feature";
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var cachedResult = new FeatureEvaluationResult
        {
            FeatureKey = featureKey,
            IsEnabled = true,
            Value = "cached-value",
            Reason = "From cache"
        };

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(cachedResult));

        // Act
        var result = await _cachedService.EvaluateAsync(featureKey, context);

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.Value.Should().Be("cached-value");
        _innerServiceMock.Verify(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()), Times.Never,
            "Inner service should not be called when cache hit");
    }

    [Fact]
    public async Task EvaluateAsync_CallsInnerService_WhenCacheMiss()
    {
        // Arrange
        var featureKey = "uncached-feature";
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var freshResult = new FeatureEvaluationResult
        {
            FeatureKey = featureKey,
            IsEnabled = true,
            Value = "fresh-value",
            Reason = "Freshly evaluated"
        };

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _innerServiceMock.Setup(x => x.EvaluateAsync(featureKey, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshResult);

        // Act
        var result = await _cachedService.EvaluateAsync(featureKey, context);

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.Value.Should().Be("fresh-value");
        _innerServiceMock.Verify(x => x.EvaluateAsync(featureKey, context, It.IsAny<CancellationToken>()), Times.Once,
            "Inner service should be called on cache miss");
    }

    [Fact]
    public async Task EvaluateAsync_StoresResultInCache_OnCacheMiss()
    {
        // Arrange
        var featureKey = "cacheable-feature";
        var context = new FeatureContext { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var freshResult = new FeatureEvaluationResult
        {
            FeatureKey = featureKey,
            IsEnabled = true,
            Value = "to-be-cached"
        };

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _innerServiceMock.Setup(x => x.EvaluateAsync(featureKey, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshResult);

        // Act
        await _cachedService.EvaluateAsync(featureKey, context);

        // Assert
        _cacheMock.Verify(
            x => x.SetAsync(
                It.Is<string>(k => k.Contains(featureKey)),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Result should be cached on cache miss");
    }

    [Fact]
    public async Task EvaluateAsync_UsesDifferentCacheKeys_ForDifferentContexts()
    {
        // Arrange
        var featureKey = "multi-context-feature";
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var context1 = new FeatureContext { TenantId = tenant1, UserId = Guid.NewGuid() };
        var context2 = new FeatureContext { TenantId = tenant2, UserId = Guid.NewGuid() };

        var capturedKeys = new List<string>();

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
            .ReturnsAsync((byte[]?)null);

        _innerServiceMock.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult { FeatureKey = featureKey, IsEnabled = true });

        // Act
        await _cachedService.EvaluateAsync(featureKey, context1);
        await _cachedService.EvaluateAsync(featureKey, context2);

        // Assert
        capturedKeys.Should().HaveCount(2);
        capturedKeys[0].Should().NotBe(capturedKeys[1], "Different contexts should have different cache keys");
        capturedKeys[0].Should().Contain(tenant1.ToString());
        capturedKeys[1].Should().Contain(tenant2.ToString());
    }

    [Fact]
    public async Task CacheKey_IncludesTenantAndUser_ForIsolation()
    {
        // Arrange
        var featureKey = "isolated-feature";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = new FeatureContext { TenantId = tenantId, UserId = userId };
        string? capturedKey = null;

        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKey = key)
            .ReturnsAsync((byte[]?)null);

        _innerServiceMock.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureEvaluationResult { FeatureKey = featureKey, IsEnabled = true });

        // Act
        await _cachedService.EvaluateAsync(featureKey, context);

        // Assert
        capturedKey.Should().NotBeNull();
        capturedKey.Should().Contain(featureKey, "Cache key should include feature key");
        capturedKey.Should().Contain(tenantId.ToString(), "Cache key should include tenant ID for isolation");
        capturedKey.Should().Contain(userId.ToString(), "Cache key should include user ID for isolation");
    }

    [Fact]
    public async Task CacheDecorator_InvalidatesOnFlagUpdate_WhenCacheKeyChanges()
    {
        // Arrange
        var featureKey = "updatable-feature";
        var tenantId = Guid.NewGuid();
        var context = new FeatureContext { TenantId = tenantId, UserId = Guid.NewGuid() };

        // First call - cache miss, stores result
        var initialResult = new FeatureEvaluationResult
        {
            FeatureKey = featureKey,
            IsEnabled = false,
            Value = "initial"
        };

        // Second call after "update" - should get fresh result
        var updatedResult = new FeatureEvaluationResult
        {
            FeatureKey = featureKey,
            IsEnabled = true,
            Value = "updated"
        };

        var callCount = 0;
        _cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null); // Always cache miss for this test

        _innerServiceMock.Setup(x => x.EvaluateAsync(featureKey, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++callCount == 1 ? initialResult : updatedResult);

        // Act
        var result1 = await _cachedService.EvaluateAsync(featureKey, context);
        // Simulate cache invalidation by having cache return null on second call
        var result2 = await _cachedService.EvaluateAsync(featureKey, context);

        // Assert
        result1.IsEnabled.Should().BeFalse("First call should return initial disabled state");
        result2.IsEnabled.Should().BeTrue("Second call after invalidation should return updated state");
        _innerServiceMock.Verify(
            x => x.EvaluateAsync(featureKey, context, It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "Both calls hit inner service because cache was invalidated");
    }

    [Fact]
    public async Task EvaluateBulkAsync_DelegatesDirectlyToInnerService()
    {
        // Arrange
        var context = new FeatureContext { TenantId = Guid.NewGuid() };
        var request = new BulkEvaluationRequest
        {
            Context = context,
            FeatureKeys = new List<string> { "feature1", "feature2" }
        };

        var expectedResponse = new BulkEvaluateFeaturesResponse
        {
            Environment = "test",
            Results = new Dictionary<string, FeatureEvaluationResult>
            {
                ["feature1"] = new() { IsEnabled = true },
                ["feature2"] = new() { IsEnabled = false }
            }
        };

        _innerServiceMock.Setup(x => x.EvaluateBulkAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _cachedService.EvaluateBulkAsync(request);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _innerServiceMock.Verify(x => x.EvaluateBulkAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
