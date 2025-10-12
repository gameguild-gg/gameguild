namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Repository interface for user profile data access operations
/// </summary>
public interface IUserProfileRepository
{
    // Basic CRUD operations
    /// <summary>
    /// Get a user profile by ID
    /// </summary>
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user profile by user ID
    /// </summary>
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active user profiles
    /// </summary>
    Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all user profiles including soft-deleted ones
    /// </summary>
    Task<IReadOnlyList<UserProfile>> GetAllAsync(bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new user profile
    /// </summary>
    Task<UserProfile> CreateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing user profile
    /// </summary>
    Task<UserProfile> UpdateAsync(UserProfile userProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard delete a user profile
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a user profile
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore a soft-deleted user profile
    /// </summary>
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all soft-deleted user profiles
    /// </summary>
    Task<IReadOnlyList<UserProfile>> GetDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user profile exists for a specific user
    /// </summary>
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search user profiles by various criteria
    /// </summary>
    Task<IReadOnlyList<UserProfile>> SearchAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user profile statistics
    /// </summary>
    Task<UserProfileStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? tenantId = null, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations
    /// </summary>
    Task<bool> BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> BulkRestoreAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save all pending changes
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Validation methods
    /// <summary>
    /// Check if a display name is unique (excluding a specific user profile ID for updates)
    /// </summary>
    Task<bool> IsDisplayNameUniqueAsync(string displayName, Guid? excludeUserProfileId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user profile exists by ID
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a deleted user profile exists by ID
    /// </summary>
    Task<bool> DeletedExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
