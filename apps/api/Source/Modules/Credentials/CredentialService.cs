using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Users;

/// <summary>
/// Service implementation for managing user credentials
/// </summary>
public class CredentialService(ApplicationDbContext context) : ICredentialService {
  /// <summary>
  /// Get all credentials for a user
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <returns>List of credentials</returns>
  public async Task<IEnumerable<Credential>> GetCredentialsByUserIdAsync(Guid userId) { return await context.Credentials.Where(c => c.UserId == userId).ToListAsync(); }

  /// <summary>
  /// Get a specific credential by ID
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>Credential or null if not found</returns>
  public async Task<Credential?> GetCredentialByIdAsync(Guid id) { return await context.Credentials.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id); }

  /// <summary>
  /// Get a credential by user ID and type
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <param name="type">Credential type</param>
  /// <returns>Credential or null if not found</returns>
  public async Task<Credential?> GetCredentialByUserIdAndTypeAsync(Guid userId, string type) {
    return await context.Credentials.Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.Type == type);
  }

  /// <summary>
  /// Create a new credential
  /// </summary>
  /// <param name="credential">Credential to create</param>
  /// <returns>Created credential</returns>
  public async Task<Credential> CreateCredentialAsync(Credential credential) {
    context.Credentials.Add(credential);
    await context.SaveChangesAsync();

    // Load the related User for the response
    await context.Entry(credential).Reference(c => c.User).LoadAsync();

    return credential;
  }

  /// <summary>
  /// Update an existing credential
  /// </summary>
  /// <param name="credential">Credential to update</param>
  /// <returns>Updated credential</returns>
  public async Task<Credential> UpdateCredentialAsync(Credential credential) {
    var existingCredential = await context.Credentials.FirstOrDefaultAsync(c => c.Id == credential.Id);

    if (existingCredential == null) throw new InvalidOperationException($"Credential with ID {credential.Id} not found");

    // Update properties
    existingCredential.Type = credential.Type;
    existingCredential.Value = credential.Value;
    existingCredential.Metadata = credential.Metadata;
    existingCredential.ExpiresAt = credential.ExpiresAt;
    existingCredential.IsActive = credential.IsActive;
    existingCredential.Touch(); // Update timestamp

    await context.SaveChangesAsync();

    // Load the related User for the response
    await context.Entry(existingCredential).Reference(c => c.User).LoadAsync();

    return existingCredential;
  }

  /// <summary>
  /// Soft delete a credential
  /// </summary>
  /// <param name="id">Credential ID to delete</param>
  /// <returns>True if deleted successfully</returns>
  public async Task<bool> SoftDeleteCredentialAsync(Guid id) {
    var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Id == id);

    if (credential == null) return false;

    credential.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Restore a soft-deleted credential
  /// </summary>
  /// <param name="id">Credential ID to restore</param>
  /// <returns>True if restored successfully</returns>
  public async Task<bool> RestoreCredentialAsync(Guid id) {
    // Need to include deleted entities to find soft-deleted credentials
    var credential = await context.Credentials.IgnoreQueryFilters()
                                  .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt != null);

    if (credential == null) return false;

    credential.Restore();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Permanently delete a credential
  /// </summary>
  /// <param name="id">Credential ID to delete</param>
  /// <returns>True if deleted successfully</returns>
  public async Task<bool> HardDeleteCredentialAsync(Guid id) {
    // Include deleted entities to allow hard deletion of soft-deleted credentials
    var credential = await context.Credentials.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);

    if (credential == null) return false;

    context.Credentials.Remove(credential);
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Mark a credential as used
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>True if marked successfully</returns>
  public async Task<bool> MarkCredentialAsUsedAsync(Guid id) {
    var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Id == id);

    if (credential == null) return false;

    credential.MarkAsUsed();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Deactivate a credential
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>True if deactivated successfully</returns>
  public async Task<bool> DeactivateCredentialAsync(Guid id) {
    var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Id == id);

    if (credential == null) return false;

    credential.Deactivate();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Activate a credential
  /// </summary>
  /// <param name="id">Credential ID</param>
  /// <returns>True if activated successfully</returns>
  public async Task<bool> ActivateCredentialAsync(Guid id) {
    var credential = await context.Credentials.FirstOrDefaultAsync(c => c.Id == id);

    if (credential == null) return false;

    credential.Activate();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Get all credentials including soft-deleted ones
  /// </summary>
  /// <returns>List of all credentials</returns>
  public async Task<IEnumerable<Credential>> GetAllCredentialsAsync() { return await context.Credentials.IgnoreQueryFilters().Include(c => c.User).ToListAsync(); }

  /// <summary>
  /// Get soft-deleted credentials
  /// </summary>
  /// <returns>List of soft-deleted credentials</returns>
  public async Task<IEnumerable<Credential>> GetDeletedCredentialsAsync() {
    return await context.Credentials.IgnoreQueryFilters()
                        .Where(c => c.DeletedAt != null)
                        .Include(c => c.User)
                        .ToListAsync();
  }
}
