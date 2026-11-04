using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records <see cref="OtherSessionsTerminatedV1"/> durable events for the host-side
///     notification bridge, gated by <c>Authentication:SecurityNotifications:Enabled</c>
/// (default on). Mirrors <see cref="NewLoginAlertPublisher"/>: recording runs in an
/// independent service scope so the outbox entry commits independently of the password
/// operation, and any failure is swallowed because the notification is defense-in-depth.
/// </summary>
public sealed class OtherSessionsTerminatedAlertPublisher(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OtherSessionsTerminatedAlertPublisher> logger) : IOtherSessionsTerminatedAlertPublisher
{
    private const string EnabledPath = "Authentication:SecurityNotifications:Enabled";

    public async Task RecordAsync(
        Guid userId,
        Guid? tenantId,
        string reason,
        int terminatedSessionCount,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || terminatedSessionCount <= 0)
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
            await events.RecordAsync(new OtherSessionsTerminatedV1(
                userId,
                string.IsNullOrWhiteSpace(reason) ? "PasswordChanged" : reason,
                terminatedSessionCount)
            {
                TenantId = tenantId ?? DurableIntegrationEventTenants.Platform,
                ActorId = DurableIntegrationEventActors.System,
                AggregateType = "User",
                AggregateId = userId.ToString(),
                CorrelationId = Guid.NewGuid()
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Informational notification: a failure here must never fail the password change.
            logger.LogError(exception,
                "Could not record the sessions-terminated event for user {UserId}",
                userId);
        }
    }
}
