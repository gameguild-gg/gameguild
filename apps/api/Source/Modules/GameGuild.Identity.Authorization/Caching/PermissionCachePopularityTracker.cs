using System.Collections.Concurrent;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>A frequently accessed subject/resource pair and its count in the current observation window.</summary>
public sealed record PermissionCachePopularityCandidate(PermissionCacheWarmupRequest Request, int AccessCount);

/// <summary>Collects bounded, process-local ACL access frequencies for scheduled cache warming.</summary>
public interface IPermissionCachePopularityTracker
{
    /// <summary>Records one subject-based ACL access without affecting the authorization decision.</summary>
    void Record(PermissionCacheWarmupRequest request);

    /// <summary>Returns the most frequently accessed pairs and starts a fresh observation window.</summary>
    IReadOnlyList<PermissionCachePopularityCandidate> Drain(int minimumAccessCount, int maximumEntries);

    /// <summary>Suppresses observations in the current asynchronous flow, such as accesses caused by warmup itself.</summary>
    IDisposable SuppressTracking();
}

/// <summary>
///     Keeps only a bounded set of distinct ACL requests between scheduled warmup cycles. New distinct requests are
///     ignored when the current window is full, while already tracked requests continue accumulating frequency.
/// </summary>
public sealed class PermissionCachePopularityTracker : IPermissionCachePopularityTracker
{
    private const int MaxPrincipalIds = 64;

    private readonly bool _enabled;
    private readonly int _capacity;
    private readonly object _windowLock = new();
    private readonly AsyncLocal<int> _suppressionDepth = new();
    private Window _activeWindow = new();

    public PermissionCachePopularityTracker(IOptions<AuthorizationCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _enabled = options.Value.AutomaticWarmupEnabled;
        _capacity = options.Value.PopularityTrackingCapacity;
    }

    public void Record(PermissionCacheWarmupRequest request)
    {
        if (!_enabled || _suppressionDepth.Value > 0 || !TryNormalize(request, out var key, out var normalizedRequest))
        {
            return;
        }

        while (true)
        {
            var window = Volatile.Read(ref _activeWindow);
            if (window.Entries.TryGetValue(key, out var existing))
            {
                existing.Record();
                return;
            }

            lock (_windowLock)
            {
                if (!ReferenceEquals(window, Volatile.Read(ref _activeWindow)))
                {
                    continue;
                }

                if (window.Entries.TryGetValue(key, out existing))
                {
                    existing.Record();
                    return;
                }

                if (window.Entries.Count >= _capacity)
                {
                    return;
                }

                window.Entries.TryAdd(key, new PopularityCounter(normalizedRequest));
                return;
            }
        }
    }

    public IReadOnlyList<PermissionCachePopularityCandidate> Drain(int minimumAccessCount, int maximumEntries)
    {
        if (minimumAccessCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAccessCount), "The minimum access count must be positive.");
        }
        if (maximumEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEntries), "The maximum entry count must be positive.");
        }

        Window completedWindow;
        lock (_windowLock)
        {
            completedWindow = _activeWindow;
            Volatile.Write(ref _activeWindow, new Window());
        }

        return completedWindow.Entries.Values
            .Select(counter => counter.Snapshot())
            .Where(candidate => candidate.AccessCount >= minimumAccessCount)
            .OrderByDescending(candidate => candidate.AccessCount)
            .ThenByDescending(candidate => candidate.LastObservedUtcTicks)
            .Take(maximumEntries)
            .Select(candidate => new PermissionCachePopularityCandidate(candidate.Request, candidate.AccessCount))
            .ToArray();
    }

    public IDisposable SuppressTracking()
    {
        var previousDepth = _suppressionDepth.Value;
        _suppressionDepth.Value = previousDepth + 1;
        return new SuppressionScope(this, previousDepth);
    }

    private static bool TryNormalize(
        PermissionCacheWarmupRequest request,
        out PermissionCacheWarmupKey key,
        out PermissionCacheWarmupRequest normalizedRequest)
    {
        key = default;
        normalizedRequest = request;

        if (request is null || request.TenantId == Guid.Empty || request.Subject is null ||
            request.Subject.RoleIds is null || request.Subject.GroupIds is null ||
            string.IsNullOrWhiteSpace(request.ResourceType) || string.IsNullOrWhiteSpace(request.ResourceId) ||
            request.ResourceType.Length > 128 || request.ResourceId.Length > 255 ||
            request.Subject.RoleIds.Count > MaxPrincipalIds || request.Subject.GroupIds.Count > MaxPrincipalIds ||
            request.Subject.RoleIds.Any(id => id == Guid.Empty) || request.Subject.GroupIds.Any(id => id == Guid.Empty) ||
            request.Subject.UserId == Guid.Empty ||
            (!request.Subject.IsAuthenticated && (request.Subject.UserId.HasValue ||
                                                   request.Subject.RoleIds.Count > 0 || request.Subject.GroupIds.Count > 0)) ||
            (request.Subject.IsAuthenticated && !request.Subject.UserId.HasValue))
        {
            return false;
        }

        (key, normalizedRequest) = PermissionCacheWarmupKey.From(request);
        return true;
    }

    private sealed class Window
    {
        public ConcurrentDictionary<PermissionCacheWarmupKey, PopularityCounter> Entries { get; } = new();
    }

    private sealed class PopularityCounter(PermissionCacheWarmupRequest request)
    {
        private int _accessCount = 1;
        private long _lastObservedUtcTicks = DateTime.UtcNow.Ticks;

        public void Record()
        {
            Interlocked.Increment(ref _accessCount);
            Interlocked.Exchange(ref _lastObservedUtcTicks, DateTime.UtcNow.Ticks);
        }

        public CounterSnapshot Snapshot()
        {
            return new CounterSnapshot(request, Volatile.Read(ref _accessCount), Volatile.Read(ref _lastObservedUtcTicks));
        }
    }

    private sealed record CounterSnapshot(PermissionCacheWarmupRequest Request, int AccessCount, long LastObservedUtcTicks);

    private sealed class SuppressionScope(PermissionCachePopularityTracker tracker, int previousDepth) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                tracker._suppressionDepth.Value = previousDepth;
            }
        }
    }
}
