using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Infrastructure;

/// <summary>
///     Concurrency tests for the in-memory SAML protocol state store (issue #268
///     acceptance, review item 3): pending-request consumption and assertion
///     reservation are single-use, so exactly one of any number of concurrent
///     consumers may ever observe <c>true</c> — mirroring the atomic SQL of
///     <see cref="PostgreSqlSamlProtocolStateStore" /> across instances.
/// </summary>
public sealed class InMemorySamlProtocolStateStoreTests
{
    private const string Slug = "corp-idp";
    private const string OtherSlug = "partner-idp";

    [Fact]
    public async Task ConsumePendingAuthnRequest_ConcurrentSameRequest_ExactlyOneWinner()
    {
        var store = new InMemorySamlProtocolStateStore();
        await store.SavePendingAuthnRequestAsync(Slug, "_req-1", "relay-1", TimeSpan.FromMinutes(10));

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ =>
            store.ConsumePendingAuthnRequestAsync(Slug, "_req-1", "relay-1")));

        results.Should().HaveCount(16);
        results.Count(won => won).Should().Be(1, "a pending AuthnRequest is single-use even under concurrent consumption");
    }

    [Fact]
    public async Task TryReserveAssertionId_ConcurrentSameAssertion_ExactlyOneWinner()
    {
        var store = new InMemorySamlProtocolStateStore();

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ =>
            store.TryReserveAssertionIdAsync(Slug, "_assert-1", TimeSpan.FromMinutes(5))));

        results.Count(won => won).Should().Be(1, "an assertion ID can be reserved exactly once, however many replays race");
    }

    [Fact]
    public async Task ConsumePendingAuthnRequest_ConcurrentWrongRelayState_NoWinnerAndRequestSurvives()
    {
        var store = new InMemorySamlProtocolStateStore();
        await store.SavePendingAuthnRequestAsync(Slug, "_req-1", "relay-1", TimeSpan.FromMinutes(10));

        var wrongRelay = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            store.ConsumePendingAuthnRequestAsync(Slug, "_req-1", "wrong-relay")));

        wrongRelay.Should().NotContain(true, "a foreign RelayState never consumes the pending request");

        // The legitimate browser can still complete after the racing wrong-relay attempts.
        (await store.ConsumePendingAuthnRequestAsync(Slug, "_req-1", "relay-1")).Should().BeTrue();
    }

    [Fact]
    public async Task PendingRequest_IsolationAcrossProvidersAndExpiry()
    {
        var store = new InMemorySamlProtocolStateStore();
        await store.SavePendingAuthnRequestAsync(Slug, "_req-1", "relay-1", TimeSpan.FromMinutes(10));

        // The same request ID under another provider's slug is a different fact.
        (await store.ConsumePendingAuthnRequestAsync(OtherSlug, "_req-1", "relay-1")).Should().BeFalse();
        (await store.ConsumePendingAuthnRequestAsync(Slug, "_req-1", "relay-1")).Should().BeTrue();

        await store.SavePendingAuthnRequestAsync(Slug, "_req-2", "relay-2", TimeSpan.FromSeconds(-1));
        (await store.ConsumePendingAuthnRequestAsync(Slug, "_req-2", "relay-2")).Should().BeFalse("expired pending requests are not consumable");
    }

    [Fact]
    public async Task TryReserveAssertionId_ExpiredReservation_CanBeTakenOver()
    {
        var store = new InMemorySamlProtocolStateStore();

        (await store.TryReserveAssertionIdAsync(Slug, "_assert-1", TimeSpan.FromSeconds(-1))).Should().BeTrue();
        (await store.TryReserveAssertionIdAsync(Slug, "_assert-1", TimeSpan.FromMinutes(5))).Should()
            .BeTrue("a reservation past its replay window no longer blocks a fresh reservation");
    }
}
