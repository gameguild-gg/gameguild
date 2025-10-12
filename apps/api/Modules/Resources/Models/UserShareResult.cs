namespace GameGuild.Modules.Resources;

/// <summary> Result for individual user in sharing operation </summary>
public class UserShareResult
{
    public string? Email { get; set; }

    public Guid? UserId { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public bool InvitationSent { get; set; }

    public Guid? InvitationId { get; set; }
}
