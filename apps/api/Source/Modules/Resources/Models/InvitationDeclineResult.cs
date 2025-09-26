namespace GameGuild.Modules.Resources;

/// <summary> Result of declining an invitation </summary>
public class InvitationDeclineResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }
}
