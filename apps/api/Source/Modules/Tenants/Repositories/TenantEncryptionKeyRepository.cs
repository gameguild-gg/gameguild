using GameGuild.Database;
using GameGuild.Modules.Tenants;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants.Repositories;

public class TenantEncryptionKeyRepository : ITenantEncryptionKeyRepository
{
    private readonly ApplicationDbContext _context;

    public TenantEncryptionKeyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantEncryptionKey?> GetByIdAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantEncryptionKey>()
            .FirstOrDefaultAsync(k => k.Id == keyId, cancellationToken);
    }

    public async Task<TenantEncryptionKey?> GetActiveKeyAsync(Guid tenantId, TenantKeyPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantEncryptionKey>()
            .Where(k => k.TenantId == tenantId && k.KeyPurpose == purpose && k.Status == TenantKeyStatus.Active)
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TenantEncryptionKey>> GetKeyHistoryAsync(Guid tenantId, TenantKeyPurpose purpose, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantEncryptionKey>()
            .Where(k => k.TenantId == tenantId && k.KeyPurpose == purpose)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantEncryptionKey> CreateAsync(TenantEncryptionKey key, CancellationToken cancellationToken = default)
    {
        await _context.Set<TenantEncryptionKey>().AddAsync(key, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task<TenantEncryptionKey> UpdateAsync(TenantEncryptionKey key, CancellationToken cancellationToken = default)
    {
        _context.Set<TenantEncryptionKey>().Update(key);
        await _context.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task DeleteAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await GetByIdAsync(keyId, cancellationToken);
        if (key != null)
        {
            _context.Set<TenantEncryptionKey>().Remove(key);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> KeyNameExistsAsync(Guid tenantId, string keyName, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantEncryptionKey>()
            .AnyAsync(k => k.TenantId == tenantId && k.KeyName == keyName, cancellationToken);
    }
}
