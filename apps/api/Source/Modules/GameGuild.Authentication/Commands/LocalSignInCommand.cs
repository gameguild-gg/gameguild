using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to handle local user sign-in
/// </summary>
public class LocalSignInCommand : IRequest<SignInResponse>
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public Guid? TenantId { get; init; }

    public string? DeviceFingerprint { get; init; }
}
