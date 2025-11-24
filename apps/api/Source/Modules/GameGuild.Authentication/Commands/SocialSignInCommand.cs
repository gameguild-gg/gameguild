using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Enums;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

/// <summary>
///     Unified command for social provider sign-in supporting multiple providers
/// </summary>
public class SocialSignInCommand : IRequest<SignInResponse>
{
    /// <summary>
    ///     Social authentication provider
    /// </summary>
    public SocialProvider Provider { get; init; }

    /// <summary>
    ///     OAuth token or authorization code
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    ///     Optional redirect URI for OAuth callback
    /// </summary>
    public string? RedirectUri { get; init; }

    /// <summary>
    ///     Optional tenant context
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Optional device fingerprint
    /// </summary>
    public string? DeviceFingerprint { get; init; }

    /// <summary>
    ///     Whether to link this account if user already exists
    /// </summary>
    public bool LinkIfExists { get; init; }

    /// <summary>
    ///     Additional provider-specific parameters
    /// </summary>
    public Dictionary<string, string> AdditionalParameters { get; init; } = new Dictionary<string, string>();
}
