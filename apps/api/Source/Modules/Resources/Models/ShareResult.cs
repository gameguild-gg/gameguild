namespace GameGuild.Modules.Resources;

/// <summary> Result of a sharing operation </summary>
public class ShareResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public List<UserShareResult> UserResults { get; set; } = [];

    public Guid? ShareId { get; set; }
}
