namespace GameGuild.Identity.Authentication;

/// <summary>
///     Discord user information from OAuth
/// </summary>
public class DiscordUserDto
{
    public string Id { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? GlobalName { get; set; }

    public string? Email { get; set; }

    public bool? Verified { get; set; }

    public string? Avatar { get; set; }
}
