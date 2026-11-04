using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Per-IP passwordless request budget unit tests (issue #1090): magic-link and
///     email-code requests share one fixed-window budget per client IP, the limiter is
///     inert while disabled or misconfigured, and store errors fail open because the
///     budget is abuse protection rather than an authorization gate.
/// </summary>
public sealed class DistributedCachePasswordlessRequestIpRateLimiterTests
{
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly PasswordlessRequestIpRateLimitOptions _options = new();

    private DistributedCachePasswordlessRequestIpRateLimiter CreateSut() =>
        new(_cache.Object, Options.Create(_options), NullLogger<DistributedCachePasswordlessRequestIpRateLimiter>.Instance);

    private void GivenStoredCount(int count) =>
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(count.ToString()));

    [Fact]
    public async Task Disabled_Limiter_Always_Admits()
    {
        _options.Enabled = false;

        (await CreateSut().TryAdmitRequestAsync("203.0.113.10")).Should().BeTrue();

        _cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "no budget work may happen while the limiter is disabled");
    }

    [Fact]
    public async Task Requests_Under_The_Budget_Are_Admitted_And_Counted()
    {
        GivenStoredCount(0);
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        (await CreateSut().TryAdmitRequestAsync("203.0.113.10")).Should().BeTrue();

        _cache.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.Is<byte[]>(value => Encoding.UTF8.GetString(value) == "1"),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once, "the admitted request increments the shared per-IP counter");
    }

    [Fact]
    public async Task Requests_At_The_Budget_Are_Throttled()
    {
        _options.MaxRequestsPerWindow = 20;
        GivenStoredCount(20);

        (await CreateSut().TryAdmitRequestAsync("203.0.113.10")).Should().BeFalse("the 21st request within the window is email bombing");

        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never, "a throttled request must not extend the counter");
    }

    [Fact]
    public async Task Budgets_Are_Partitioned_Per_Ip()
    {
        _options.MaxRequestsPerWindow = 20;
        var keys = new List<string>();
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("20"));
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback((string key, byte[] _, DistributedCacheEntryOptions _, CancellationToken _) => keys.Add(key))
            .Returns(Task.CompletedTask);

        // Both sit at their own budget ceiling and are throttled, but the bucket keys
        // recorded for their earlier admitted requests must differ per IP.
        (await CreateSut().TryAdmitRequestAsync("203.0.113.10")).Should().BeFalse();
        (await CreateSut().TryAdmitRequestAsync("198.51.100.23")).Should().BeFalse();

        // Re-admit two fresh addresses so the write path records their bucket keys.
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("0"));
        await CreateSut().TryAdmitRequestAsync("203.0.113.10");
        await CreateSut().TryAdmitRequestAsync("198.51.100.23");

        keys.Should().OnlyHaveUniqueItems("each client IP gets its own fixed-window budget bucket");
    }

    [Fact]
    public async Task Missing_Ip_Is_Admitted_Without_A_Cache_Roundtrip()
    {
        (await CreateSut().TryAdmitRequestAsync(" ")).Should().BeTrue();

        _cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cache_Errors_Fail_Open()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        (await CreateSut().TryAdmitRequestAsync("203.0.113.10")).Should().BeTrue();
    }

    [Fact]
    public async Task Misconfigured_Window_Treats_The_Limiter_As_Inert()
    {
        _options.Window = TimeSpan.Zero; // IsValid() false → admit without counting.

        (await CreateSut().TryAdmitRequestAsync("203.0.113.10")).Should().BeTrue();

        _cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
