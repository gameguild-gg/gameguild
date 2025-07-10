using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.UserProfiles.Entities;
using MediatR;

namespace GameGuild.Modules.UserProfiles.Queries;

/// <summary>
/// Query to get user profile by user ID
/// </summary>
public class GetUserProfileByUserIdQuery : IRequest<UserProfile?>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeDeleted { get; set; } = false;
}
