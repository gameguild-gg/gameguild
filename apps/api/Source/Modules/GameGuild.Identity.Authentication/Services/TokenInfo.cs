namespace GameGuild.Identity.Authentication;

internal class TokenInfo
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
