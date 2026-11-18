using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
///     Distributed WebAuthn challenge store tests (issue #234): user-keyed ceremony storage,
///     challenge-keyed guest assertion sessions (issue #1198), JSON round-trips of the Fido2
///     options and TTL expiry against the domain clock.
/// </summary>
public sealed class DistributedCacheWebAuthnSessionStoreTests
{
    private readonly IServiceProvider _services = new ServiceCollection().AddDistributedMemoryCache().BuildServiceProvider();

    private IDistributedCache Cache => _services.GetRequiredService<IDistributedCache>();

    private DistributedCacheWebAuthnSessionStore CreateStore() =>
        new(Cache, NullLogger<DistributedCacheWebAuthnSessionStore>.Instance);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static Fido2NetLib.CredentialCreateOptions RegistrationOptions(Guid userId, byte challenge) =>
        Fido2NetLib.CredentialCreateOptions.FromJson($$"""
            {
                "challenge": "{{Base64Url([challenge])}}",
                "rp": { "id": "localhost", "name": "GameGuild" },
                "user": { "id": "{{Base64Url(userId.ToByteArray())}}", "name": "user@example.com", "displayName": "User" },
                "pubKeyCredParams": [ { "type": "public-key", "alg": -7 } ],
                "timeout": 60000,
                "attestation": "none"
            }
            """);

    private static Fido2NetLib.AssertionOptions AssertionOptions(byte challenge) =>
        Fido2NetLib.AssertionOptions.FromJson($$"""
            {
                "challenge": "{{Base64Url([challenge])}}",
                "timeout": 30000,
                "allowCredentials": [],
                "userVerification": "preferred"
            }
            """);

    [Fact]
    public async Task RegistrationSession_RoundTrips_Options_And_Challenge()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();

        await store.StoreRegistrationSessionAsync(new WebAuthnSession
        {
            UserId = userId,
            Challenge = [1, 2, 3, 4],
            RegistrationOptions = RegistrationOptions(userId, 4)
        });

        var found = await store.FindRegistrationSessionAsync(userId);
        found.Should().NotBeNull();
        found!.UserId.Should().Be(userId);
        found.Challenge.Should().Equal(1, 2, 3, 4);
        found.RegistrationOptions.Should().NotBeNull();
        found.RegistrationOptions!.User.Name.Should().Be("user@example.com");
        found.RegistrationOptions.Challenge.Should().Equal(4);

        await store.RemoveRegistrationSessionAsync(userId);
        (await store.FindRegistrationSessionAsync(userId)).Should().BeNull();
    }

    [Fact]
    public async Task AssertionSession_IsIsolatedPerUser_AndGuestCeremoniesAreIsolatedPerChallenge()
    {
        var store = CreateStore();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = user1, Challenge = [11], AssertionOptions = AssertionOptions(11) });
        await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = user2, Challenge = [22], AssertionOptions = AssertionOptions(22) });
        await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = null, Challenge = [33], AssertionOptions = AssertionOptions(33) });

        (await store.FindAssertionSessionAsync(user1))!.AssertionOptions!.Challenge.Should().Equal(11);
        (await store.FindAssertionSessionAsync(user2))!.AssertionOptions!.Challenge.Should().Equal(22);
        (await store.FindGuestAssertionSessionAsync(Base64Url([33])))!.AssertionOptions!.Challenge.Should().Equal(33);

        // Per-user removal leaves the others intact (replaces the process-local FindFirst scan).
        await store.RemoveAssertionSessionAsync(user1);
        (await store.FindAssertionSessionAsync(user1)).Should().BeNull();
        (await store.FindAssertionSessionAsync(user2)).Should().NotBeNull();
        (await store.FindGuestAssertionSessionAsync(Base64Url([33]))).Should().NotBeNull();
    }

    [Fact]
    public async Task GuestCeremonies_ConcurrentSessionsDoNotOverwriteEachOther()
    {
        // Issue #1198: two anonymous ceremonies (second tab / second device) must coexist —
        // the old single shared guest slot let the second begin invalidate the first.
        var store = CreateStore();

        var firstSessionId = await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = null, Challenge = [1], AssertionOptions = AssertionOptions(1) });
        var secondSessionId = await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = null, Challenge = [2], AssertionOptions = AssertionOptions(2) });

        firstSessionId.Should().NotBe(secondSessionId);

        var first = await store.FindGuestAssertionSessionAsync(Base64Url([1]));
        var second = await store.FindGuestAssertionSessionAsync(Base64Url([2]));
        first!.AssertionOptions!.Challenge.Should().Equal(1);
        second!.AssertionOptions!.Challenge.Should().Equal(2);

        // Completing the first ceremony leaves the second fully usable.
        await store.RemoveGuestAssertionSessionAsync(Base64Url([1]));
        (await store.FindGuestAssertionSessionAsync(Base64Url([1]))).Should().BeNull();
        (await store.FindGuestAssertionSessionAsync(Base64Url([2])))!.AssertionOptions!.Challenge.Should().Equal(2);
    }

    [Fact]
    public async Task ReissuedAssertionSession_ReplacesThePreviousOneForThatUser()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();

        await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = userId, Challenge = [1], AssertionOptions = AssertionOptions(1) });
        await store.StoreAssertionSessionAsync(new WebAuthnSession { UserId = userId, Challenge = [2], AssertionOptions = AssertionOptions(2) });

        var found = await store.FindAssertionSessionAsync(userId);
        found!.AssertionOptions!.Challenge.Should().Equal(2);
    }

    [Fact]
    public async Task ExpiredSession_FailsClosed_EvenWhenCacheEntryStillExists()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();

        await store.StoreRegistrationSessionAsync(new WebAuthnSession
        {
            UserId = userId,
            Challenge = [9],
            CreatedAt = DateTime.UtcNow.Subtract(DistributedCacheWebAuthnSessionStore.SessionLifetime.Add(TimeSpan.FromMinutes(1))),
            RegistrationOptions = RegistrationOptions(userId, 9)
        });

        (await store.FindRegistrationSessionAsync(userId)).Should().BeNull();
    }

    [Fact]
    public async Task RegistrationSessionWithoutUser_IsRejected()
    {
        var store = CreateStore();
        var act = () => store.StoreRegistrationSessionAsync(new WebAuthnSession
        {
            UserId = null,
            Challenge = [1],
            RegistrationOptions = RegistrationOptions(Guid.NewGuid(), 1)
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SessionWithoutOptions_IsRejected()
    {
        var store = CreateStore();
        var act = () => store.StoreRegistrationSessionAsync(new WebAuthnSession { UserId = Guid.NewGuid(), Challenge = [1] });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CorruptPayload_IsTreatedAsExpired()
    {
        var cache = Cache;
        var userId = Guid.NewGuid();
        await cache.SetStringAsync("auth:webauthn:session:reg:" + userId.ToString("N"), "not-json{{");

        var store = CreateStore();
        (await store.FindRegistrationSessionAsync(userId)).Should().BeNull();
    }
}
