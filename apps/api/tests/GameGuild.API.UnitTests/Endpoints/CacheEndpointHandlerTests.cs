using FluentAssertions;
using GameGuild.API.Endpoints;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Moq;

namespace GameGuild.API.UnitTests.Endpoints;

public sealed class CacheEndpointHandlerTests
{
    [Fact]
    public async Task ClearCacheByPattern_ShouldUsePatternCacheServiceAndReportRemovedCount()
    {
        var cache = new Mock<IPatternCacheService>();
        cache
            .Setup(x => x.RemoveByPatternAsync("tenant:42:*", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:42:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<int>(value, "RemovedCount").Should().Be(3);
        GetProperty<string>(value, "Pattern").Should().Be("tenant:42:*");
        cache.Verify(x => x.RemoveByPatternAsync("tenant:42:*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldReturnBadRequestWhenCacheDoesNotSupportPatternRemoval()
    {
        var cache = new Mock<ICacheService>();

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<string>(value, "Pattern").Should().Be("tenant:*");
        GetProperty<string>(value, "Message").Should().Contain("does not support pattern-based cache clearing");
    }

    [Fact]
    public async Task ClearCacheByPattern_ShouldReturnBadRequestWhenPatternServiceRejectsPattern()
    {
        var cache = new Mock<IPatternCacheService>();
        cache
            .Setup(x => x.RemoveByPatternAsync("tenant:*", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Wildcard scan is not available."));

        var result = await CacheEndpointHandlers.ClearCacheByPattern("tenant:*", cache.Object, CancellationToken.None);

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var value = result.Should().BeAssignableTo<IValueHttpResult>().Subject.Value;
        GetProperty<string>(value, "Error").Should().Be("Wildcard scan is not available.");
    }

    private static T GetProperty<T>(object? value, string name)
    {
        value.Should().NotBeNull();
        var property = value!.GetType().GetProperty(name);
        property.Should().NotBeNull();
        return property!.GetValue(value).Should().BeAssignableTo<T>().Subject;
    }
}
