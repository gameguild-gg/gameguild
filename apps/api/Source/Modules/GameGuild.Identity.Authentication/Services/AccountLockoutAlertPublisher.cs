using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records <see cref="AccountLockoutDetectedV1"/> durable events for the host-side
///     notification bridge (issue #1083), gated by
///     <c>Authentication:SecurityNotifications:Enabled</c> (default on) — the same gate
///     as <see cref="SuspiciousLoginAlertPublisher"/>. Recording runs in an independent
///     service scope so the outbox entry commits independently of the denied request,
///     and any failure is swallowed: the lockout alert is defense-in-depth and must
///     never change the sign-in denial path.
/// </summary>
public sealed class AccountLockoutAlertPublisher(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AccountLockoutAlertPublisher> logger) : IAccountLockoutAlertPublisher
{
    private const string EnabledPath = "Authentication:SecurityNotifications:Enabled";

    public async Task RecordAsync(
        Guid userId,
        DateTime lockoutEndsAtUtc,
        int failedAttemptThreshold,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
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
            await events.RecordAsync(new AccountLockoutDetectedV1(
                userId,
                lockoutEndsAtUtc.ToUniversalTime(),
                failedAttemptThreshold)
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
            // change the lockout denial. The durable transport retries recorded events.
            logger.LogError(exception,
                "Could not record the account-lockout alert event for user {UserId}", userId);
        }
    }
}
