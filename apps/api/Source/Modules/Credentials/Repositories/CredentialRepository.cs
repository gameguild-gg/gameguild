using System.Linq.Expressions;
using GameGuild.Database;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Repository implementation for credential data access operations
/// Adapter implementation following hexagonal architecture principles
/// </summary>
public class CredentialRepository(ApplicationDbContext context) : ICredentialRepository
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Credential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken); }

    public async Task<Credential?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Credential?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.IgnoreQueryFilters().Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetAllAsync(CancellationToken cancellationToken = default) { return await _context.Credentials.Include(c => c.User).ToListAsync(cancellationToken); }

    public async Task<IEnumerable<Credential>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.IgnoreQueryFilters().Include(c => c.User).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.IgnoreQueryFilters().Where(c => c.DeletedAt != null).Include(c => c.User).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Credential> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Credentials.Include(c => c.User);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Credential>> FindAsync(Expression<Func<Credential, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.Where(predicate).Include(c => c.User).ToListAsync(cancellationToken);
    }

    public async Task<Credential?> FirstOrDefaultAsync(Expression<Func<Credential, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.Include(c => c.User).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<Credential, bool>> predicate, CancellationToken cancellationToken = default) { return await _context.Credentials.AnyAsync(predicate, cancellationToken); }

    public async Task<int> CountAsync(Expression<Func<Credential, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        return predicate == null ? await _context.Credentials.CountAsync(cancellationToken) : await _context.Credentials.CountAsync(predicate, cancellationToken);
    }

    public async Task<IEnumerable<Credential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<Credential?> GetByUserIdAndTypeAsync(Guid userId, string type, CancellationToken cancellationToken = default)
    {
        return await _context.Credentials.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == userId && c.Type == type, cancellationToken);
    }

    public async Task<Credential> AddAsync(Credential entity, CancellationToken cancellationToken = default)
    {
        _context.Credentials.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Load the related User for the response
        await _context.Entry(entity).Reference(c => c.User).LoadAsync(cancellationToken);

        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<Credential> entities, CancellationToken cancellationToken = default)
    {
        _context.Credentials.AddRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Credential> UpdateAsync(Credential entity, CancellationToken cancellationToken = default)
    {
        var existingCredential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == entity.Id, cancellationToken);

        if (existingCredential == null) throw new InvalidOperationException($"Credential with ID {entity.Id} not found");

        // Update properties
        existingCredential.Type = entity.Type;
        existingCredential.Value = entity.Value;
        existingCredential.Metadata = entity.Metadata;
        existingCredential.ExpiresAt = entity.ExpiresAt;
        existingCredential.IsActive = entity.IsActive;
        existingCredential.Touch(); // Update timestamp

        await _context.SaveChangesAsync(cancellationToken);

        // Load the related User for the response
        await _context.Entry(existingCredential).Reference(c => c.User).LoadAsync(cancellationToken);

        return existingCredential;
    }

    public async Task UpdateRangeAsync(IEnumerable<Credential> entities, CancellationToken cancellationToken = default)
    {
        _context.Credentials.UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Credential entity, CancellationToken cancellationToken = default)
    {
        _context.Credentials.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Include deleted entities to allow hard deletion of soft-deleted credentials
        var credential = await _context.Credentials.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (credential != null)
        {
            _context.Credentials.Remove(credential);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveRangeAsync(IEnumerable<Credential> entities, CancellationToken cancellationToken = default)
    {
        _context.Credentials.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (credential != null)
        {
            credential.SoftDelete();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Need to include deleted entities to find soft-deleted credentials
        var credential = await _context.Credentials.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt != null, cancellationToken);

        if (credential != null)
        {
            credential.Restore();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> MarkAsUsedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (credential == null) return false;

        credential.MarkAsUsed();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (credential == null) return false;

        credential.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (credential == null) return false;

        credential.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { return await _context.SaveChangesAsync(cancellationToken); }
}
