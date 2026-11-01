using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

public sealed class VerificationToken
{
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

/// <summary>
/// Durable storage for one-time email-verification and password-reset tokens
/// (issue #1151). Mirrors <see cref="IMagicLinkTokenStore"/>: only SHA-256
/// digests are persisted, consumption is an atomic single-use claim, and the
/// rows survive restarts and are shared across load-balanced nodes.
/// </summary>
public interface IVerificationTokenStore
{
    Task AddAsync(
        string token,
        Guid userId,
        string email,
        string tokenType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<TokenValidationResult> ConsumeAsync(
        string token,
        string expectedType,
        Guid? expectedUserId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default);

    Task<bool> IsValidAsync(
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public sealed class DatabaseVerificationTokenStore(IApplicationDbContext context) : IVerificationTokenStore
{
    private DbSet<VerificationToken> Tokens => context.Set<VerificationToken>();

    public async Task AddAsync(
        string token,
        Guid userId,
        string email,
        string tokenType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var now = SystemClock.UtcNow;
        await Tokens.Where(item => item.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await Tokens.AddAsync(new VerificationToken
        {
            TokenHash = HashToken(token),
            UserId = userId,
            Email = email.ToLowerInvariant(),
            TokenType = tokenType,
            ExpiresAt = expiresAt
        }, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TokenValidationResult> ConsumeAsync(
        string token,
        string expectedType,
        Guid? expectedUserId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var item = await Tokens.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return TokenValidationResult.Failed("Invalid token");
        }

        if (expectedUserId.HasValue && item.UserId != expectedUserId.Value)
        {
            return TokenValidationResult.Failed("Token does not belong to the requested user");
        }

        if (item.ExpiresAt < consumedAt)
        {
            return TokenValidationResult.Failed("Expired token");
        }

        if (!string.Equals(item.TokenType, expectedType, StringComparison.Ordinal))
        {
            return TokenValidationResult.Failed("Invalid token type");
        }

        // Atomic single-use claim: a row already consumed concurrently updates zero rows.
        var changed = await Tokens
            .Where(candidate => candidate.TokenHash == tokenHash
                                && candidate.ExpiresAt >= consumedAt
                                && candidate.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(candidate => candidate.ConsumedAt, consumedAt),
                cancellationToken)
            .ConfigureAwait(false);

        return changed == 1
            ? new TokenValidationResult(true, item.UserId, item.Email)
            : TokenValidationResult.Failed("Invalid token");
    }

    public Task<bool> IsValidAsync(
        string token,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        return Tokens.AnyAsync(
            item => item.TokenHash == tokenHash && item.ExpiresAt > now && item.ConsumedAt == null,
            cancellationToken);
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
