using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.KYC;

public class KycRepository : IKycRepository
{
    private readonly IApplicationDbContext _context;

    public KycRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserKycVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<UserKycVerification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .Include(v => v.User)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.SubmittedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserKycVerification?> GetLatestVerificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .Include(v => v.User)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasApprovedVerificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .AnyAsync(v => v.UserId == userId &&
                          v.Status == KycVerificationStatus.Approved &&
                          (v.ExpiresAt == null || v.ExpiresAt > SystemClock.UtcNow),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<UserKycVerification>> GetByStatusAsync(KycVerificationStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .Include(v => v.User)
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.SubmittedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserKycVerification?> GetByExternalIdAsync(string externalVerificationId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.ExternalVerificationId == externalVerificationId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<UserKycVerification>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserKycVerification>()
            .Include(v => v.User)
            .Where(v => v.SubmittedAt >= startDate && v.SubmittedAt <= endDate)
            .OrderByDescending(v => v.SubmittedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CreateAsync(UserKycVerification verification, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserKycVerification>().AddAsync(verification, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserKycVerification verification, CancellationToken cancellationToken = default)
    {
        _context.Set<UserKycVerification>().Update(verification);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var verification = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (verification != null)
        {
            _context.Set<UserKycVerification>().Remove(verification);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
