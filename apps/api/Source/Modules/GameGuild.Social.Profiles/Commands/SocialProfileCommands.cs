using GameGuild.CQRS;

namespace GameGuild.Social.Profiles;

public sealed record UpdateSocialProfileCommand(
    Guid UserId,
    string Handle,
    string DisplayName,
    string? Bio = null,
    string? AvatarUrl = null,
    string? BannerUrl = null,
    string? Headline = null,
    string? Location = null,
    string? TimeZone = null,
    string? WebsiteUrl = null,
    string SocialLinksJson = "{}",
    ProfileAvailabilityStatus AvailabilityStatus = ProfileAvailabilityStatus.NotSet) : ICommand<SocialProfileDto>;

public sealed record UpdateProfilePrivacyCommand(
    Guid UserId,
    ProfileVisibility Visibility,
    bool ShowActivity,
    bool ShowPortfolio,
    bool ShowSkills) : ICommand<SocialProfileDto>;

public sealed record UpdateProfileStatsCommand(
    Guid UserId,
    int FollowerCount,
    int FollowingCount,
    int PostCount,
    int ProjectCount) : ICommand<SocialProfileDto>;

public sealed record AddProfileSkillCommand(
    Guid ProfileId,
    string Name,
    ProfileSkillProficiency Proficiency = ProfileSkillProficiency.Intermediate,
    int DisplayOrder = 0) : ICommand<ProfileSkillDto>;

public sealed record RemoveProfileSkillCommand(Guid SkillId) : ICommand<bool>;

public sealed record AddProfilePortfolioItemCommand(
    Guid ProfileId,
    string Title,
    Guid? ProjectId = null,
    string? Description = null,
    string? Url = null,
    string? ImageUrl = null,
    bool IsPinned = false,
    int DisplayOrder = 0) : ICommand<ProfilePortfolioItemDto>;

public sealed record UpdateProfilePortfolioItemCommand(
    Guid ItemId,
    string Title,
    string? Description = null,
    string? Url = null,
    string? ImageUrl = null,
    bool IsPinned = false,
    int DisplayOrder = 0) : ICommand<ProfilePortfolioItemDto>;

public sealed record RemoveProfilePortfolioItemCommand(Guid ItemId) : ICommand<bool>;

public sealed record GetSocialProfileByUserQuery(Guid UserId) : IQuery<SocialProfileDto?>;
public sealed record GetSocialProfileByHandleQuery(string Handle) : IQuery<SocialProfileDto?>;
public sealed record SearchSocialProfilesQuery(string? Query = null, int Take = 20) : IQuery<List<SocialProfileDto>>;

public sealed class UpdateSocialProfileCommandHandler(ISocialProfileService service) : ICommandHandler<UpdateSocialProfileCommand, SocialProfileDto>
{
    public Task<SocialProfileDto> Handle(UpdateSocialProfileCommand request, CancellationToken cancellationToken)
        => service.UpsertProfileAsync(request, cancellationToken);
}

public sealed class UpdateProfilePrivacyCommandHandler(ISocialProfileService service) : ICommandHandler<UpdateProfilePrivacyCommand, SocialProfileDto>
{
    public Task<SocialProfileDto> Handle(UpdateProfilePrivacyCommand request, CancellationToken cancellationToken)
        => service.UpdatePrivacyAsync(request, cancellationToken);
}

public sealed class UpdateProfileStatsCommandHandler(ISocialProfileService service) : ICommandHandler<UpdateProfileStatsCommand, SocialProfileDto>
{
    public Task<SocialProfileDto> Handle(UpdateProfileStatsCommand request, CancellationToken cancellationToken)
        => service.UpdateStatsAsync(request, cancellationToken);
}

public sealed class AddProfileSkillCommandHandler(ISocialProfileService service) : ICommandHandler<AddProfileSkillCommand, ProfileSkillDto>
{
    public Task<ProfileSkillDto> Handle(AddProfileSkillCommand request, CancellationToken cancellationToken)
        => service.AddOrUpdateSkillAsync(request, cancellationToken);
}

public sealed class RemoveProfileSkillCommandHandler(ISocialProfileService service) : ICommandHandler<RemoveProfileSkillCommand, bool>
{
    public Task<bool> Handle(RemoveProfileSkillCommand request, CancellationToken cancellationToken)
        => service.RemoveSkillAsync(request.SkillId, cancellationToken);
}

public sealed class AddProfilePortfolioItemCommandHandler(ISocialProfileService service) : ICommandHandler<AddProfilePortfolioItemCommand, ProfilePortfolioItemDto>
{
    public Task<ProfilePortfolioItemDto> Handle(AddProfilePortfolioItemCommand request, CancellationToken cancellationToken)
        => service.AddPortfolioItemAsync(request, cancellationToken);
}

public sealed class UpdateProfilePortfolioItemCommandHandler(ISocialProfileService service) : ICommandHandler<UpdateProfilePortfolioItemCommand, ProfilePortfolioItemDto>
{
    public Task<ProfilePortfolioItemDto> Handle(UpdateProfilePortfolioItemCommand request, CancellationToken cancellationToken)
        => service.UpdatePortfolioItemAsync(request, cancellationToken);
}

public sealed class RemoveProfilePortfolioItemCommandHandler(ISocialProfileService service) : ICommandHandler<RemoveProfilePortfolioItemCommand, bool>
{
    public Task<bool> Handle(RemoveProfilePortfolioItemCommand request, CancellationToken cancellationToken)
        => service.RemovePortfolioItemAsync(request.ItemId, cancellationToken);
}

public sealed class GetSocialProfileByUserQueryHandler(ISocialProfileService service) : IQueryHandler<GetSocialProfileByUserQuery, SocialProfileDto?>
{
    public Task<SocialProfileDto?> Handle(GetSocialProfileByUserQuery request, CancellationToken cancellationToken)
        => service.GetByUserAsync(request.UserId, cancellationToken);
}

public sealed class GetSocialProfileByHandleQueryHandler(ISocialProfileService service) : IQueryHandler<GetSocialProfileByHandleQuery, SocialProfileDto?>
{
    public Task<SocialProfileDto?> Handle(GetSocialProfileByHandleQuery request, CancellationToken cancellationToken)
        => service.GetByHandleAsync(request.Handle, cancellationToken);
}

public sealed class SearchSocialProfilesQueryHandler(ISocialProfileService service) : IQueryHandler<SearchSocialProfilesQuery, List<SocialProfileDto>>
{
    public Task<List<SocialProfileDto>> Handle(SearchSocialProfilesQuery request, CancellationToken cancellationToken)
        => service.SearchAsync(request.Query, request.Take, cancellationToken);
}
