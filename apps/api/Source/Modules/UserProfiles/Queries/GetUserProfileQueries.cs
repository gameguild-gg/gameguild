using System.ComponentModel.DataAnnotations;
using MediatR;

namespace GameGuild.Modules.UserProfiles.Queries;

/// <summary>
/// Query to get all user profiles with optional filtering
/// </summary>
public class GetAllUserProfilesQuery : IRequest<IEnumerable<Models.UserProfile>>
{
    public bool IncludeDeleted { get; set; } = false;

    public int Skip { get; set; } = 0;

    [Range(1, 100)]
    public int Take { get; set; } = 50;

    public string? SearchTerm { get; set; }

    public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get user profile by ID
/// </summary>
public class GetUserProfileByIdQuery : IRequest<Models.UserProfile?>
{
    [Required]
    public Guid UserProfileId { get; set; }

    public bool IncludeDeleted { get; set; } = false;
}
