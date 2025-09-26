namespace GameGuild.Modules.Resources;

/// <summary> Result of an invitation operation </summary>
public class InvitationResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public Guid? InvitationId { get; set; }

    public bool UserExists { get; set; }

    public bool EmailSent { get; set; }
}
