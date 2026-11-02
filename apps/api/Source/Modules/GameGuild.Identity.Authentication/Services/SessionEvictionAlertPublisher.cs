using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records <see cref="SessionEvictionDetectedV1"/> durable events for the host-side
///     notification bridge (issue #1086), gated by
///     <c>Authentication:SessionNotifications:Enabled</c> (default on). Recording runs in
///     an independent service scope so the outbox entry commits independently of the
///     sign-in transaction, and any failure is swallowed: the eviction notice is
///     defense-in-depth and must never fail session creation.
/// </summary>
public sealed class SessionEvictionAlertPublisher(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SessionEvictionAlertPublisher> logger) : ISessionEvictionAlertPublisher
{
    private const string EnabledPath = "Authentication:SessionNotifications:Enabled";

    public async Task RecordAsync(
        Guid userId,
        Guid sessionId,
        string? deviceSummary,
        int maxConcurrentSessions,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty)
        {
            return;
        }

        if (!configuration.GetValue(EnabledPath, true))
        {
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var events = scope.ServiceProvider.GetRequiredService<IDurableEventProducer>();
            await events.RecordAsync(new SessionEvictionDetectedV1(
                userId,
                sessionId,
                string.IsNullOrWhiteSpace(deviceSummary) ? string.Empty : deviceSummary,
                maxConcurrentSessions)
            {
                TenantId = DurableIntegrationEventTenants.Platform,
                ActorId = DurableIntegrationEventActors.System,
                AggregateType = "User",
                AggregateId = userId.ToString(),
                CorrelationId = Guid.NewGuid()
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Informational notice: a failure here must never break session creation.
            logger.LogError(exception,
                "Could not record the session-eviction event for user {UserId} (session {SessionId})",
                userId, sessionId);
        }
    }
}
