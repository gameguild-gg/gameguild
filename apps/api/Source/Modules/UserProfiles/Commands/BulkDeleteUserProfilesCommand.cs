using GameGuild.Common;

namespace GameGuild.Modules.UserProfiles.Commands;

/// <summary>
/// Command to bulk delete multiple user profiles
/// </summary>
public sealed class BulkDeleteUserProfilesCommand : ICommand<GameGuild.Common.Result<int>>
{
    /// <summary>
    /// User profile IDs to delete
    /// </summary>
    public required IEnumerable<Guid> UserProfileIds { get; set; }
    
    /// <summary>
    /// Whether to perform soft delete (default) or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;
    
    /// <summary>
    /// Reason for deletion
    /// </summary>
    public string? Reason { get; set; }
}
