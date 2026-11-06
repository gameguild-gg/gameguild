using System.Security.Cryptography;
using System.Text;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class MfaRecoveryTransitionSecurityTests
{
    [Theory]
    [InlineData(ActorKind.Anonymous, true, false)]
    [InlineData(ActorKind.Service, true, false)]
    [InlineData(ActorKind.System, true, false)]
    [InlineData(ActorKind.Webhook, true, false)]
    [InlineData(ActorKind.External, true, false)]
    [InlineData(ActorKind.User, false, false)]
    [InlineData(ActorKind.User, true, true)]
    public async Task CancellationRejectsNonUserOrUnauthenticatedOrInvalidSubjectBeforeStoreAccess(
        ActorKind kind, bool authenticated, bool invalidSubject)
    {
        var actors = Actor(ActorContext.Anonymous with { ActorKind = kind, IsAuthenticated = authenticated,
            SubjectId = invalidSubject ? "invalid-subject" : Guid.NewGuid().ToString() });
        var store = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
        var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
        var handler = new CancelMfaRecoveryCommandHandler(actors.Object, store.Object, audit.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CancelMfaRecoveryCommand(), CancellationToken.None));
        store.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancellationRejectsEmptyUserIdentifierBeforeStoreAccess()
    {
        var actors = Actor(UserActor(Guid.Empty));
        var store = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
        var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
        var handler = new CancelMfaRecoveryCommandHandler(actors.Object, store.Object, audit.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CancelMfaRecoveryCommand(), CancellationToken.None));
        store.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancellationUsesOnlyTheActorAndRequiresTheCancelledTransitionAudit()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actors = Actor(UserActor(userId));
        var row = new MfaRecoveryRequest { Id = Guid.NewGuid(), UserId = userId.ToString(), TenantId = tenantId.ToString(),
            Status = MfaRecoveryRequestStatus.Cancelled };
        var store = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
        store.Setup(value => value.CancelLiveByUserAsync(userId.ToString(), CancellationToken.None)).ReturnsAsync(row);
        AuthenticationAuditEvent? captured = null;
        var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
        audit.Setup(value => value.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), CancellationToken.None))
            .Callback<AuthenticationAuditEvent, CancellationToken>((value, _) => captured = value).Returns(Task.CompletedTask);
        var handler = new CancelMfaRecoveryCommandHandler(actors.Object, store.Object, audit.Object);
        var result = await handler.Handle(new CancelMfaRecoveryCommand(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.True(result.Cancelled);
        Assert.NotNull(captured);
        Assert.Equal("Authentication.MfaRecoveryCancelled", captured.ActionType);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(tenantId, captured.TenantId);
        Assert.True(captured.Success);
        store.Verify(value => value.CancelLiveByUserAsync(userId.ToString(), CancellationToken.None), Times.Once);
        audit.Verify(value => value.RecordAsync(It.IsAny<AuthenticationAuditEvent>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CancellationWithoutAnActiveRequestIsIdempotentAndDoesNotInventATransitionAudit()
    {
        var userId = Guid.NewGuid();
        var store = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
        store.Setup(value => value.CancelLiveByUserAsync(userId.ToString(), CancellationToken.None)).ReturnsAsync((MfaRecoveryRequest?)null);
        var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
        var handler = new CancelMfaRecoveryCommandHandler(Actor(UserActor(userId)).Object, store.Object, audit.Object);
        var result = await handler.Handle(new CancelMfaRecoveryCommand(), CancellationToken.None);
        Assert.True(result.Success);
        Assert.False(result.Cancelled);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConfirmationLosingTheAtomicTransitionCannotDisableMfaOrRecordSuccess()
    {
        var token = RandomBearer();
        var digest = Hash(token);
        var row = Row(digest, confirmation: true);
        var store = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
        store.Setup(value => value.FindAwaitingByConfirmationTokenHashAsync(digest, CancellationToken.None)).ReturnsAsync(row);
        store.Setup(value => value.TryConfirmAsync(row.Id, digest, It.IsAny<DateTimeOffset>(), CancellationToken.None)).ReturnsAsync(false);
        var tracking = new Mock<IMfaAttemptTrackingService>(MockBehavior.Strict);
        var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
        var handler = new ConfirmMfaRecoveryCommandHandler(store.Object, tracking.Object,
            NullLogger<ConfirmMfaRecoveryCommandHandler>.Instance, audit.Object);
        var result = await handler.Handle(new ConfirmMfaRecoveryCommand { ConfirmationToken = token }, CancellationToken.None);
        Assert.False(result.Success);
        tracking.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
        store.Verify(value => value.TryConfirmAsync(row.Id, digest, It.IsAny<DateTimeOffset>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ContinuationLosingTheAtomicTransitionCannotQueueAnotherBearerOrRecordSuccess()
    {
        var token = RandomBearer();
        var digest = Hash(token);
        var row = Row(digest, confirmation: false);
        var store = new Mock<IMfaRecoveryRequestStore>(MockBehavior.Strict);
        store.Setup(value => value.ExpireOlderThanAsync(It.IsAny<DateTimeOffset>(), CancellationToken.None)).ReturnsAsync(0);
        store.Setup(value => value.FindLiveByRequestTokenHashAsync(digest, CancellationToken.None)).ReturnsAsync(row);
        store.Setup(value => value.TryIssueConfirmationAsync(row.Id, digest, It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset>(), CancellationToken.None)).ReturnsAsync(false);
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync(Guid.Parse(row.UserId), CancellationToken.None))
            .ReturnsAsync(new User { Id = Guid.Parse(row.UserId), Email = "owned-recovery@example.test", Username = "owned-recovery" });
        var notifications = new Mock<INotificationService>(MockBehavior.Strict);
        var audit = new Mock<IAuthenticationAuditEventSink>(MockBehavior.Strict);
        var handler = new ContinueMfaRecoveryCommandHandler(store.Object, users.Object, notifications.Object,
            NullLogger<ContinueMfaRecoveryCommandHandler>.Instance, audit.Object);
        var result = await handler.Handle(new ContinueMfaRecoveryCommand { RequestToken = token }, CancellationToken.None);
        Assert.False(result.Success);
        notifications.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
    }

    private static MfaRecoveryRequest Row(string digest, bool confirmation) => new()
    {
        Id = Guid.NewGuid(), UserId = Guid.NewGuid().ToString(), TenantId = Guid.NewGuid().ToString(),
        RequestedAt = DateTimeOffset.UtcNow.AddDays(-8), CooldownEndsAt = DateTimeOffset.UtcNow.AddDays(-1),
        Status = confirmation ? MfaRecoveryRequestStatus.AwaitingConfirmation : MfaRecoveryRequestStatus.Pending,
        RequestTokenHash = confirmation ? Hash(RandomBearer()) : digest,
        ConfirmationTokenHash = confirmation ? digest : null, ConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
    };
    private static ActorContext UserActor(Guid id) => ActorContext.Anonymous with
    {
        ActorKind = ActorKind.User, IsAuthenticated = true, SubjectId = id.ToString()
    };
    private static Mock<IActorContextAccessor> Actor(ActorContext actor)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(actor);
        return accessor;
    }
    private static string RandomBearer() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
