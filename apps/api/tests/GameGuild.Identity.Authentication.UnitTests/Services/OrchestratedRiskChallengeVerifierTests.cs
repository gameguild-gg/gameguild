using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class OrchestratedRiskChallengeVerifierTests
{
    [Fact]
    public async Task DeliveryUsesVerifiedAddressAndExactExpiryWithoutPersistingPlaintextCode()
    {
        var fixture = new Fixture();
        var challenge = await fixture.Create();
        var delivery = Assert.Single(fixture.Deliveries);
        Assert.Equal(fixture.User.Id, delivery.UserId);
        Assert.Equal(fixture.User.Email, delivery.Email);
        Assert.Equal(8, delivery.Code.Length);
        Assert.All(delivery.Code, character => Assert.InRange(character, '0', '9'));
        Assert.Equal(fixture.Now.AddMinutes(5), delivery.ExpiresAt);
        Assert.Equal(300, delivery.ExpiresInSeconds);
        Assert.DoesNotContain(delivery.Code, JsonSerializer.Serialize(challenge), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PersistedStateWorksAcrossInstancesAndSuccessfulCodeIsConsumedOnce()
    {
        var fixture = new Fixture();
        var state = RoundTrip(await fixture.Create());
        var otherInstance = new OrchestratedRiskChallengeVerifier(fixture.Keyring, fixture.Publisher.Object);
        var result = otherInstance.Verify(state, fixture.Flow, fixture.User.Id, fixture.Tenant, fixture.Code, fixture.Now.AddSeconds(1));
        Assert.True(result.Accepted);
        Assert.Equal(fixture.Now.AddSeconds(1), result.State.VerifiedAt);
        Assert.Empty(result.State.ProtectedDigest);
        Assert.False(otherInstance.Verify(RoundTrip(result.State), fixture.Flow, fixture.User.Id, fixture.Tenant, fixture.Code, fixture.Now.AddSeconds(2)).Accepted);
    }

    [Theory]
    [InlineData("flow")]
    [InlineData("subject")]
    [InlineData("tenant")]
    [InlineData("empty-flow")]
    [InlineData("exact-expiry")]
    [InlineData("consumed")]
    [InlineData("exhausted")]
    [InlineData("damaged")]
    [InlineData("other-keyring")]
    [InlineData("malformed-code")]
    public async Task MismatchedExpiredConsumedOrMalformedProofCannotVerify(string fault)
    {
        var fixture = new Fixture();
        var state = await fixture.Create();
        var flow = fixture.Flow; var subject = fixture.User.Id; var tenant = fixture.Tenant;
        var now = fixture.Now.AddSeconds(1); var code = fixture.Code; var verifier = fixture.Verifier;
        switch (fault)
        {
            case "flow": flow = Guid.NewGuid(); break;
            case "subject": subject = Guid.NewGuid(); break;
            case "tenant": tenant = Guid.NewGuid(); break;
            case "empty-flow": flow = Guid.Empty; break;
            case "exact-expiry": now = state.ExpiresAt; break;
            case "consumed": state = state with { VerifiedAt = fixture.Now }; break;
            case "exhausted": state = state with { FailedAttempts = 5 }; break;
            case "damaged": state = state with { ProtectedDigest = "invalid-protected-value" }; break;
            case "other-keyring": verifier = new OrchestratedRiskChallengeVerifier(new EphemeralDataProtectionProvider(), fixture.Publisher.Object); break;
            case "malformed-code": code = "1234x678"; break;
            default: throw new ArgumentOutOfRangeException(nameof(fault));
        }
        Assert.False(verifier.Verify(state, flow, subject, tenant, code, now).Accepted);
    }

    [Fact]
    public async Task FivePersistedFailuresBlockEvenTheCorrectCode()
    {
        var fixture = new Fixture();
        var state = await fixture.Create();
        var wrong = fixture.Code == "00000000" ? "11111111" : "00000000";
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var result = fixture.Verifier.Verify(state, fixture.Flow, fixture.User.Id, fixture.Tenant, wrong, fixture.Now.AddSeconds(attempt));
            Assert.False(result.Accepted);
            Assert.Equal(attempt, result.State.FailedAttempts);
            state = RoundTrip(result.State);
        }
        Assert.False(fixture.Verifier.Verify(state, fixture.Flow, fixture.User.Id, fixture.Tenant, fixture.Code, fixture.Now.AddSeconds(6)).Accepted);
    }

    [Fact]
    public async Task MalformedCodeConsumesAnAttempt()
    {
        var fixture = new Fixture();
        var result = fixture.Verifier.Verify(await fixture.Create(), fixture.Flow, fixture.User.Id, fixture.Tenant, "1234x678", fixture.Now);
        Assert.False(result.Accepted);
        Assert.Equal(1, result.State.FailedAttempts);
    }

    [Theory]
    [InlineData("unverified")]
    [InlineData("missing-address")]
    public async Task UntrustedEmailDestinationNeverQueuesCode(string fault)
    {
        var fixture = new Fixture();
        if (fault == "unverified") { fixture.User.IsEmailVerified = false; }
        else { fixture.User.Email = string.Empty; }
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => fixture.Create());
        Assert.Empty(fixture.Deliveries);
    }

    [Fact]
    public async Task CancellationBeforeCreationDoesNotQueueCode()
    {
        var fixture = new Fixture();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Create(cancelled.Token));
        Assert.Empty(fixture.Deliveries);
    }

    [Fact]
    public async Task QueueFailureDoesNotAcknowledgeALiveChallenge()
    {
        var fixture = new Fixture();
        fixture.Publisher.Setup(port => port.Publish(It.IsAny<OrchestratedRiskCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Synthetic queue failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Create());
        Assert.Empty(fixture.Deliveries);
    }

    private static OrchestratedRiskChallengeState RoundTrip(OrchestratedRiskChallengeState state) =>
        JsonSerializer.Deserialize<OrchestratedRiskChallengeState>(JsonSerializer.Serialize(state))!;

    private sealed class Fixture
    {
        public Guid Flow { get; } = Guid.NewGuid();
        public Guid Tenant { get; } = Guid.NewGuid();
        public DateTimeOffset Now { get; } = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        public User User { get; } = new() { Id = Guid.NewGuid(), Email = "synthetic@example.invalid", IsActive = true, IsEmailVerified = true };
        public IDataProtectionProvider Keyring { get; } = new EphemeralDataProtectionProvider();
        public Mock<IPublisher> Publisher { get; } = new();
        public List<OrchestratedRiskCodeRequestedNotification> Deliveries { get; } = [];
        public string Code => Assert.Single(Deliveries).Code;
        public OrchestratedRiskChallengeVerifier Verifier { get; }
        public Fixture()
        {
            Publisher.Setup(port => port.Publish(It.IsAny<OrchestratedRiskCodeRequestedNotification>(), It.IsAny<CancellationToken>()))
                .Callback((OrchestratedRiskCodeRequestedNotification delivery, CancellationToken _) => Deliveries.Add(delivery))
                .Returns(Task.CompletedTask);
            Verifier = new OrchestratedRiskChallengeVerifier(Keyring, Publisher.Object);
        }
        public Task<OrchestratedRiskChallengeState> Create() => Create(CancellationToken.None);
        public Task<OrchestratedRiskChallengeState> Create(CancellationToken cancellationToken) =>
            Verifier.CreateAsync(Flow, User, Tenant, Now, Now.AddMinutes(5), cancellationToken);
    }
}
