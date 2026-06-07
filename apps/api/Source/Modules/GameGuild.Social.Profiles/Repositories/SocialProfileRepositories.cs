using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Profiles;

public interface ISocialProfileRepository
{
    Task<SocialProfile> AddAsync(SocialProfile profile, CancellationToken ct = default);
    Task<SocialProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SocialProfile?> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SocialProfile?> GetByHandleAsync(string handle, CancellationToken ct = default);
    Task<List<SocialProfile>> SearchAsync(string? query, int take, CancellationToken ct = default);
    Task UpdateAsync(SocialProfile profile, CancellationToken ct = default);
}

public interface IProfileSkillRepository
{
    Task<ProfileSkill> AddAsync(ProfileSkill skill, CancellationToken ct = default);
    Task<ProfileSkill?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProfileSkill?> GetByProfileAndNameAsync(Guid profileId, string name, CancellationToken ct = default);
    Task UpdateAsync(ProfileSkill skill, CancellationToken ct = default);
    Task DeleteAsync(ProfileSkill skill, CancellationToken ct = default);
}

public interface IProfilePortfolioRepository
{
    Task<ProfilePortfolioItem> AddAsync(ProfilePortfolioItem item, CancellationToken ct = default);
    Task<ProfilePortfolioItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ProfilePortfolioItem>> GetByProfileAsync(Guid profileId, CancellationToken ct = default);
    Task UpdateAsync(ProfilePortfolioItem item, CancellationToken ct = default);
    Task DeleteAsync(ProfilePortfolioItem item, CancellationToken ct = default);
}

public sealed class SocialProfileRepository(IApplicationDbContext context) : ISocialProfileRepository
{
    public async Task<SocialProfile> AddAsync(SocialProfile profile, CancellationToken ct = default)
    {
        var entry = await context.Set<SocialProfile>().AddAsync(profile, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<SocialProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await IncludeProfileData(context.Set<SocialProfile>())
            .FirstOrDefaultAsync(profile => profile.Id == id && profile.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<SocialProfile?> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await IncludeProfileData(context.Set<SocialProfile>())
            .FirstOrDefaultAsync(profile => profile.UserId == userId && profile.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<SocialProfile?> GetByHandleAsync(string handle, CancellationToken ct = default)
    {
        var normalized = SocialProfile.NormalizeHandle(handle);
        return await IncludeProfileData(context.Set<SocialProfile>())
            .FirstOrDefaultAsync(profile => profile.Handle == normalized && profile.DeletedAt == null, ct)
            .ConfigureAwait(false);
    }

    public async Task<List<SocialProfile>> SearchAsync(string? query, int take, CancellationToken ct = default)
    {
        var profiles = IncludeProfileData(context.Set<SocialProfile>())
            .Where(profile => profile.Visibility == ProfileVisibility.Public && profile.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            profiles = profiles.Where(profile =>
                profile.Handle.Contains(normalized) ||
                profile.DisplayName.ToLower().Contains(normalized) ||
                (profile.Headline != null && profile.Headline.ToLower().Contains(normalized)));
        }

        return await profiles.OrderByDescending(profile => profile.CompletenessScore)
            .ThenBy(profile => profile.Handle)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(SocialProfile profile, CancellationToken ct = default)
    {
        profile.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static IQueryable<SocialProfile> IncludeProfileData(IQueryable<SocialProfile> query)
        => query.Include(profile => profile.Skills).Include(profile => profile.PortfolioItems);
}

public sealed class ProfileSkillRepository(IApplicationDbContext context) : IProfileSkillRepository
{
    public async Task<ProfileSkill> AddAsync(ProfileSkill skill, CancellationToken ct = default)
    {
        var entry = await context.Set<ProfileSkill>().AddAsync(skill, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<ProfileSkill?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<ProfileSkill>()
            .FirstOrDefaultAsync(skill => skill.Id == id && skill.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<ProfileSkill?> GetByProfileAndNameAsync(Guid profileId, string name, CancellationToken ct = default)
        => await context.Set<ProfileSkill>()
            .FirstOrDefaultAsync(skill => skill.ProfileId == profileId && skill.Name == name && skill.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task UpdateAsync(ProfileSkill skill, CancellationToken ct = default)
    {
        skill.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(ProfileSkill skill, CancellationToken ct = default)
    {
        context.Set<ProfileSkill>().Remove(skill);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class ProfilePortfolioRepository(IApplicationDbContext context) : IProfilePortfolioRepository
{
    public async Task<ProfilePortfolioItem> AddAsync(ProfilePortfolioItem item, CancellationToken ct = default)
    {
        var entry = await context.Set<ProfilePortfolioItem>().AddAsync(item, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<ProfilePortfolioItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<ProfilePortfolioItem>()
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<List<ProfilePortfolioItem>> GetByProfileAsync(Guid profileId, CancellationToken ct = default)
        => await context.Set<ProfilePortfolioItem>()
            .Where(item => item.ProfileId == profileId && item.DeletedAt == null)
            .OrderByDescending(item => item.IsPinned)
            .ThenBy(item => item.DisplayOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task UpdateAsync(ProfilePortfolioItem item, CancellationToken ct = default)
    {
        item.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(ProfilePortfolioItem item, CancellationToken ct = default)
    {
        context.Set<ProfilePortfolioItem>().Remove(item);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
