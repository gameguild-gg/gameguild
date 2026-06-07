using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Social.Profiles;

[Microsoft.AspNetCore.Http.Tags("social/profiles")]
[ApiController]
[Route("api/social/profiles")]
[Authorize]
public sealed class SocialProfilesController(ISender sender) : ControllerBase
{
    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<SocialProfileDto?>> GetByUser(Guid userId, CancellationToken ct)
        => Ok(await sender.Send(new GetSocialProfileByUserQuery(userId), ct).ConfigureAwait(false));

    [AllowAnonymous]
    [HttpGet("@{handle}")]
    public async Task<ActionResult<SocialProfileDto?>> GetByHandle(string handle, CancellationToken ct)
        => Ok(await sender.Send(new GetSocialProfileByHandleQuery(handle), ct).ConfigureAwait(false));

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<ActionResult<List<SocialProfileDto>>> Search([FromQuery] string? query, [FromQuery] int take = 20, CancellationToken ct = default)
        => Ok(await sender.Send(new SearchSocialProfilesQuery(query, take), ct).ConfigureAwait(false));

    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult<SocialProfileDto>> Upsert(Guid userId, [FromBody] UpdateSocialProfileBody body, CancellationToken ct)
        => Ok(await sender.Send(body.ToCommand(userId), ct).ConfigureAwait(false));

    [HttpPut("users/{userId:guid}/privacy")]
    public async Task<ActionResult<SocialProfileDto>> UpdatePrivacy(Guid userId, [FromBody] UpdateProfilePrivacyBody body, CancellationToken ct)
        => Ok(await sender.Send(new UpdateProfilePrivacyCommand(userId, body.Visibility, body.ShowActivity, body.ShowPortfolio, body.ShowSkills), ct).ConfigureAwait(false));

    [HttpPut("users/{userId:guid}/stats")]
    public async Task<ActionResult<SocialProfileDto>> UpdateStats(Guid userId, [FromBody] UpdateProfileStatsBody body, CancellationToken ct)
        => Ok(await sender.Send(new UpdateProfileStatsCommand(userId, body.FollowerCount, body.FollowingCount, body.PostCount, body.ProjectCount), ct).ConfigureAwait(false));

    [HttpPost("{profileId:guid}/skills")]
    public async Task<ActionResult<ProfileSkillDto>> AddSkill(Guid profileId, [FromBody] AddProfileSkillBody body, CancellationToken ct)
        => Ok(await sender.Send(new AddProfileSkillCommand(profileId, body.Name, body.Proficiency, body.DisplayOrder), ct).ConfigureAwait(false));

    [HttpDelete("skills/{skillId:guid}")]
    public async Task<IActionResult> RemoveSkill(Guid skillId, CancellationToken ct)
        => await sender.Send(new RemoveProfileSkillCommand(skillId), ct).ConfigureAwait(false) ? NoContent() : NotFound();

    [HttpPost("{profileId:guid}/portfolio")]
    public async Task<ActionResult<ProfilePortfolioItemDto>> AddPortfolioItem(Guid profileId, [FromBody] AddProfilePortfolioItemBody body, CancellationToken ct)
        => Ok(await sender.Send(new AddProfilePortfolioItemCommand(profileId, body.Title, body.ProjectId, body.Description, body.Url, body.ImageUrl, body.IsPinned, body.DisplayOrder), ct).ConfigureAwait(false));

    [HttpPut("portfolio/{itemId:guid}")]
    public async Task<ActionResult<ProfilePortfolioItemDto>> UpdatePortfolioItem(Guid itemId, [FromBody] UpdateProfilePortfolioItemBody body, CancellationToken ct)
        => Ok(await sender.Send(new UpdateProfilePortfolioItemCommand(itemId, body.Title, body.Description, body.Url, body.ImageUrl, body.IsPinned, body.DisplayOrder), ct).ConfigureAwait(false));

    [HttpDelete("portfolio/{itemId:guid}")]
    public async Task<IActionResult> RemovePortfolioItem(Guid itemId, CancellationToken ct)
        => await sender.Send(new RemoveProfilePortfolioItemCommand(itemId), ct).ConfigureAwait(false) ? NoContent() : NotFound();
}

public sealed record UpdateSocialProfileBody(
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
    ProfileAvailabilityStatus AvailabilityStatus = ProfileAvailabilityStatus.NotSet)
{
    public UpdateSocialProfileCommand ToCommand(Guid userId)
        => new(userId, Handle, DisplayName, Bio, AvatarUrl, BannerUrl, Headline, Location, TimeZone, WebsiteUrl, SocialLinksJson, AvailabilityStatus);
}

public sealed record UpdateProfilePrivacyBody(ProfileVisibility Visibility, bool ShowActivity, bool ShowPortfolio, bool ShowSkills);
public sealed record UpdateProfileStatsBody(int FollowerCount, int FollowingCount, int PostCount, int ProjectCount);
public sealed record AddProfileSkillBody(string Name, ProfileSkillProficiency Proficiency = ProfileSkillProficiency.Intermediate, int DisplayOrder = 0);
public sealed record AddProfilePortfolioItemBody(string Title, Guid? ProjectId = null, string? Description = null, string? Url = null, string? ImageUrl = null, bool IsPinned = false, int DisplayOrder = 0);
public sealed record UpdateProfilePortfolioItemBody(string Title, string? Description = null, string? Url = null, string? ImageUrl = null, bool IsPinned = false, int DisplayOrder = 0);
