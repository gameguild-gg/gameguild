using System.Text.Json;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     <see cref="ITenantMaintenanceStateService" /> backed by <see cref="IDistributedCache"/>
///     (issue #1134): one key per tenant, set while the maintenance window is open and
///     deleted when it is cleared. Redis in production, so every API instance observes the
///     same window; the host's distributed-memory cache keeps single-instance deployments
///     working. Read paths fail open — an unavailable cache reports "no maintenance" and
///     logs, because the gate must never turn into a platform-wide sign-in outage.
/// </summary>
public sealed class DistributedCacheTenantMaintenanceStateService(
    IDistributedCache cache,
    ILogger<DistributedCacheTenantMaintenanceStateService> logger) : ITenantMaintenanceStateService
{
    private const string KeyPrefix = "auth:tenant-maintenance:";

    /// <summary>Retention for a maintenance key with no scheduled resume (open-ended).</summary>
    private static readonly TimeSpan OpenEndedRetention = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);

    public async Task<TenantMaintenanceState?> FindAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return null;
        }

        try
        {
            var raw = await cache.GetStringAsync(KeyPrefix + tenantId.ToString("N"), cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            var state = JsonSerializer.Deserialize<TenantMaintenanceState>(raw, SerializationOptions);
            if (state is null)
            {
                return null;
            }

            // A scheduled resume that has passed ends the window even if the operator forgot
            // to clear the key; open-ended windows (null) only end when cleared.
            return state.ResumesAt is { } resumesAt && resumesAt <= SystemClock.UtcNow ? null : state;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Tenant maintenance lookup skipped after a cache error: {ExceptionType}",
                exception.GetType().Name);
            return null;
        }
    }

    public async Task SetMaintenanceAsync(Guid tenantId, TenantMaintenanceState? state, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant id is required.", nameof(tenantId));
        }

        try
        {
            if (state is null)
            {
                await cache.RemoveAsync(KeyPrefix + tenantId.ToString("N"), cancellationToken).ConfigureAwait(false);
                return;
            }

            // Retain the key until just past the scheduled resume so the self-clearing read
            // above wins even if nobody deletes it; open-ended windows are re-bounded by the
            // absolute retention instead of living forever.
            var retention = state.ResumesAt is { } resumesAt && resumesAt > SystemClock.UtcNow
                ? resumesAt - SystemClock.UtcNow + TimeSpan.FromMinutes(5)
                : OpenEndedRetention;

            await cache.SetStringAsync(
                KeyPrefix + tenantId.ToString("N"),
                JsonSerializer.Serialize(state with { TenantId = tenantId }, SerializationOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = retention },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Writes are operator actions: surface the failure rather than silently
            // reporting a window that never opened.
            logger.LogError(
                exception,
                "Could not update the maintenance window for tenant {TenantId}",
                tenantId);
            throw;
        }
    }
}
