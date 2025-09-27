namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for GitHub callback requests
/// </summary>
public class GitHubCallbackRequestDto
{
    public string Code { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }
}
