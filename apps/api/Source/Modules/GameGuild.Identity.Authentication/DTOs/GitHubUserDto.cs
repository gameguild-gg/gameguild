namespace GameGuild.Identity.Authentication;

/// <summary>
///     GitHub user information from OAuth
/// </summary>
public class GitHubUserDto
{
    public long Id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;
}
