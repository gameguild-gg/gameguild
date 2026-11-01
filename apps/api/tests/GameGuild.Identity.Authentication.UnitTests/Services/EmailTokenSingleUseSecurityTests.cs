using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using GameGuild.CQRS;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class EmailTokenSingleUseSecurityTests
{
    private const string Email = "token-owner@example.test";
    private const string TokenPrefix = "emailverify:token:";

    [Theory]
    [InlineData("verification")]
    [InlineData("reset")]
    public async Task GeneratedDistributedTokens_StoreOnlyTheirDigestAsTheCacheKey(string kind)
    {
        var cache = new RecordingKeyCache();
        var token = await Generate(CreateService(cache), kind, Guid.NewGuid());
        var key = Assert.Single(cache.Keys);

        Assert.DoesNotContain(token, key, StringComparison.Ordinal);
        Assert.Equal(TokenPrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), key);
    }

    [Fact]
    public async Task GeneratedMagicLink_UsesDurableStoreWithoutPopulatingTheTokenCache()
    {
        var cache = new RecordingKeyCache();
        var store = new Mock<IMagicLinkTokenStore>();
        var userId = Guid.NewGuid();
        var started = DateTimeOffset.UtcNow;
        store.Setup(value => value.AddAsync(It.IsAny<string>(), userId, Email,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var token = await CreateService(cache, store.Object).GenerateMagicLinkTokenAsync(userId, Email);

        Assert.Empty(cache.Keys);
        store.Verify(value => value.AddAsync(token, userId, Email,
            It.Is<DateTimeOffset>(expiry => expiry >= started.AddMinutes(14) && expiry <= DateTimeOffset.UtcNow.AddMinutes(16)),
            CancellationToken.None), Times.Once);
    }

    [Theory]
    [InlineData("verification", false)]
    [InlineData("verification", true)]
    [InlineData("verification-with-user", false)]
    [InlineData("verification-with-user", true)]
    [InlineData("reset", false)]
    [InlineData("reset", true)]
    [InlineData("magic", false)]
    [InlineData("magic", true)]
    public async Task ConcurrentConsumersAcrossServiceScopes_AllowExactlyOneSuccess(string kind, bool separateServices)
    {
        using var cache = new SynchronizedReadCache();
        var userId = Guid.NewGuid();
        var magicStore = new EmailVerificationServiceTests.InMemoryMagicLinkTokenStore();
        var service = CreateService(cache, magicStore);
        var token = await Generate(service, kind, userId);
        cache.SynchronizeNextTokenReads();
        var consumers = Enumerable.Range(0, 4).Select(_ => Task.Factory.StartNew(
            () => Consume(separateServices ? CreateService(cache, magicStore) : service, kind, userId, token),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap()).ToArray();

        var results = await Task.WhenAll(consumers);

        Assert.False(cache.SynchronizationFailed);
        var result = Assert.Single(results, value => value.Success);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(Email, result.Email);
        Assert.False(await service.IsTokenValidAsync(token));
    }

    [Theory]
    [InlineData("verification")]
    [InlineData("reset")]
    public async Task ClaimedDistributedToken_IsAlreadyInvalidWhileCacheRemovalIsPending(string kind)
    {
        using var cache = new SuspendedRemovalCache();
        var userId = Guid.NewGuid();
        var service = CreateService(cache);
        var token = await Generate(service, kind, userId);
        cache.SuspendRemoval = true;
        var consumption = Task.Factory.StartNew(() => Consume(service, kind, userId, token),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        bool valid;
        try
        {
            Assert.True(cache.RemovalStarted.Wait(TimeSpan.FromSeconds(10)));
            valid = await CreateService(cache).IsTokenValidAsync(token);
        }
        finally
        {
            cache.AllowRemoval.Set();
        }
        Assert.True((await consumption).Success);
        Assert.False(valid);
    }

    [Theory]
    [InlineData("verification")]
    [InlineData("reset")]
    [InlineData("magic")]
    public async Task SequentialConsumption_PreservesIdentityAndRejectsReplay(string kind)
    {
        var service = CreateService(EmailVerificationServiceTests.CreateCache());
        var userId = Guid.NewGuid();
        var token = await Generate(service, kind, userId);

        Assert.True(await service.IsTokenValidAsync(token));
        var consumed = await Consume(service, kind, userId, token);
        Assert.True(consumed.Success);
        Assert.Equal(userId, consumed.UserId);
        Assert.Equal(Email, consumed.Email);
        Assert.False((await Consume(service, kind, userId, token)).Success);
        Assert.False(await service.IsTokenValidAsync(token));
    }

    [Fact]
    public async Task WrongPurpose_DoesNotConsumeTheValidToken()
    {
        var service = CreateService(EmailVerificationServiceTests.CreateCache());
        var token = await service.GenerateMagicLinkTokenAsync(Guid.NewGuid(), Email);

        Assert.False((await service.VerifyPasswordResetTokenAsync(token)).Success);
        Assert.True(await service.IsTokenValidAsync(token));
        Assert.True((await service.VerifyMagicLinkTokenAsync(token)).Success);
    }

    [Fact]
    public async Task WrongUser_DoesNotConsumeTheValidToken()
    {
        var service = CreateService(EmailVerificationServiceTests.CreateCache());
        var userId = Guid.NewGuid();
        var token = await service.GenerateVerificationTokenAsync(userId, Email);

        Assert.False(await service.VerifyEmailTokenAsync(Guid.NewGuid(), token));
        Assert.True(await service.IsTokenValidAsync(token));
        Assert.True(await service.VerifyEmailTokenAsync(userId, token));
    }

    private static EmailVerificationService CreateService(IDistributedCache cache, IMagicLinkTokenStore? magicStore = null) =>
        new(NullLogger<EmailVerificationService>.Instance, cache, Mock.Of<IPublisher>(),
            magicLinkTokenStore: magicStore ?? new EmailVerificationServiceTests.InMemoryMagicLinkTokenStore());

    private static Task<string> Generate(EmailVerificationService service, string kind, Guid userId) => kind switch
    {
        "verification" or "verification-with-user" => service.GenerateVerificationTokenAsync(userId, Email),
        "reset" => service.GeneratePasswordResetTokenAsync(userId, Email),
        "magic" => service.GenerateMagicLinkTokenAsync(userId, Email),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static async Task<TokenValidationResult> Consume(EmailVerificationService service, string kind, Guid userId, string token) => kind switch
    {
        "verification" => await service.VerifyEmailTokenAsync(token),
        "verification-with-user" => new(await service.VerifyEmailTokenAsync(userId, token), userId, Email),
        "reset" => await service.VerifyPasswordResetTokenAsync(token),
        "magic" => await service.VerifyMagicLinkTokenAsync(token),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    /// <summary>
    ///     Wraps the real distributed-memory cache and records every key touched, standing in for
    ///     the key enumeration the distributed abstraction does not expose.
    /// </summary>
    private sealed class RecordingKeyCache : IDistributedCache
    {
        private readonly Microsoft.Extensions.Caching.Memory.DistributedMemoryCache _inner =
            new(Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.DistributedCacheOptions()));
        private readonly ConcurrentDictionary<string, byte> _keys = new();

        public IReadOnlyCollection<string> Keys => _keys.Keys.ToArray();

        public byte[]? Get(string key)
        {
            _keys[key] = 1;
            return _inner.Get(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            _keys[key] = 1;
            return _inner.GetAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _keys[key] = 1;
            _inner.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            _keys[key] = 1;
            return _inner.SetAsync(key, value, options, token);
        }

        public void Refresh(string key)
        {
            _keys[key] = 1;
            _inner.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            _keys[key] = 1;
            return _inner.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            _keys[key] = 1;
            _inner.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            _keys[key] = 1;
            return _inner.RemoveAsync(key, token);
        }
    }

    private sealed class SynchronizedReadCache : IDistributedCache, IDisposable
    {
        private readonly Microsoft.Extensions.Caching.Memory.DistributedMemoryCache _inner =
            new(Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.DistributedCacheOptions()));
        private readonly Barrier _barrier = new(4);
        private int _readCount;
        private int _synchronizationFailed;
        private bool _synchronize;
        public bool SynchronizationFailed => Volatile.Read(ref _synchronizationFailed) != 0;
        public void SynchronizeNextTokenReads() => _synchronize = true;

        public byte[]? Get(string key)
        {
            Synchronize(key);
            return _inner.Get(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            Synchronize(key);
            return _inner.GetAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _inner.Set(key, value, options);
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => _inner.SetAsync(key, value, options, token);
        public void Refresh(string key) => _inner.Refresh(key);
        public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);
        public void Remove(string key) => _inner.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);

        private void Synchronize(string key)
        {
            if (!_synchronize || key is not { Length: > 0 } || !key.StartsWith(TokenPrefix, StringComparison.Ordinal))
            {
                return;
            }

            if (Interlocked.Increment(ref _readCount) <= 4 && !_barrier.SignalAndWait(TimeSpan.FromSeconds(10)))
            {
                Interlocked.Exchange(ref _synchronizationFailed, 1);
            }
        }

        public void Dispose()
        {
            _barrier.Dispose();
        }
    }

    private sealed class SuspendedRemovalCache : IDistributedCache, IDisposable
    {
        private readonly Microsoft.Extensions.Caching.Memory.DistributedMemoryCache _inner =
            new(Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.DistributedCacheOptions()));
        public ManualResetEventSlim RemovalStarted { get; } = new();
        public ManualResetEventSlim AllowRemoval { get; } = new();
        public bool SuspendRemoval { get; set; }

        public byte[]? Get(string key) => _inner.Get(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => _inner.GetAsync(key, token);
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _inner.Set(key, value, options);
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => _inner.SetAsync(key, value, options, token);
        public void Refresh(string key) => _inner.Refresh(key);
        public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);

        public void Remove(string key)
        {
            if (ShouldSuspend(key))
            {
                RemovalStarted.Set();
                if (!AllowRemoval.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The test did not release suspended token removal.");
                }
            }

            _inner.Remove(key);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (ShouldSuspend(key))
            {
                RemovalStarted.Set();
                var released = await Task.Run(() => AllowRemoval.Wait(TimeSpan.FromSeconds(10)), token).ConfigureAwait(false);
                if (!released)
                {
                    throw new TimeoutException("The test did not release suspended token removal.");
                }
            }

            await _inner.RemoveAsync(key, token).ConfigureAwait(false);
        }

        private bool ShouldSuspend(string key) =>
            SuspendRemoval && key.StartsWith(TokenPrefix, StringComparison.Ordinal);

        public void Dispose()
        {
            AllowRemoval.Set();
            AllowRemoval.Dispose();
            RemovalStarted.Dispose();
        }
    }
}
