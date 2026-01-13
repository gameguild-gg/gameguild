using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository implementation for WebAuthn/FIDO2 credentials.
/// </summary>
public class WebAuthnCredentialRepository(IApplicationDbContext context) : IWebAuthnCredentialRepository
{
    private DbSet<UserWebAuthnCredential> Credentials => context.Set<UserWebAuthnCredential>();

    public async Task<UserWebAuthnCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<UserWebAuthnCredential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId && c.IsActive, cancellationToken);
    }

    public async Task<List<UserWebAuthnCredential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserWebAuthnCredential>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetCredentialIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .Select(c => c.CredentialId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserWebAuthnCredential> CreateAsync(UserWebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        credential.CreatedAt = DateTime.UtcNow;
        Credentials.Add(credential);
        await context.SaveChangesAsync(cancellationToken);
        return credential;
    }

    public async Task<UserWebAuthnCredential> UpdateAsync(UserWebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        Credentials.Update(credential);
        await context.SaveChangesAsync(cancellationToken);
        return credential;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (credential == null) return false;

        Credentials.Remove(credential);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HasActiveCredentialsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.IsActive, cancellationToken);
    }

    public async Task<int> CountActiveCredentialsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await Credentials
            .AsNoTracking()
            .CountAsync(c => c.UserId == userId && c.IsActive, cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (credential == null) return false;

        credential.IsActive = false;
        credential.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateSignatureCounterAsync(Guid id, uint newCounter, CancellationToken cancellationToken = default)
    {
        var credential = await Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (credential != null)
        {
            credential.SignatureCounter = newCounter;
            credential.LastUsedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
