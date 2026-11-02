using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     IP blocklist unit tests (issue #1089) over a mocked <see cref="IDistributedCache"/>:
///     entries resolve on the hot lookup path until their own expiry, the registry document
///     backs listing and removal, and lookups fail open on cache errors because the
///     blocklist is defense-in-depth for sign-in availability.
/// </summary>
public sealed class DistributedCacheIpBlocklistServiceTests
{
    private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);

    private readonly Mock<IDistributedCache> _cache = new();

    private DistributedCacheIpBlocklistService CreateSut() =>
        new(_cache.Object, NullLogger<DistributedCacheIpBlocklistService>.Instance);

    private static IpBlocklistEntry Entry(string ip, DateTimeOffset? expiresAt = null) => new()
    {
        IpAddress = ip,
        Reason = "known attacker",
        AddedBy = "admin@example.test",
        AddedAt = DateTimeOffset.UtcNow.AddDays(-1),
        ExpiresAt = expiresAt
    };

    private void GivenStoredEntry(IpBlocklistEntry entry) =>
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken _) =>
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, SerializationOptions)));

    [Fact]
    public async Task Empty_Cache_Means_Not_Blocklisted()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var found = await CreateSut().FindEntryAsync("203.0.113.10");

        found.Should().BeNull();
    }

    [Fact]
    public async Task Stored_Entry_Is_Found_Until_Its_Own_Expiry()
    {
        var entry = Entry("203.0.113.10", DateTimeOffset.UtcNow.AddHours(1));
        GivenStoredEntry(entry);

        (await CreateSut().FindEntryAsync("203.0.113.10")).Should().BeEquivalentTo(entry);

        GivenStoredEntry(Entry("203.0.113.10", DateTimeOffset.UtcNow.AddMinutes(-1)));
        (await CreateSut().FindEntryAsync("203.0.113.10")).Should().BeNull("expired entries stop matching");
    }

    [Fact]
    public async Task Blank_Ip_Is_Never_Blocklisted_Without_A_Cache_Roundtrip()
    {
        var found = await CreateSut().FindEntryAsync("   ");

        found.Should().BeNull();
        _cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cache_Errors_Fail_Open()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        var found = await CreateSut().FindEntryAsync("203.0.113.10");

        found.Should().BeNull("an unavailable store must never lock every user out");
    }

    [Fact]
    public async Task AddOrUpdate_Persists_Entry_And_Registry()
    {
        var written = new List<string>();
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback((string key, byte[] value, DistributedCacheEntryOptions _, CancellationToken _) => written.Add(key))
            .Returns(Task.CompletedTask);

        await CreateSut().AddOrUpdateEntryAsync(Entry(" 203.0.113.10 "));

        written.Should().HaveCount(2, "one registry document plus one hot-path entry key");
    }

    [Fact]
    public async Task Remove_Returns_True_Only_While_The_Entry_Exists()
    {
        var registryJson = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new List<IpBlocklistEntry> { Entry("203.0.113.10") }, SerializationOptions));
        var current = registryJson;
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => current);
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback((string _, byte[] value, DistributedCacheEntryOptions _, CancellationToken _) => current = value)
            .Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateSut();
        (await service.RemoveEntryAsync("203.0.113.10")).Should().BeTrue();
        (await service.RemoveEntryAsync("203.0.113.10")).Should().BeFalse("the second removal finds an empty registry");
    }

    [Fact]
    public async Task ListEntries_Filters_Out_Expired_Entries()
    {
        var registry = new List<IpBlocklistEntry>
        {
            Entry("203.0.113.10", DateTimeOffset.UtcNow.AddHours(1)),
            Entry("198.51.100.23", DateTimeOffset.UtcNow.AddMinutes(-1))
        };
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(registry, SerializationOptions)));

        var listed = await CreateSut().ListEntriesAsync();

        listed.Should().ContainSingle(entry => entry.IpAddress == "203.0.113.10");
    }
}
