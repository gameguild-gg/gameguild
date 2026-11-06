using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     PostgreSQL-backed persistence for lost-MFA-device recovery requests, following
///     the PostgreSqlSignInMfaChallengeStore pattern (shared IApplicationDbContext).
/// </summary>
public sealed class PostgreSqlMfaRecoveryRequestStore(IApplicationDbContext context) : IMfaRecoveryRequestStore
{
    private DbSet<MfaRecoveryRequest> Requests => context.Set<MfaRecoveryRequest>();

    public async Task AddAsync(MfaRecoveryRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        await Requests.AddAsync(request, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<MfaRecoveryRequest?> FindLiveByUserAsync(string userId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return Requests.SingleOrDefaultAsync(request => request.UserId == userId &&
            (request.Status == MfaRecoveryRequestStatus.Pending ||
             request.Status == MfaRecoveryRequestStatus.AwaitingConfirmation), cancellationToken);
    }

    public Task<MfaRecoveryRequest?> FindLiveByRequestTokenHashAsync(string requestTokenHash, CancellationToken cancellationToken)
    {
        RequireDigest(requestTokenHash);
        return Requests.SingleOrDefaultAsync(request => request.RequestTokenHash == requestTokenHash &&
            (request.Status == MfaRecoveryRequestStatus.Pending ||
             request.Status == MfaRecoveryRequestStatus.AwaitingConfirmation), cancellationToken);
    }

    public Task<MfaRecoveryRequest?> FindAwaitingByConfirmationTokenHashAsync(string confirmationTokenHash, CancellationToken cancellationToken)
    {
        RequireDigest(confirmationTokenHash);
        return Requests.SingleOrDefaultAsync(request => request.ConfirmationTokenHash == confirmationTokenHash &&
            request.Status == MfaRecoveryRequestStatus.AwaitingConfirmation, cancellationToken);
    }

    public Task<int> ExpireOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        return Requests.Where(request => request.RequestedAt < cutoff &&
                (request.Status == MfaRecoveryRequestStatus.Pending ||
                 request.Status == MfaRecoveryRequestStatus.AwaitingConfirmation))
            .ExecuteUpdateAsync(setters => setters.SetProperty(request => request.Status, MfaRecoveryRequestStatus.Expired),
                cancellationToken);
    }

    public async Task<MfaRecoveryRequest?> CancelLiveByUserAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        RequireOwningTransaction();
        var request = await Requests.AsNoTracking().SingleOrDefaultAsync(row => row.UserId == userId &&
            (row.Status == MfaRecoveryRequestStatus.Pending || row.Status == MfaRecoveryRequestStatus.AwaitingConfirmation),
            cancellationToken).ConfigureAwait(false);
        if (request is null) { return null; }
        DateTimeOffset now = SystemClock.UtcNow;
        var changed = await Requests.Where(row => row.Id == request.Id && row.UserId == userId &&
                (row.Status == MfaRecoveryRequestStatus.Pending || row.Status == MfaRecoveryRequestStatus.AwaitingConfirmation))
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Status, MfaRecoveryRequestStatus.Cancelled)
                .SetProperty(row => row.ConsumedAt, now), cancellationToken).ConfigureAwait(false);
        if (changed != 1) { return null; }
        // The detached snapshot describes the successful update without a later stale tracked write.
        request.Status = MfaRecoveryRequestStatus.Cancelled;
        request.ConsumedAt = now;
        return request;
    }

    public async Task<bool> TryIssueConfirmationAsync(Guid requestId, string requestTokenHash, string confirmationTokenHash,
        DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireDigest(requestTokenHash);
        RequireDigest(confirmationTokenHash);
        RequireOwningTransaction();
        if (requestId == Guid.Empty || expiresAt <= now) { throw new ArgumentException("A valid recovery transition is required."); }
        var cutoff = now.Subtract(MfaRecoveryTokens.RequestRetention);
        var changed = await Requests.Where(row => row.Id == requestId && row.RequestTokenHash == requestTokenHash &&
                (row.Status == MfaRecoveryRequestStatus.Pending || row.Status == MfaRecoveryRequestStatus.AwaitingConfirmation) &&
                row.ConsumedAt == null && row.RequestedAt >= cutoff && row.CooldownEndsAt <= now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Status, MfaRecoveryRequestStatus.AwaitingConfirmation)
                .SetProperty(row => row.ConfirmationTokenHash, confirmationTokenHash)
                .SetProperty(row => row.ConfirmationTokenExpiresAt, expiresAt), cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    public async Task<bool> TryConfirmAsync(Guid requestId, string confirmationTokenHash, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequireDigest(confirmationTokenHash);
        RequireOwningTransaction();
        if (requestId == Guid.Empty) { throw new ArgumentException("A recovery request is required.", nameof(requestId)); }
        var changed = await Requests.Where(row => row.Id == requestId && row.Status == MfaRecoveryRequestStatus.AwaitingConfirmation &&
                row.ConfirmationTokenHash == confirmationTokenHash && row.ConfirmationTokenExpiresAt != null &&
                row.ConfirmationTokenExpiresAt >= now && row.CooldownEndsAt <= now && row.ConsumedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Status, MfaRecoveryRequestStatus.Completed)
                .SetProperty(row => row.ConsumedAt, now), cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    private void RequireOwningTransaction()
    {
        if (context is not DbContext database || !database.Database.IsRelational() || database.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("MFA recovery transitions require the owning relational command transaction.");
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void RequireDigest(string tokenHash)
    {
        if (tokenHash is not { Length: 64 } || !tokenHash.All(IsAsciiDigitLowerHex))
        {
            throw new ArgumentException("A lowercase 64-character hex digest is required.", nameof(tokenHash));
        }
    }

    private static bool IsAsciiDigitLowerHex(char c) => c is (>= '0' and <= '9') or (>= 'a' and <= 'f');
}
