using System.ComponentModel.DataAnnotations;
using MediatR;

namespace GameGuild.Modules.UserProfiles.Commands;

/// <summary>
/// Command to delete a user profile
/// </summary>
public class DeleteUserProfileCommand : IRequest<bool>
{
    [Required]
    public Guid UserProfileId { get; set; }

    public bool SoftDelete { get; set; } = true;
}

/// <summary>
/// Command to restore a soft-deleted user profile
/// </summary>
public class RestoreUserProfileCommand : IRequest<bool>
{
    [Required]
    public Guid UserProfileId { get; set; }
}
