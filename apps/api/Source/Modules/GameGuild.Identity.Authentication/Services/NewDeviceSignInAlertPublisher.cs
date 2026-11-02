using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records <see cref="NewDeviceSignInDetectedV1"/> durable events for the host-side
///     notification bridge (issue #1085), gated by
///     <c>Authentication:DeviceNotifications:Enabled</c> (default on — the per-user
///     delivery preferences are evaluated by the consumer). Recording runs in an
///     independent service scope so the outbox entry commits independently of the
///     sign-in transaction, and any failure is swallowed: the new-device alert is
///     defense-in-depth and must never fail authentication.
/// </summary>
public sealed class NewDeviceSignInAlertPublisher(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<NewDeviceSignInAlertPublisher> logger) : INewDeviceSignInAlertPublisher
{
    private const string EnabledPath = "Authentication:DeviceNotifications:Enabled";

    public async Task RecordAsync(
        Guid userId,
        Guid sessionId,
        string? deviceSummary,
        string? locationSummary,
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
            await events.RecordAsync(new NewDeviceSignInDetectedV1(
                userId,
                sessionId,
                string.IsNullOrWhiteSpace(deviceSummary) ? string.Empty : deviceSummary,
                string.IsNullOrWhiteSpace(locationSummary) ? string.Empty : locationSummary)
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
            // Security alert recording is defense-in-depth: a failure here must never
            // break authentication. The durable transport retries recorded events.
            logger.LogError(exception,
                "Could not record the new-device sign-in event for user {UserId} (session {SessionId})",
                userId, sessionId);
        }
    }
}
