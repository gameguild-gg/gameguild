using System.ComponentModel.DataAnnotations;
using MediatR;

namespace GameGuild.Modules.UserProfiles.Queries;

/// <summary>
/// Query to get user profile by user ID
/// </summary>
public class GetUserProfileByUserIdQuery : IRequest<Models.UserProfile?>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeDeleted { get; set; } = false;
}
