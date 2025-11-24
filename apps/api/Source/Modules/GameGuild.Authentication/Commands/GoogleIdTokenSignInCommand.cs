using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to handle Google ID token sign-in (for NextAuth.js integration)
/// </summary>
public class GoogleIdTokenSignInCommand : IRequest<SignInResponse>
{
    public string IdToken { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public string? DeviceFingerprint { get; set; }
}
