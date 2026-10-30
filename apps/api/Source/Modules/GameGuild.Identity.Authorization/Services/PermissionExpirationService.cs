using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Implementation of <see cref="IPermissionExpirationService"/> (issue #331):
///     automatic expiration of tenant permission grants, upcoming-expiration
///     notifications, and bulk administrative expiration management.
/// </summary>
/// <remarks>
///     <para>
///         <b>SECURITY - Cache invalidation:</b> every mutation increments the tenant
///         security version (<see cref="ITenantSecurityVersionStore"/>) so cached
///         permission data is invalidated immediately.
///     </para>
///     <para>
///         <b>SECURITY - Audit:</b> every mutation writes a
///         <see cref="PermissionAuditLog"/> entry; automatic expirations use
///         <see cref="PermissionOperationType.Expire"/> with a system actor,
///         administrative set/extend use the authenticated actor from
///         <see cref="IActorContextAccessor"/>.
///     </para>
///     <para>
///         Expired grants already contribute nothing to permission evaluation
///         (<c>EffectivePermissionResolverService</c> and <c>PermissionQueryService</c>
///         filter them fail-closed); the durable deactivation performed here exists so
///         revocation survives re-activation paths and produces the audit/notification
///         trail required for temporal access control.
///     </para>
/// </remarks>
public sealed class PermissionExpirationService(
    ITenantPermissionRepository repository,
    IPermissionAuditService auditService,
    ITenantSecurityVersionStore securityVersionStore,
    IOptions<PermissionExpirationOptions> expirationOptions,
    ILogger<PermissionExpirationService> logger,
    IPermissionMutationExecutor mutationExecutor,
    IPublisher? publisher = null,
    IActorContextAccessor? actorContextAccessor = null
) : IPermissionExpirationService
{
    private const string ReminderStampKey = "expirationReminderAt";
    private const string ProcessedStampKey = "expirationProcessedAt";

    private PermissionExpirationOptions Options => expirationOptions.Value;

    private ActorContext Actor => actorContextAccessor?.ActorContext ?? ActorContext.Anonymous;

    public async Task<int> ProcessExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var actor = Actor;
        if (actor.IsAuthenticated && (!actor.IsSystemAdmin || actor.SubjectIdAsGuid is null || actor.SubjectIdAsGuid == Guid.Empty))
        {
            throw new UnauthorizedAccessException("Cross-tenant expiration processing requires system administration.");
        }
        if (Options.Validate().Count != 0) { throw new InvalidOperationException("Invalid permission expiration configuration."); }
        var actorId = actor.SubjectIdAsGuid ?? Guid.Empty;
        var expired = await repository.GetExpiredPermissionsAsync(cancellationToken).ConfigureAwait(false);
        var processed = 0;
        var failures = new List<Exception>();
        var eligible = expired.Where(grant => !HasStamp(grant, ProcessedStampKey)).Take(Options.BatchSize).ToArray();
        foreach (var group in eligible.GroupBy(grant => grant.TenantId ?? Guid.Empty))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var groupFailures = new List<Exception>();
            var batch = await ProcessGroupAsync(group, actorId, failures, groupFailures, cancellationToken).ConfigureAwait(false);
            failures.AddRange(groupFailures);
            if (batch.IsFailure) { continue; }
            processed += batch.Value.Notifications.Count;
            foreach (var notification in batch.Value.Notifications)
            {
                try { await PublishAsync(notification, cancellationToken).ConfigureAwait(false); }
                catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
                {
                    // Delivery remains best effort; an accepted expiration is not rolled back
                    // or advertised as retryable merely because alert queueing failed.
                    logger.LogWarning(exception, "Failed to publish a accepted permission expiration alert.");
                }
            }
        }
        if (failures.Count > 0)
        {
            var failure = new AggregateException("Required permission expiration processing failed; retry pending grants.", failures);
            failure.Data["ProcessedGrantCount"] = processed;
            throw failure;
        }
        logger.LogInformation("Processed {Count} expired permission grants.", processed);
        return processed;
    }

    private Task<Result<ExpirationBatchOutcome>> ProcessGroupAsync(IGrouping<Guid, TenantPermission> group,
        Guid actorId, List<Exception> failures, List<Exception> groupFailures, CancellationToken cancellationToken)
    {
        var scope = group.Key == Guid.Empty ? (Guid?)null : group.Key;
        return ExecuteMutationAsync(actorId, scope, async token =>
        {
            var notifications = new List<PermissionExpirationNotification>();
            foreach (var grant in group)
            {
                token.ThrowIfCancellationRequested();
                var attempt = await ProcessGrantAsync(grant, actorId, groupFailures, token).ConfigureAwait(false);
                if (attempt.IsSuccess) { notifications.Add(attempt.Value); }
            }
            if (notifications.Count > 0)
            {
                await IncrementTenantVersionAsync(group.Key, token).ConfigureAwait(false);
            }
            return Result.Success(new ExpirationBatchOutcome(notifications));
        }, cancellationToken, failures, outcome => outcome.Notifications.Count > 0, true);
    }

    private Task<Result<PermissionExpirationNotification>> ProcessGrantAsync(TenantPermission grant,
        Guid actorId, List<Exception> failures, CancellationToken cancellationToken)
    {
        var originalExpiresAt = grant.ExpiresAt;
        return ExecuteMutationAsync(actorId, grant.TenantId, async token =>
        {
            grant.Expire();
            SetStamp(grant, ProcessedStampKey);
            await repository.UpdateAsync(grant, token).ConfigureAwait(false);
            var audit = await auditService.LogPermissionChangeAsync(
                PermissionOperationType.Expire, grant.UserId, actorId, grant.TenantId,
                permissionType: "Tenant", resourceType: "TenantPermission", resourceId: grant.Id,
                oldValue: originalExpiresAt?.ToString("O"), newValue: "inactive",
                reason: "Automatic permission expiration", cancellationToken: token).ConfigureAwait(false);
            if (audit is null || audit.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Permission expiration audit was not persisted.");
            }
            return Result.Success(new PermissionExpirationNotification(grant.Id, grant.UserId,
                grant.TenantId, grant.Permissions.ToArray(), originalExpiresAt ?? SystemClock.UtcNow,
                PermissionExpirationKind.Expired));
        }, cancellationToken, failures, null, false);
    }

    private async Task<Result<T>> ExecuteMutationAsync<T>(Guid actorId, Guid? tenantId,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
        List<Exception> failures, Func<T, bool>? shouldCommit, bool useEntityTenantScope)
    {
        Exception? operationFailure = null;
        try
        {
            var result = await mutationExecutor.ExecuteAsync(actorId, tenantId, async token =>
            {
                try { return await operation(token).ConfigureAwait(false); }
                catch (Exception exception) { operationFailure = exception; throw; }
            }, cancellationToken, shouldCommit, useEntityTenantScope).ConfigureAwait(false);
            if (result.IsFailure)
            {
                failures.Add(new InvalidOperationException(result.Error.Description));
            }
            return result;
        }
        catch (Exception exception) when (ReferenceEquals(exception, operationFailure)
            && exception is not AggregateException and not OperationCanceledException and not OutOfMemoryException)
        {
            // The shared executor rethrows the original operation failure only after
            // confirmed rollback. Setup/commit/rollback failures must escape immediately.
            failures.Add(exception);
            return Result.Failure<T>(Error.Failure("PermissionExpiration.OperationFailed", "Required expiration processing failed."));
        }
    }

    private sealed record ExpirationBatchOutcome(IReadOnlyList<PermissionExpirationNotification> Notifications);

    public async Task<int> SendUpcomingExpirationRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        var cutoff = now + Options.UpcomingNotificationWindow;
        var expiring = await repository.GetExpiringBeforeAsync(cutoff, cancellationToken).ConfigureAwait(false);
        var published = 0;

        var eligible = expiring.Where(grant => !TryGetStamp(grant, ReminderStampKey, out var lastReminder)
            || now - lastReminder >= Options.MinimumReminderInterval);
        foreach (var grant in eligible.Take(Options.BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                SetStamp(grant, ReminderStampKey);
                await repository.UpdateAsync(grant, cancellationToken).ConfigureAwait(false);

                await PublishAsync(
                    new PermissionExpirationNotification(
                        grant.Id,
                        grant.UserId,
                        grant.TenantId,
                        grant.Permissions.ToArray(),
                        grant.ExpiresAt!.Value,
                        PermissionExpirationKind.Upcoming),
                    cancellationToken).ConfigureAwait(false);

                published++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning(exception,
                    "Failed to send expiration reminder for permission grant {PermissionId}.",
                    grant.Id);
            }
        }

        if (published > 0)
        {
            logger.LogInformation("Published {Count} upcoming-expiration reminders.", published);
        }

        return published;
    }

    public async Task<List<TenantPermission>> SetExpirationAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> permissionIds,
        DateTime? expiresAt,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionIds);

        if (expiresAt.HasValue && expiresAt.Value <= SystemClock.UtcNow)
        {
            throw new ArgumentException("Expiration must be in the future. Use ProcessExpiredAsync to expire grants now.", nameof(expiresAt));
        }

        return await MutateExpirationAsync(
            tenantId,
            permissionIds,
            grant =>
            {
                grant.ExpiresAt = expiresAt;
                grant.IsActive = expiresAt is null || expiresAt.Value > SystemClock.UtcNow;
                ClearStamp(grant, ReminderStampKey);
                ClearStamp(grant, ProcessedStampKey);
            },
            expiresAt is null ? "Expiration cleared (grant made permanent)" : $"Expiration set to {expiresAt.Value:O}",
            reason,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TenantPermission>> ExtendExpirationAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> permissionIds,
        TimeSpan extension,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionIds);

        if (extension <= TimeSpan.Zero)
        {
            throw new ArgumentException("Extension must be positive.", nameof(extension));
        }

        var now = SystemClock.UtcNow;

        return await MutateExpirationAsync(
            tenantId,
            permissionIds,
            grant =>
            {
                var baseline = grant.ExpiresAt is { } current && current > now ? current : now;
                var newExpiresAt = baseline + extension;
                grant.ExpiresAt = newExpiresAt;
                grant.IsActive = true;
                ClearStamp(grant, ReminderStampKey);
                ClearStamp(grant, ProcessedStampKey);
            },
            $"Expiration extended by {extension}",
            reason,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<TenantPermission>> GetExpiringAsync(
        Guid tenantId,
        DateTime? cutoff = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveCutoff = cutoff ?? SystemClock.UtcNow + Options.UpcomingNotificationWindow;
        var expiring = await repository.GetExpiringBeforeAsync(effectiveCutoff, cancellationToken).ConfigureAwait(false);

        return expiring.Where(p => p.TenantId == tenantId).ToList();
    }

    private async Task<List<TenantPermission>> MutateExpirationAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> permissionIds,
        Action<TenantPermission> mutate,
        string operationDescription,
        string? reason,
        CancellationToken cancellationToken)
    {
        var grants = await repository.GetByIdsInTenantAsync(tenantId, permissionIds, cancellationToken).ConfigureAwait(false);
        var performedBy = Actor.SubjectIdAsGuid ?? Guid.Empty;
        var updated = new List<TenantPermission>(grants.Count);

        foreach (var grant in grants)
        {
            var previousExpiresAt = grant.ExpiresAt;
            mutate(grant);
            await repository.UpdateAsync(grant, cancellationToken).ConfigureAwait(false);
            updated.Add(grant);

            await auditService.LogPermissionChangeAsync(
                PermissionOperationType.Update,
                grant.UserId,
                performedBy,
                grant.TenantId,
                permissionType: "Tenant",
                resourceType: "TenantPermission",
                resourceId: grant.Id,
                oldValue: previousExpiresAt?.ToString("O") ?? "permanent",
                newValue: grant.ExpiresAt?.ToString("O") ?? "permanent",
                reason: string.IsNullOrWhiteSpace(reason) ? operationDescription : $"{operationDescription}. {reason}",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (updated.Count > 0)
        {
            // SECURITY: Invalidate cached permissions for the mutated tenant.
            await IncrementTenantVersionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Bulk expiration mutation '{Operation}' applied to {Count} grants in tenant {TenantId}.",
            operationDescription,
            updated.Count,
            tenantId);

        return updated;
    }

    private async Task IncrementTenantVersionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var newVersion = await securityVersionStore
                .IncrementVersionAsync(tenantId.ToString(), cancellationToken)
                .ConfigureAwait(false);
            if (newVersion <= 0) { throw new InvalidOperationException("Permission invalidation did not advance the security version."); }
            logger.LogDebug("Incremented security version for tenant {TenantId} to {Version}.", tenantId, newVersion);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Failed to increment security version for tenant {TenantId}. Cache may be stale.",
                tenantId);
            throw;
        }
    }

    private async Task PublishAsync(PermissionExpirationNotification notification, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
    }

    private static bool HasStamp(TenantPermission grant, string key)
    {
        return TryGetStamp(grant, key, out _);
    }

    private static bool TryGetStamp(TenantPermission grant, string key, out DateTime stamp)
    {
        stamp = default;

        if (grant.Metadata is null || !grant.Metadata.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        string? raw = value switch
        {
            DateTime dateTime => dateTime.ToString("O"),
            string text => text,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } element => element.GetString(),
            _ => null
        };

        return raw is not null && DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out stamp);
    }

    private static void SetStamp(TenantPermission grant, string key)
    {
        grant.Metadata ??= new Dictionary<string, object>();
        grant.Metadata[key] = SystemClock.UtcNow.ToString("O");
    }

    private static void ClearStamp(TenantPermission grant, string key)
    {
        grant.Metadata?.Remove(key);
    }
}
