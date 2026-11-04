using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class JwtValidationKeyRefreshConcurrencyTests
{
    [Fact]
    public async Task OverlappingRefreshes_DoNotLetAnOlderQueryEraseTheNewSigningKey()
    {
        var cache = new JwtSigningKeyCache();
        var old = Key(1);
        var fresh = Key(2);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<List<JwtSigningKey>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldStore = new Mock<IKeyRotationService>();
        oldStore.Setup(store => store.GetValidationKeysAsync(It.IsAny<CancellationToken>()))
            .Callback(() => entered.SetResult()).Returns(release.Task);
        var newStore = new Mock<IKeyRotationService>();
        newStore.Setup(store => store.GetValidationKeysAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([old, fresh]);

        var first = cache.RefreshAsync(oldStore.Object);
        await entered.Task;
        var second = cache.RefreshAsync(newStore.Object);
        // Without ordered reads/publication, the second query completes synchronously
        // and its fresh key is erased when this older query is finally allowed to return.
        release.SetResult([old]);
        await Task.WhenAll(first, second);

        var fallback = Fallback();
        Assert.Contains(cache.GetValidationKeys(fallback), key => key.KeyId == old.KeyId);
        Assert.Contains(cache.GetValidationKeys(fallback), key => key.KeyId == fresh.KeyId);
    }

    [Fact]
    public async Task CancelledRefresh_DoesNotPublishLateRowsOrPreventFollowingRefreshes()
    {
        var cache = new JwtSigningKeyCache();
        var old = Key(1);
        var fresh = Key(2);
        var release = new TaskCompletionSource<List<JwtSigningKey>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldStore = new Mock<IKeyRotationService>();
        oldStore.Setup(store => store.GetValidationKeysAsync(It.IsAny<CancellationToken>()))
            .Returns(release.Task);
        var newStore = new Mock<IKeyRotationService>();
        newStore.Setup(store => store.GetValidationKeysAsync(It.IsAny<CancellationToken>())).ReturnsAsync([old, fresh]);
        using var cancellation = new CancellationTokenSource();
        var cancelled = cache.RefreshAsync(oldStore.Object, cancellation.Token);
        await cache.RefreshAsync(newStore.Object);
        cancellation.Cancel();
        // Simulate a store that completes despite cancellation: the caller must still
        // observe cancellation, and those late rows must never be published.
        release.SetResult([old]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        Assert.Contains(cache.GetValidationKeys(Fallback()), key => key.KeyId == fresh.KeyId);
        await cache.RefreshAsync(newStore.Object);
        Assert.Contains(cache.GetValidationKeys(Fallback()), key => key.KeyId == fresh.KeyId);
    }

    private static JwtSigningKey Key(int version)
        => JwtSigningKey.CreateNew(version, DateTime.UtcNow.AddMinutes(-1), TimeSpan.FromHours(1));

    private static SecurityKey Fallback()
        => new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "fallback" };
}
