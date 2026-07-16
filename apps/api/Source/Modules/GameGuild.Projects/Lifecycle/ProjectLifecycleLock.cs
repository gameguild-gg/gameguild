using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Projects;

public interface IProjectLifecycleLock
{
    Task<IProjectLifecycleLockHandle> AcquireAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}

public interface IProjectLifecycleLockHandle : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public sealed class ProjectLifecycleLock(IApplicationDbContext context) : IProjectLifecycleLock
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> FallbackLocks = new();

    public async Task<IProjectLifecycleLockHandle> AcquireAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (context is DbContext dbContext &&
            dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var transaction = dbContext.Database.CurrentTransaction == null
                ? await context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                : null;
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                        "SELECT pg_advisory_xact_lock({0})",
                        [CreateLockKey(projectId)],
                        cancellationToken)
                    .ConfigureAwait(false);
                return new Handle(transaction, null);
            }
            catch
            {
                if (transaction != null) await transaction.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        var fallback = FallbackLocks.GetOrAdd(projectId, static _ => new SemaphoreSlim(1, 1));
        await fallback.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Handle(null, fallback);
    }

    private static long CreateLockKey(Guid projectId)
    {
        Span<byte> source = stackalloc byte[33];
        "project-lifecycle"u8.CopyTo(source);
        projectId.TryWriteBytes(source[17..]);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(source, hash);
        return BitConverter.ToInt64(hash);
    }

    private sealed class Handle(
        IDbContextTransaction? transaction,
        SemaphoreSlim? fallback) : IProjectLifecycleLockHandle
    {
        private bool _disposed;

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => transaction == null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (transaction != null) await transaction.DisposeAsync().ConfigureAwait(false);
            fallback?.Release();
        }
    }
}
