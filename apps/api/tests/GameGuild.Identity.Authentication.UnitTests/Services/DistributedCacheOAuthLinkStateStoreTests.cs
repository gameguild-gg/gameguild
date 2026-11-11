using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Distributed OAuth link-state store tests (issue #1181): save/round-trip-validate
///     semantics, one-shot consumption, per-(user, provider) isolation and case-insensitive
///     provider matching.
/// </summary>
public sealed class DistributedCacheOAuthLinkStateStoreTests
{
    private readonly IServiceProvider _services = new ServiceCollection().AddDistributedMemoryCache().BuildServiceProvider();

    private DistributedCacheOAuthLinkStateStore CreateStore() =>
        new(_services.GetRequiredService<IDistributedCache>());

    [Fact]
    public async Task SaveThenValidate_SameState_SucceedsAndConsumes()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();

        await store.SaveAsync(userId, "discord", "state-abc");
        (await store.ValidateAndConsumeAsync(userId, "discord", "state-abc")).Should().BeTrue();

        // One-shot: the consumed state cannot link a second time.
        (await store.ValidateAndConsumeAsync(userId, "discord", "state-abc")).Should().BeFalse();
    }

    [Fact]
    public async Task Validate_UnknownState_Fails()
    {
        var store = CreateStore();

        (await store.ValidateAndConsumeAsync(Guid.NewGuid(), "github", "never-issued")).Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WrongUserOrProvider_Fails()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        await store.SaveAsync(userId, "github", "state-1");

        (await store.ValidateAndConsumeAsync(Guid.NewGuid(), "github", "state-1")).Should().BeFalse("state is bound to the issued user");
        (await store.ValidateAndConsumeAsync(userId, "microsoft", "state-1")).Should().BeFalse("state is bound to the issued provider");
    }

    [Fact]
    public async Task Validate_ProviderMatchingIsCaseInsensitive()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        await store.SaveAsync(userId, "GitHub", "state-2");

        (await store.ValidateAndConsumeAsync(userId, "github", "state-2")).Should().BeTrue();
    }

    [Fact]
    public async Task ReissuedState_ReplacesThePendingOne()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();

        await store.SaveAsync(userId, "discord", "first");
        await store.SaveAsync(userId, "discord", "second");

        (await store.ValidateAndConsumeAsync(userId, "discord", "first")).Should().BeFalse("the superseded state is gone");
        (await store.ValidateAndConsumeAsync(userId, "discord", "second")).Should().BeTrue();
    }

    [Fact]
    public async Task Save_NullOrEmptyState_IsRejected()
    {
        var store = CreateStore();
        var act = () => store.SaveAsync(Guid.NewGuid(), "discord", "");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Validate_EmptyState_FailsWithoutThrowing()
    {
        var store = CreateStore();

        (await store.ValidateAndConsumeAsync(Guid.NewGuid(), "discord", "")).Should().BeFalse();
        (await store.ValidateAndConsumeAsync(Guid.NewGuid(), "discord", null!)).Should().BeFalse();
    }
}
