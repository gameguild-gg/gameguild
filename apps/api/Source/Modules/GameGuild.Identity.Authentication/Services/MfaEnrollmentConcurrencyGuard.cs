using System.Collections.Concurrent;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Serializes MFA lifecycle mutations per subject (issue #1117). Settings-driven
///     enrollment (setup, first-verify flip, disable) is a check-then-act sequence over the
///     single <see cref="UserMfaConfiguration" /> row: without serialization, an enrollment
///     on one device can interleave with a sign-in-time enrollment or a disable on another,
///     producing a torn MFA state (for example a re-run setup clobbering a factor that was
///     enabled a request earlier). The database keeps optimistic concurrency tokens on the
///     MFA columns as the last line of defense (a lost writer fails with
///     <c>DbUpdateConcurrencyException</c>); this guard keeps the common races from ever
///     reaching that layer by claiming the subject's MFA configuration for the whole
///     read-modify-write operation. Sign-in itself only reads the row, and a single-row
///     read is atomic — a concurrent sign-in observes either the old (MFA-off) or the new
///     (MFA-on) state, never a mix.
///     <para>
///         The sign-in-bound enrollment path already serializes through the PostgreSQL
///         advisory lock in <see cref="PostgreSqlSignInMfaEnrollmentPort.AcquireSubjectLockAsync" />;
///         this guard is the in-process equivalent for the settings path and any other
///         mutation performed outside a sign-in transaction. The state is a process-local
///         semaphore keyed by subject id — instances share it via the singleton
///         registration, mirroring the advisory lock on a single API instance.
///     </para>
/// </summary>
public interface IMfaEnrollmentConcurrencyGuard
{
    /// <summary>
    ///     Executes <paramref name="operation" /> while holding the subject's MFA enrollment
    ///     claim. Callers must not await unrelated work inside the claim beyond the MFA
    ///     mutation itself.
    /// </summary>
    Task<T> ExecuteEnrollmentOperationAsync<T>(Guid userId, Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>Value-less variant of <see cref="ExecuteEnrollmentOperationAsync{T}" />.</summary>
    Task ExecuteEnrollmentOperationAsync(Guid userId, Func<Task> operation, CancellationToken cancellationToken = default);
}

/// <summary>
///     Default <see cref="IMfaEnrollmentConcurrencyGuard" />: one in-process
///     <see cref="SemaphoreSlim" /> per subject. Semaphores are created lazily and removed
///     when the last waiter releases, so the map cannot grow with the user base.
/// </summary>
public sealed class MfaEnrollmentConcurrencyGuard : IMfaEnrollmentConcurrencyGuard, IDisposable
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _subjectLocks = new();
    private bool _disposed;

    public async Task<T> ExecuteEnrollmentOperationAsync<T>(Guid userId, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        var subjectLock = AcquireSubjectLock(userId);
        await subjectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            subjectLock.Release();
            ReleaseSubjectLock(userId, subjectLock);
        }
    }

    public async Task ExecuteEnrollmentOperationAsync(Guid userId, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        var subjectLock = AcquireSubjectLock(userId);
        await subjectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            subjectLock.Release();
            ReleaseSubjectLock(userId, subjectLock);
        }
    }

    private SemaphoreSlim AcquireSubjectLock(Guid userId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _subjectLocks.GetOrAdd(userId, static _ => new SemaphoreSlim(1, 1));
    }

    private void ReleaseSubjectLock(Guid userId, SemaphoreSlim subjectLock)
    {
        // Remove the entry only when nobody else is waiting on it; a racing acquirer that
        // missed the removal simply re-adds a fresh semaphore and stays serialized with
        // whoever holds the old one until it drains (currentCount transitions keep both
        // sides safe — the loser just serializes a little longer than strictly needed).
        if (_subjectLocks.TryRemove(new KeyValuePair<Guid, SemaphoreSlim>(userId, subjectLock)) &&
            subjectLock.CurrentCount == 1)
        {
            subjectLock.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var subjectLock in _subjectLocks.Values)
        {
            subjectLock.Dispose();
        }

        _subjectLocks.Clear();
    }
}
