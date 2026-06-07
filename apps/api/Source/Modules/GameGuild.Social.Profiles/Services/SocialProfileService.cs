namespace GameGuild.Social.Profiles;

public interface ISocialProfileService
{
    Task<SocialProfileDto> UpsertProfileAsync(UpdateSocialProfileCommand command, CancellationToken ct = default);
    Task<SocialProfileDto?> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SocialProfileDto?> GetByHandleAsync(string handle, CancellationToken ct = default);
    Task<List<SocialProfileDto>> SearchAsync(string? query, int take, CancellationToken ct = default);
    Task<SocialProfileDto> UpdatePrivacyAsync(UpdateProfilePrivacyCommand command, CancellationToken ct = default);
    Task<SocialProfileDto> UpdateStatsAsync(UpdateProfileStatsCommand command, CancellationToken ct = default);
    Task<ProfileSkillDto> AddOrUpdateSkillAsync(AddProfileSkillCommand command, CancellationToken ct = default);
    Task<bool> RemoveSkillAsync(Guid skillId, CancellationToken ct = default);
    Task<ProfilePortfolioItemDto> AddPortfolioItemAsync(AddProfilePortfolioItemCommand command, CancellationToken ct = default);
    Task<ProfilePortfolioItemDto> UpdatePortfolioItemAsync(UpdateProfilePortfolioItemCommand command, CancellationToken ct = default);
    Task<bool> RemovePortfolioItemAsync(Guid itemId, CancellationToken ct = default);
}

public sealed class SocialProfileService(
    ISocialProfileRepository profileRepository,
    IProfileSkillRepository skillRepository,
    IProfilePortfolioRepository portfolioRepository) : ISocialProfileService
{
    public async Task<SocialProfileDto> UpsertProfileAsync(UpdateSocialProfileCommand command, CancellationToken ct = default)
    {
        var profile = await profileRepository.GetByUserAsync(command.UserId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            profile = new SocialProfile { UserId = command.UserId };
            profile.UpdateProfile(command);
            return (await profileRepository.AddAsync(profile, ct).ConfigureAwait(false)).ToDto();
        }

        profile.UpdateProfile(command);
        await profileRepository.UpdateAsync(profile, ct).ConfigureAwait(false);
        return profile.ToDto();
    }

    public async Task<SocialProfileDto?> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => (await profileRepository.GetByUserAsync(userId, ct).ConfigureAwait(false))?.ToDto();

    public async Task<SocialProfileDto?> GetByHandleAsync(string handle, CancellationToken ct = default)
        => (await profileRepository.GetByHandleAsync(handle, ct).ConfigureAwait(false))?.ToDto();

    public async Task<List<SocialProfileDto>> SearchAsync(string? query, int take, CancellationToken ct = default)
        => (await profileRepository.SearchAsync(query, take, ct).ConfigureAwait(false)).Select(profile => profile.ToDto()).ToList();

    public async Task<SocialProfileDto> UpdatePrivacyAsync(UpdateProfilePrivacyCommand command, CancellationToken ct = default)
    {
        var profile = await profileRepository.GetByUserAsync(command.UserId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Social profile for user {command.UserId} was not found.");

        profile.UpdatePrivacy(command.Visibility, command.ShowActivity, command.ShowPortfolio, command.ShowSkills);
        await profileRepository.UpdateAsync(profile, ct).ConfigureAwait(false);
        return profile.ToDto();
    }

    public async Task<SocialProfileDto> UpdateStatsAsync(UpdateProfileStatsCommand command, CancellationToken ct = default)
    {
        var profile = await profileRepository.GetByUserAsync(command.UserId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Social profile for user {command.UserId} was not found.");

        profile.UpdateStats(command.FollowerCount, command.FollowingCount, command.PostCount, command.ProjectCount);
        await profileRepository.UpdateAsync(profile, ct).ConfigureAwait(false);
        return profile.ToDto();
    }

    public async Task<ProfileSkillDto> AddOrUpdateSkillAsync(AddProfileSkillCommand command, CancellationToken ct = default)
    {
        var normalized = command.Name.Trim();
        var existing = await skillRepository.GetByProfileAndNameAsync(command.ProfileId, normalized, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            existing.Update(command.Proficiency, command.DisplayOrder);
            await skillRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
            return existing.ToDto();
        }

        var skill = new ProfileSkill
        {
            ProfileId = command.ProfileId,
            Name = normalized,
            Proficiency = command.Proficiency,
            DisplayOrder = command.DisplayOrder
        };

        return (await skillRepository.AddAsync(skill, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<bool> RemoveSkillAsync(Guid skillId, CancellationToken ct = default)
    {
        var skill = await skillRepository.GetByIdAsync(skillId, ct).ConfigureAwait(false);
        if (skill is null)
        {
            return false;
        }

        await skillRepository.DeleteAsync(skill, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<ProfilePortfolioItemDto> AddPortfolioItemAsync(AddProfilePortfolioItemCommand command, CancellationToken ct = default)
    {
        var item = new ProfilePortfolioItem
        {
            ProfileId = command.ProfileId,
            ProjectId = command.ProjectId,
            Title = command.Title,
            Description = command.Description,
            Url = command.Url,
            ImageUrl = command.ImageUrl,
            IsPinned = command.IsPinned,
            DisplayOrder = command.DisplayOrder
        };

        return (await portfolioRepository.AddAsync(item, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<ProfilePortfolioItemDto> UpdatePortfolioItemAsync(UpdateProfilePortfolioItemCommand command, CancellationToken ct = default)
    {
        var item = await portfolioRepository.GetByIdAsync(command.ItemId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Portfolio item {command.ItemId} was not found.");

        item.Update(command.Title, command.Description, command.Url, command.ImageUrl, command.IsPinned, command.DisplayOrder);
        await portfolioRepository.UpdateAsync(item, ct).ConfigureAwait(false);
        return item.ToDto();
    }

    public async Task<bool> RemovePortfolioItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await portfolioRepository.GetByIdAsync(itemId, ct).ConfigureAwait(false);
        if (item is null)
        {
            return false;
        }

        await portfolioRepository.DeleteAsync(item, ct).ConfigureAwait(false);
        return true;
    }
}
