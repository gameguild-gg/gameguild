using GameGuild.Identity.Authentication;
using GameGuild.API.Database;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.Authorization;

/// <summary>
///     Transaction-aware routing for session access-token deny-list writes (issue #1179).
///     Implemented in the API layer because only the host composition knows the ambient EF
///     transaction: inside the command pipeline's transaction the write is deferred until the
///     use-case operation behavior commits (and discarded on rollback — a failed
///     terminate-everywhere must not leave deny-list residue that kills still-valid tokens);
///     outside a transaction the write is immediate.
/// </summary>
public sealed class TransactionAwareTokenRevocationCoordinator(
    ApplicationDbContext context,
    ITokenRevocationService tokenRevocationService,
    ILogger<TransactionAwareTokenRevocationCoordinator> logger) : ITokenRevocationPostCommitCoordinator
{
    private readonly List<KeyValuePair<Guid, string>> _pending = [];

    public Task RevokeSessionAccessTokenAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is null)
        {
            return WriteSafeAsync(sessionId, reason, cancellationToken);
        }

        // Inside the command pipeline's transaction: defer until commit (the pipeline flushes).
        _pending.Add(new KeyValuePair<Guid, string>(sessionId, reason));
        return Task.CompletedTask;
    }

    public async Task FlushCommittedAsync(CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var pending = _pending.ToArray();
        _pending.Clear();
        foreach (var entry in pending)
        {
            await WriteSafeAsync(entry.Key, entry.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    public void DiscardPending() => _pending.Clear();

    private async Task WriteSafeAsync(Guid sessionId, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await tokenRevocationService.RevokeSessionAccessTokenAsync(sessionId, reason, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Session access-token deny-list write failed for session {SessionId}: {ErrorType}",
                sessionId, exception.GetType().FullName);
        }
    }
}
