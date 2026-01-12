using System.ComponentModel.DataAnnotations;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Request for GitHub OAuth callback
/// </summary>
public class GitHubCallbackRequestDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    public string? State { get; set; }

    public string? RedirectUri { get; set; }

    public Guid? TenantId { get; set; }
}
