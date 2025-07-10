using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for adding a user to a group
/// </summary>
public class AddUserToGroupDto
{
    /// <summary>
    /// ID of the user to add to the group
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// ID of the user group to add the user to
    /// </summary>
    [Required]
    public Guid UserGroupId { get; set; }

    /// <summary>
    /// Whether this membership is automatically assigned
    /// </summary>
    public bool IsAutoAssigned { get; set; } = false;
}
