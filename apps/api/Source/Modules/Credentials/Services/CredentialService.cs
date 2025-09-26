namespace GameGuild.Modules.Credentials;

/// <summary> Service implementation for managing user credentials </summary>
public class CredentialService(ICredentialRepository credentialRepository) : ICredentialService
{
    private readonly ICredentialRepository _credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));

    /// <summary> Get all credentials for a user </summary>
    /// <param name="userId"> User ID </param>
    /// <returns> List of credentials </returns>
    public async Task<IEnumerable<Credential>> GetCredentialsByUserIdAsync(Guid userId) { return await _credentialRepository.GetByUserIdAsync(userId); }

    /// <summary> Get a specific credential by ID </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> Credential or null if not found </returns>
    public async Task<Credential?> GetCredentialByIdAsync(Guid id) { return await _credentialRepository.GetByIdWithUserAsync(id); }

    /// <summary> Get a credential by ID including soft-deleted entries </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> Credential or null if not found </returns>
    public async Task<Credential?> GetCredentialIncludingDeletedAsync(Guid id) { return await _credentialRepository.GetByIdIncludingDeletedAsync(id); }

    /// <summary> Get a credential by user ID and type </summary>
    /// <param name="userId"> User ID </param>
    /// <param name="type"> Credential type </param>
    /// <returns> Credential or null if not found </returns>
    public async Task<Credential?> GetCredentialByUserIdAndTypeAsync(Guid userId, string type) { return await _credentialRepository.GetByUserIdAndTypeAsync(userId, type); }

    /// <summary> Create a new credential </summary>
    /// <param name="credential"> Credential to create </param>
    /// <returns> Created credential </returns>
    public async Task<Credential> CreateCredentialAsync(Credential credential) { return await _credentialRepository.AddAsync(credential); }

    /// <summary> Update an existing credential </summary>
    /// <param name="credential"> Credential to update </param>
    /// <returns> Updated credential </returns>
    public async Task<Credential> UpdateCredentialAsync(Credential credential) { return await _credentialRepository.UpdateAsync(credential); }

    /// <summary> Soft delete a credential </summary>
    /// <param name="id"> Credential ID to delete </param>
    /// <returns> True if deleted successfully </returns>
    public async Task<bool> SoftDeleteCredentialAsync(Guid id)
    {
        Credential? credential = await _credentialRepository.GetByIdAsync(id);

        if (credential == null)
        {
            return false;
        }

        if (credential.DeletedAt != null)
        {
            return false;
        }

        await _credentialRepository.SoftDeleteAsync(id);

        return true;
    }

    /// <summary> Restore a soft-deleted credential </summary>
    /// <param name="id"> Credential ID to restore </param>
    /// <returns> True if restored successfully </returns>
    public async Task<bool> RestoreCredentialAsync(Guid id)
    {
        Credential? credential = await _credentialRepository.GetByIdIncludingDeletedAsync(id);

        if (credential == null)
        {
            return false;
        }

        if (credential.DeletedAt == null)
        {
            return false;
        }

        await _credentialRepository.RestoreAsync(id);

        return true;
    }

    /// <summary> Permanently delete a credential </summary>
    /// <param name="id"> Credential ID to delete </param>
    /// <returns> True if deleted successfully </returns>
    public async Task<bool> HardDeleteCredentialAsync(Guid id)
    {
        Credential? credential = await _credentialRepository.GetByIdIncludingDeletedAsync(id);

        if (credential == null)
        {
            return false;
        }

        await _credentialRepository.RemoveAsync(id);

        return true;
    }

    /// <summary> Mark a credential as used </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> True if marked successfully </returns>
    public async Task<bool> MarkCredentialAsUsedAsync(Guid id) { return await _credentialRepository.MarkAsUsedAsync(id); }

    /// <summary> Deactivate a credential </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> True if deactivated successfully </returns>
    public async Task<bool> DeactivateCredentialAsync(Guid id)
    {
        Credential? credential = await _credentialRepository.GetByIdAsync(id);

        if (credential == null)
        {
            return false;
        }

        if (credential.DeletedAt != null)
        {
            return false;
        }

        if (credential.IsActive)
        {
            return await _credentialRepository.DeactivateAsync(id);
        }

        return false;
    }

    /// <summary> Activate a credential </summary>
    /// <param name="id"> Credential ID </param>
    /// <returns> True if activated successfully </returns>
    public async Task<bool> ActivateCredentialAsync(Guid id)
    {
        Credential? credential = await _credentialRepository.GetByIdAsync(id);

        if (credential == null)
        {
            return false;
        }

        if (credential.DeletedAt != null)
        {
            return false;
        }

        if (!credential.IsActive)
        {
            return await _credentialRepository.ActivateAsync(id);
        }

        return false;
    }

    /// <summary> Get all credentials including soft-deleted ones </summary>
    /// <returns> List of all credentials </returns>
    public async Task<IEnumerable<Credential>> GetAllCredentialsAsync() { return await _credentialRepository.GetAllIncludingDeletedAsync(); }

    /// <summary> Get soft-deleted credentials </summary>
    /// <returns> List of soft-deleted credentials </returns>
    public async Task<IEnumerable<Credential>> GetDeletedCredentialsAsync() { return await _credentialRepository.GetDeletedAsync(); }
}
