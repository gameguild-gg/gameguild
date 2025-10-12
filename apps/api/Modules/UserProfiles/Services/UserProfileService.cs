namespace GameGuild.Modules.UserProfiles;

public class UserProfileService(IUserProfileRepository repository) : IUserProfileService
{
    private readonly IUserProfileRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<UserProfile>> GetAllUserProfilesAsync() { return await _repository.GetAllAsync(); }

    public async Task<UserProfile?> GetUserProfileByIdAsync(Guid id) { return await _repository.GetByIdAsync(id); }

    public async Task<UserProfile?> GetUserProfileByUserIdAsync(Guid userId) { return await _repository.GetByUserIdAsync(userId); }

    public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile) { return await _repository.CreateAsync(userProfile); }

    public async Task<UserProfile?> UpdateUserProfileAsync(Guid id, UserProfile userProfile)
    {
        UserProfile? existingProfile = await _repository.GetByIdAsync(id);

        if (existingProfile == null) return null;

        existingProfile.DisplayName = userProfile.DisplayName;

        return await _repository.UpdateAsync(existingProfile);
    }

    public async Task<bool> DeleteUserProfileAsync(Guid id) { return await _repository.DeleteAsync(id); }

    public async Task<bool> SoftDeleteUserProfileAsync(Guid id) { return await _repository.SoftDeleteAsync(id); }

    public async Task<bool> RestoreUserProfileAsync(Guid id) { return await _repository.RestoreAsync(id); }

    public async Task<IEnumerable<UserProfile>> GetDeletedUserProfilesAsync() { return await _repository.GetDeletedAsync(); }

    public async Task<UserProfileStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, Guid? tenantId = null, bool includeDeleted = false)
    {
        return await _repository.GetStatisticsAsync(fromDate, toDate, tenantId, includeDeleted);
    }
}
