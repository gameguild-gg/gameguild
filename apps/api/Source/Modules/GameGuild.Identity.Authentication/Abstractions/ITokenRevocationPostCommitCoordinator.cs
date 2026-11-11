namespace GameGuild.Identity.Authentication;

/// <summary>
///     Routes session access-token deny-list writes (issue #1179) with transaction awareness.
///     The host composition implements this: when the caller runs inside an ambient database
///     transaction (the command pipeline), the write is deferred until the transaction commits
///     and discarded on rollback — a failed terminate-everywhere must not leave deny-list
///     residue that kills still-valid tokens. Without an ambient transaction the write is
///     immediate. See <c>GameGuild.API.Authorization.TransactionAwareTokenRevocationCoordinator</c>.
/// </summary>
public interface ITokenRevocationPostCommitCoordinator
{
    /// <summary>
    ///     Writes (or defers) the session access-token deny-list entry for a terminated session.
    /// </summary>
    Task RevokeSessionAccessTokenAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Writes every deferred entry. Called by the command pipeline after its transaction
    ///     commits; best-effort — a failed write is logged and dropped (the session row's
    ///     terminated state remains the primary revocation signal).
    /// </summary>
    Task FlushCommittedAsync(CancellationToken cancellationToken = default);

    /// <summary>Discards every deferred entry (rollback path).</summary>
    void DiscardPending();
}
